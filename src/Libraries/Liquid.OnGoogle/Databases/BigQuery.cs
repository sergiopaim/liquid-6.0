using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Bigquery.v2.Data;
using Google.Cloud.BigQuery.V2;
using Liquid.Base;
using Liquid.Interfaces;
using Liquid.Repository;
using Liquid.Runtime;
using Microsoft.Azure.Cosmos.Spatial;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static System.Text.Json.JsonElement;

namespace Liquid.OnGoogle
{
    /// <summary>
    /// Provides a unique namespace to store and access your BigQuery table rows.
    /// </summary>
    public class BigQuery : LightRepository
    {
        private const int COMMAND_TIMEOUT_IN_MINUTES = 30;
        private const int COMMAND_RETRY_MAX_NUMBER = 3;
        private const int COMMAND_RETRY_DELTA_IN_MILLISECONDS = 10;
        private const int QUERY_TIMEOUT_IN_SECONDS = 50;

        private static GetQueryResultsOptions commandOptions = null;
        private static GetQueryResultsOptions CommandOptions
        {
            get
            {
                commandOptions ??= new() { Timeout = TimeSpan.FromMinutes(COMMAND_TIMEOUT_IN_MINUTES) };
                return commandOptions;
            }
        }

        private readonly BigQueryClientHandler clientHandler = new();
        private readonly string suffixName;
        private static readonly ConcurrentDictionary<string, BigQueryConfiguration> configs = new();
        private static readonly ConcurrentDictionary<string, BigQueryConfiguration> configsPerEntity = new();

        private static string isReseeding = "false";
        private static bool IsReseeding
        {
            get
            {
                return isReseeding == "true";
            }
            set
            {
                lock (isReseeding)
                {
                    isReseeding = value ? "true" : "false";
                }
            }
        }

        #region OLTP methods that are also OLAP/ETL/ELT/Streaming methods
        /// <summary>
        /// Add the document
        /// Calls the base class because there may be some generic behavior in it
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="model">generic model</param>
        /// <returns>The entity to be created</returns>
        public override async Task<T> AddAsync<T>(T model)
        {
            try
            {
                var config = GetConfigByEntity(model.GetType().Name);

                _ = await clientHandler.Client.InsertRowAsync(config.DatasetId, model.GetType().Name.ToLower(), BigQueryRowFrom(model));
            }
            catch (GoogleApiException e)
            {
                string textError = string.Join("\n", e.Error.Errors.Select(e => $"[{e.Location}] {e.Message}"));

                throw new LightException($"The following errors was found during row insertion into Google BigQuery:\n{textError}\n", e);
            }

            return default;
        }

        /// <summary>
        /// Add the document
        /// Calls the base class because there may be some generic behavior in it
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="models">list generic models</param>
        /// <returns>Entities not processed due to errors</returns>
        public override async Task<IEnumerable<T>> AddAsync<T>(List<T> models)
        {
            if (models is null)
                return default;

            List<BigQueryInsertRow> rows = new();

            var config = GetConfigByEntity(models.GetType().GetGenericTypeDefinition().Name);

            foreach (var model in models)
                rows.Add(BigQueryRowFrom(model));

            try
            {
                _ = await clientHandler.Client.InsertRowsAsync(config.DatasetId, models.FirstOrDefault().GetType().Name.ToLower(), rows);
            }
            catch (GoogleApiException e)
            {
                string textError = string.Join("\n", e.Error.Errors.Select(e => $"[{e.Location}] {e.Message}"));

                throw new LightException($"The following errors was found during row insertion into Google BigQuery:\n{textError}\n", e);
            }

            return default;
        }

        private static BigQueryInsertRow BigQueryRowFrom<T>(T model)
        {
            var row = new BigQueryInsertRow();

            foreach (var prop in model.GetType().GetProperties()
                                .Where(p => p.GetCustomAttributes(true)?.Any(a => a.GetType() == typeof(JsonIgnoreAttribute)) != true))
            {
                string key = prop.Name.Length <= 1 ? prop.Name.ToLower() : (prop.Name[..1].ToLower() + prop.Name[1..]);
                object obj = prop.GetValue(model, null);

                if (obj is not null)
                    if (obj is IList && obj.GetType().IsGenericType)
                    {
                        var baseTypeName = obj.GetType().GenericTypeArguments.FirstOrDefault().BaseType.Name;
                        if (baseTypeName.StartsWith("LightModel") ||
                            baseTypeName.StartsWith("LightValueObject"))
                        {
                            var rowList = new List<BigQueryInsertRow>();

                            foreach (var item in (obj as IList))
                                rowList.Add(BigQueryRowFrom(item));

                            row.Add(key, rowList);
                        }
                        else
                            row.Add(key, obj);
                    }
                    else if (obj.GetType().BaseType.Name.StartsWith("LightModel") ||
                             obj.GetType().BaseType.Name.StartsWith("LightValueObject"))
                    {
                        row.Add(key, BigQueryRowFrom(obj));
                    }
                    else if (obj.GetType().BaseType.Name.StartsWith("LightLocalizedEnum"))
                    {
                        var enumRow = new BigQueryInsertRow
                        {
                            { "code", obj.GetType().GetProperty("Code").GetValue(obj) },
                            { "label", obj.GetType().GetProperty("Label").GetValue(obj) }
                        };

                        row.Add(key, enumRow);
                    }
                    else if (obj.GetType().Equals(typeof(Point)))
                        row.Add(key, (obj as Point).ToJsonString());
                    else
                        row.Add(key, obj);
                else
                    row.Add(key, null);
            }

            if (model.GetType().GetInterfaces().Any(i => i == typeof(ILightModel)))
                row.InsertId = (model as ILightModel).Id;

            return row;
        }

        /// <summary>
        /// Expand general SQL prototypes as SQL sintax specific for the repository database
        /// </summary>
        /// <param name="query">SQL DML commands</param>
        /// <returns></returns>
        public override string ExpandQuery(string query)
        {
            return ExpandQueryReferences(query);
        }

        /// <summary>
        /// Expand general DML prototypes as DLM sintax specific for the repository database
        /// </summary>
        /// <param name="command">SQL DML commands</param>
        /// <returns></returns>
        public override string ExpandCommand(string command)
        {
            return ExpandCommandReferences(command);
        }

        /// <summary>
        /// Calls the base class because there may be some generic behavior in it
        /// This method creates a query for documents under a collection
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="query">query to create the document</param>
        /// <returns></returns>
        public override IEnumerable<T> Query<T>(string query)
        {
            if (string.IsNullOrEmpty(query))
                throw new LightException("Cannot submit an empty query to Google BigQuery.");

            base.Query<T>(query);

            query = ExpandQueryReferences(query);

            try
            {
                return TryQuery<T>(query);
            }
            catch (Exception e) when (e is not TimeoutException && 
                                      e is not LightException && 
                                      e is not TaskCanceledException)
            //Catches the job errors thrown during results interation and immediately retries one more time
            //https://stackoverflow.com/questions/41129805/why-is-google-bigquery-api-returning-a-400-instead-of-a-500-status-code
            //https://github.com/googleapis/python-bigquery/issues/23
            {
                return TryQuery<T>(query);
            }
        }

        /// <summary>
        /// Calls the base class because there may be some generic behavior in it
        /// This method creates a query for documents under a collection
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="query">query to create the document</param>
        /// <returns></returns>
        public override async Task<IEnumerable<T>> QueryAsync<T>(string query)
        {
            const int MAX_RETRY = 2;
            if (string.IsNullOrEmpty(query))
                throw new LightException("Cannot submit an empty query to Google BigQuery.");

            await base.QueryAsync<T>(query);

            query = ExpandQueryReferences(query);

            var timeoutAt = WorkBench.UtcNow.AddSeconds(QUERY_TIMEOUT_IN_SECONDS);

            var docs = new List<T>();
            BigQueryResults results = null;
            for (int retry = 1; retry <= MAX_RETRY; retry++)
                try
                {
                    try
                    {
                        var utcNow = WorkBench.UtcNow;
                        if (timeoutAt > utcNow)
                        {
                            CancellationTokenSource cts = new();
                            cts.CancelAfter(timeoutAt - utcNow);

                            results = await clientHandler.Client.ExecuteQueryAsync(query, null, null, cancellationToken: cts.Token);

                            foreach (var row in results)
                                docs.Add(JsonSerializer.Deserialize<T>(row["document"].ToString(), LightGeneralSerialization.IgnoreCase));

                            break;
                        }
                        else
                            throw new OperationCanceledException("timeout in between retries");
                    }
                    catch (GoogleApiException e)
                    {
                        string textError = string.Join("\n", e.Error.Errors.Select(e => e.Message));

                        if (retry >= MAX_RETRY || !textError.ToLower().Contains("retry"))
                            throw new LightException($"The query following query got an exception from Google BigQuery:\n{query}\n\n ---> With errors:\n{textError}\n", e);
                    }
                    catch (TaskCanceledException e)
                    {
                        WorkBench.Telemetry.TrackTrace(e.Message);
                        WorkBench.Telemetry.TrackTrace(e.StackTrace);

                        if (timeoutAt <= WorkBench.UtcNow)
                            throw new TimeoutException($"Bigquery query took more than {QUERY_TIMEOUT_IN_SECONDS}s to respond", e);
                        else
                        {
                            clientHandler.Reset();
                            throw;
                        }
                    }
                }
                catch (OperationCanceledException e)
                {
                    throw new TimeoutException($"Bigquery query took more than {QUERY_TIMEOUT_IN_SECONDS}s to respond", e);
                }
                catch (Exception e) when (e is not TimeoutException && e is not LightException)
                //Catches the job errors thrown during results interation
                //https://stackoverflow.com/questions/41129805/why-is-google-bigquery-api-returning-a-400-instead-of-a-500-status-code
                //https://github.com/googleapis/python-bigquery/issues/23
                {
                    if (retry >= MAX_RETRY)
                        throw;
                }

            return docs;
        }

        /// <summary>
        /// Get data by sql query in paginated form
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="query">SQL statement with parameterized values</param>
        /// <param name="pagingParms">Paging parameters (if ommited, default is first page and 20 itemsper page)</param>
        /// <returns>Paginated list of LightDomain entities</returns>
        public override async Task<ILightPaging<T>> QueryByPageAsync<T>(string query, ILightPagingParms pagingParms) 
        {
            pagingParms ??= LightPagingParms.Default;
            if (pagingParms.ItemsPerPage <= 0)
                pagingParms.ItemsPerPage = LightPagingParms.Default.ItemsPerPage;

            const int MAX_RETRY = 2;
            if (string.IsNullOrEmpty(query))
                throw new LightException("Cannot submit an empty query to Google BigQuery.");

            await base.QueryByPageAsync<T>(query, pagingParms); //Calls the base class because there may be some generic behavior in it

            query = ExpandQueryReferences(query);

            var timeoutAt = WorkBench.UtcNow.AddSeconds(QUERY_TIMEOUT_IN_SECONDS);

            GetQueryResultsOptions pageOptions = new()
            {
                PageSize = pagingParms.ItemsPerPage,
                PageToken = pagingParms.ContinuationToken == "string" ? null : pagingParms.ContinuationToken
            };

            var docs = new List<T>();
            BigQueryResults results = null;
            BigQueryPage page = null;
            for (int retry = 1; retry <= MAX_RETRY; retry++)
                try
                {
                    try
                    {
                        var utcNow = WorkBench.UtcNow;
                        if (timeoutAt > utcNow)
                        {
                            CancellationTokenSource cts = new();
                            cts.CancelAfter(timeoutAt - utcNow);

                            results = await clientHandler.Client.ExecuteQueryAsync(query, null, null, pageOptions, cts.Token);
                            page = results.ReadPage(pageOptions.PageSize ?? LightPagingParms.Default.ItemsPerPage);

                            foreach (var row in page.Rows)
                                docs.Add(JsonSerializer.Deserialize<T>(row["document"].ToString(), LightGeneralSerialization.IgnoreCase));

                            break;
                        }
                        else
                            throw new OperationCanceledException("timeout in between retries");
                    }
                    catch (GoogleApiException e)
                    {
                        string textError = string.Join("\n", e.Error.Errors.Select(e => e.Message));

                        if (retry >= MAX_RETRY || !textError.ToLower().Contains("retry"))
                            throw new LightException($"The query following query got an exception from Google BigQuery:\n{query}\n\n ---> With errors:\n{textError}\n", e);
                    }
                    catch (TaskCanceledException e)
                    {
                        WorkBench.Telemetry.TrackTrace(e.Message);
                        WorkBench.Telemetry.TrackTrace(e.StackTrace);

                        if (timeoutAt <= WorkBench.UtcNow)
                            throw new TimeoutException($"Bigquery query took more than {QUERY_TIMEOUT_IN_SECONDS}s to respond", e);
                        else
                        {
                            clientHandler.Reset();
                            throw;
                        }
                    }
                }
                catch (OperationCanceledException e)
                {
                    throw new TimeoutException($"Bigquery query took more than {QUERY_TIMEOUT_IN_SECONDS}s to respond", e);
                }
                catch (Exception e) when (e is not TimeoutException && e is not LightException)
                //Catches the job errors thrown during results interation
                //https://stackoverflow.com/questions/41129805/why-is-google-bigquery-api-returning-a-400-instead-of-a-500-status-code
                //https://github.com/googleapis/python-bigquery/issues/23
                {
                    if (retry >= MAX_RETRY)
                        throw;
                }

            string continuationToken = page?.NextPageToken;

            LightPaging<T> pageResult = new()
            {
                Data = docs,
                ItemsPerPage = pagingParms.ItemsPerPage,
                ContinuationToken = continuationToken
            };

            return pageResult;
        }

        /// <summary>
        /// Submit DML commands to database
        /// </summary>
        /// <param name="command">SQL DML commands</param>
        /// <returns></returns>
        public override async Task CommandAsync(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new LightException("Cannot submit an empty DML command to Google BigQuery.");

            await base.CommandAsync(command);

            await Task.Run(() => CommandWithRetry(ExpandCommandReferences(command), 0));
        }

        /// <summary>
        /// Mount a SQL query for agregating insert and update data events by a time sliding window
        /// </summary>
        /// <param name="selectedColumns">List of projected columns from the event agregation</param>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        public override string SlidingUpsertSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            string query = @$"SELECT {selectedColumns} 
                              FROM (SELECT 
                                      *,
                                      ROW_NUMBER() OVER (PARTITION BY {partitionColumns} 
                                                         ORDER BY ingestedAt DESC) AS rowNumber
                                    FROM <{eventTable?.Name}> 
                                    WHERE ingestedAt BETWEEN '{fromDateTime:u}' AND '{toDateTime:u}')
                              WHERE
                                  rowNumber = 1
                                  AND command IN ('{LightDataEventCMD.Insert.Code}', '{LightDataEventCMD.Update.Code}')";

            return query;
        }

        /// <summary>
        /// Mount a SQL query for agregating delete data events by a time sliding window
        /// </summary>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        public override string SlidingDeleteSql(Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            return SlidingDeleteSql(partitionColumns, eventTable, partitionColumns, fromDateTime, toDateTime);
        }

        /// <summary>
        /// Mount a SQL query for agregating delete data events by a time sliding window
        /// </summary>
        /// <param name="selectedColumns">List of projected columns from the event agregation</param>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        public override string SlidingDeleteSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            string query = @$"SELECT {selectedColumns} 
                              FROM (SELECT 
                                      *,
                                      ROW_NUMBER() OVER (PARTITION BY {partitionColumns} 
                                                         ORDER BY ingestedAt DESC) AS rowNumber
                                    FROM <{eventTable?.Name}> 
                                    WHERE ingestedAt BETWEEN '{fromDateTime:u}' AND '{toDateTime:u}')
                              WHERE
                                  rowNumber = 1
                                  AND command IN ('{LightDataEventCMD.Delete.Code}')";

            return query;
        }

        /// <summary>
        /// Mount a SQL query for agregating all data events by a time sliding window
        /// </summary>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        public override string SlidingAllSql(Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            return SlidingAllSql(partitionColumns, eventTable, partitionColumns, fromDateTime, toDateTime);
        }

        /// <summary>
        /// Mount a SQL query for agregating all data events by a time sliding window
        /// </summary>
        /// <param name="selectedColumns">List of projected columns from the event agregation</param>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        public override string SlidingAllSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            string query = @$"SELECT {selectedColumns}  
                              FROM (SELECT 
                                      *,
                                      ROW_NUMBER() OVER (PARTITION BY {partitionColumns} 
                                                         ORDER BY ingestedAt DESC) AS rowNumber
                                    FROM <{eventTable?.Name}> 
                                    WHERE ingestedAt BETWEEN '{fromDateTime:u}' AND '{toDateTime:u}')
                              WHERE
                                  rowNumber = 1";

            return query;
        }

        private void CommandWithRetry(string command, int retry)
        {
            BigQueryResults results;

            try
            {
                results = clientHandler.Client.ExecuteQuery(command, parameters: null, resultsOptions: CommandOptions);
            }
            catch (GoogleApiException e)
            {
                string textError = string.Join("\n", e.Error.Errors.Select(e => e.Message));

                throw new LightException($"The query following DML command got an exception from Google BigQuery:\n{command}\n\n ---> With errors:\n{textError}\n", e);
            }
            catch (TaskCanceledException)
            {
                if (clientHandler.Reset())
                    results = clientHandler.Client.ExecuteQuery(command, parameters: null, resultsOptions: CommandOptions);
                else
                    throw;
            }

            try
            {
                foreach (var row in results) 
                //forces the sdk to raise job errors, including the 400 that should be 503
                //https://stackoverflow.com/questions/41129805/why-is-google-bigquery-api-returning-a-400-instead-of-a-500-status-code
                //https://github.com/googleapis/python-bigquery/issues/23
                { }
            }
            catch
            {
                if (retry < COMMAND_RETRY_MAX_NUMBER)
                {
                    Thread.Sleep(retry * COMMAND_RETRY_DELTA_IN_MILLISECONDS);
                    CommandWithRetry(command, retry + 1);
                }
            }

            return;
        }

        private IEnumerable<T> TryQuery<T>(string query)
        {
            var timeoutAt = WorkBench.UtcNow.AddSeconds(QUERY_TIMEOUT_IN_SECONDS);

            BigQueryResults results = null;
            try
            {
                try
                {
                    CancellationTokenSource cts = new();
                    cts.CancelAfter(timeoutAt - WorkBench.UtcNow);

                    results = clientHandler.Client.ExecuteQueryAsync(query, null, null, cancellationToken: cts.Token).Result;
                }
                catch (GoogleApiException e)
                {
                    string textError = string.Join("\n", e.Error.Errors.Select(e => e.Message));

                    throw new LightException($"The query following query got an exception from Google BigQuery:\n{query}\n\n ---> With errors:\n{textError}\n", e);
                }
                catch (TaskCanceledException)
                {
                    if (clientHandler.Reset())
                    {
                        var utcNow = WorkBench.UtcNow;

                        if (timeoutAt > utcNow)
                        {
                            CancellationTokenSource cts = new();
                            cts.CancelAfter(timeoutAt - utcNow);

                            results = clientHandler.Client.ExecuteQueryAsync(query, null, null, cancellationToken: cts.Token).Result;
                        }
                        else
                            throw new OperationCanceledException("timeout in between retries");
                    }
                    else
                        throw;
                }
            }
            catch (OperationCanceledException ex)
            {
                throw new TimeoutException($"Bigquery query took more than {QUERY_TIMEOUT_IN_SECONDS}s to respond", ex);
            }

            if (results is null)
                yield break;
            else
                foreach (var row in results)
                    yield return JsonSerializer.Deserialize<T>(row["document"].ToString(), LightGeneralSerialization.IgnoreCase);
        }

        private static string ExpandQueryReferences(string query)
        {
            query = Regex.Replace(query, @"[^\S\r\n]+", " ");

            return @$"WITH json AS ({ExpandTableRefs(query)}) SELECT TO_JSON_STRING(j) AS document FROM json AS j";
        }

        private static string ExpandCommandReferences(string command)
        {
            command = Regex.Replace(command, @"[^\S\r\n]+", " ");

            return ExpandTableRefs(command);
        }

        private static string ExpandTableRefs(string statement)
        {
            var tableRefs = new Regex(@"\<(\w|-)+\>")
                                     .Matches(statement).Select(m => m.Value);

            foreach (var tableRef in tableRefs)
            {
                var tableName = tableRef[1..^1];

                var config = GetConfigByEntity(tableName);

                statement = statement.Replace(tableRef, $"{config.ProjectId}.{config.DatasetId}.{tableName.ToLower()}");
            }

            return statement;
        }

        private static BigQueryConfiguration GetConfigByEntity(string entityName)
        {
            if (configsPerEntity.ContainsKey(entityName))
                return configsPerEntity.FirstOrDefault(c => c.Key == entityName).Value;

            var type = GetEntityType(entityName);
            var attribute = type?.GetCustomAttributes(typeof(RepositoryConfigAttribute), false).FirstOrDefault() as RepositoryConfigAttribute;
            var config = GetConfig(attribute?.Suffix);

            configsPerEntity.AddOrUpdate(entityName, config, (k, v) => v = config);

            return config;
        }

        #endregion

        #region Repository setup and management methods

        /// <summary>
        /// Creates a BigQuery instance from default settings provided on appsettings.
        /// </summary>
        public BigQuery() : this("")
        {
        }

        /// <summary>
        /// Creates a BigQuery instance from specific settings to a given name provided on appsettings.
        /// The configuration should be provided like "BigQuery_{sufixName}".
        /// </summary>
        /// <param name="suffixName">Name of configuration</param>
        public BigQuery(string suffixName)
        {
            this.suffixName = suffixName;
        }

        /// <summary>
        /// Initialize BigQuery repository
        /// </summary>
        public override void Initialize()
        {
            //Initialize BigQuery instance with provided settings            
            LoadConfigurations();
        }

        private static BigQueryConfiguration GetConfig(string suffixName = null)
        {
            string key = string.IsNullOrEmpty(suffixName) ? "STD" : suffixName.ToUpper();

            if (configs.ContainsKey(key))
                return configs.FirstOrDefault(c => c.Key == key).Value;
            else
            {
                BigQueryConfiguration config;

                if (string.IsNullOrEmpty(suffixName)) // Load specific settings if provided
                    config = LightConfigurator.LoadConfig<BigQueryConfiguration>($"{nameof(BigQuery)}");
                else
                    config = LightConfigurator.LoadConfig<BigQueryConfiguration>($"{nameof(BigQuery)}_{suffixName}");

                if (WorkBench.IsDevelopmentEnvironment)
                {
                    var mn = Environment.MachineName.ToLower();
                    if (mn.Length > 7)
                        // assuming MachineName of the format `DESKTOP-XXXXXXX`
                        mn = mn[^7..];

                    config.DatasetId = $"{mn}_{config.DatasetId}";
                }

                configs.AddOrUpdate(key, config, (k, v) => v = config);

                return config;
            }
        }

        /// <summary>
        /// Load Configutaion variables
        /// </summary>
        private void LoadConfigurations()
        {
            Setup = true;
            var config = GetConfig(suffixName);

            using var stream = new MemoryStream(Convert.FromBase64String(config.Base64Key));
            var saCredential = ServiceAccountCredential.FromServiceAccountData(stream);
            var gCredential = GoogleCredential.FromServiceAccountCredential(saCredential);

            clientHandler.Initialize(config.ProjectId, gCredential);
        }

        /// <summary>
        /// Clean the database and fullfill with inicial data 
        /// (only if is develop, integration and testing environment)
        /// </summary>
        /// <param name="dataSetType">Type of dataset (Unit, Integration, System, etc.)</param>
        /// <param name="force"></param>
        /// <returns>Result from the operation in a message</returns>
        public override async Task<bool> ReseedDataAsync(string dataSetType = "Unit", bool force = false)
        {
            if (IsReseeding)
            {
                WorkBench.ConsoleWriteLine($"A reseed operation is ALREADY RUNNING. Wait for it to complete before starting a new one.");
                return true;
            }

            WorkBench.ConsoleClear();

            WorkBench.ConsoleWriteLine("Repository setup: ");
            WorkBench.ConsoleWriteHighlighted(dataSetType.ToUpper());
            WorkBench.ConsoleWrite($" [{WorkBench.UtcNow.ToShortDateString()} {WorkBench.UtcNow.ToLongTimeString()} UTC]\n");
            WorkBench.ConsoleWriteHighlightedLine("----------------------------------------------------------------------------\n");

            bool appendOnly = !(WorkBench.IsDevelopmentEnvironment || WorkBench.IsIntegrationEnvironment || WorkBench.IsQualityEnvironment);
            bool reseededOnce = false;
            string[] entityNames;

            try
            {
                IsReseeding = true;

                if (force)
                {
                    entityNames = GetEntityNamesFromReseedFiles();
                }
                else
                {
                    dataSetType = WorkBench.IsDevelopmentEnvironment ? "Unit"
                                : WorkBench.IsIntegrationEnvironment ? "Integration"
                                : WorkBench.IsQualityEnvironment ? "System"
                                : WorkBench.IsDemonstrationEnvironment ? "Demonstration"
                                : WorkBench.IsProductionEnvironment ? "Production"
                                : throw new LightException($"Default reseed data type not implemented for `{WorkBench.EnvironmentName}` environment");

                    entityNames = GetEntityNamesFromLightModelTypes();
                }

                foreach (var entityName in entityNames)
                {
                    var config = GetConfigByEntity(entityName);

                    if (force || appendOnly)
                    {
                        if (!appendOnly)
                            await TruncateTableAsync(config.ProjectId, config.DatasetId, entityName);

                        JsonDocument json = JsonDocument.Parse("[]");
                        try
                        {
                            json = GetSeedData(entityName, dataSetType);
                            if (json is null)
                                return reseededOnce;
                        }
                        catch (Exception ex)
                        {
                            WorkBench.ConsoleWriteErrorLine($"Seed data json file '{entityName}' is malformed and could not be parsed");
                            WorkBench.ConsoleWriteHighlightedLine(ex.Message);
                            WorkBench.ConsoleWriteLine();
                            break;
                        }

                        ArrayEnumerator arrayOfObjects;
                        try
                        {
                            arrayOfObjects = json.RootElement.EnumerateArray();
                        }
                        catch
                        {
                            WorkBench.ConsoleWriteLine();
                            WorkBench.ConsoleWriteErrorLine($"Seed data json file '{entityName}' is not a array of json");
                            WorkBench.ConsoleWriteLine();
                            break;
                        }

                        if (arrayOfObjects.Any() &&
                            await InsertDataAsync(arrayOfObjects, config.ProjectId, config.DatasetId, GetEntityType(entityName), dataSetType, appendOnly))
                            reseededOnce = true;
                        else
                        {
                            WorkBench.ConsoleWriteLine();
                            WorkBench.ConsoleWriteHighlightedLine($"Seed data json file '{entityName}' is an empty array");
                            WorkBench.ConsoleWriteLine();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WorkBench.ConsoleWriteLine(e.Message);
                throw;
            }
            finally
            {
                IsReseeding = false;
            }

            return await Task.Run(() => { return reseededOnce; });
        }

        private async Task TruncateTableAsync(string projectId, string datasetId, string entityName)
        {
            string tableName = $"{projectId}.{datasetId}.{entityName.ToLower()}";

            string createCopy = @$"CREATE TABLE {tableName}_copy AS
                                   SELECT *
                                   FROM {tableName}
                                   LIMIT 0";
            string dropOrigin = $"DROP TABLE {tableName}";
            string createOrigin = @$"CREATE TABLE {tableName} AS
                                   SELECT *
                                   FROM {tableName}_copy";
            string dropCopy = $"DROP TABLE {tableName}_copy";

            string query = default;

            WorkBench.ConsoleWriteLine("----------------------------------------------------------------------------");
            WorkBench.ConsoleWriteLine($"[{WorkBench.UtcNow.ToShortDateString()} {WorkBench.UtcNow.ToLongTimeString()} UTC] Starting to truncate table '{tableName}'");

            try
            {
                try
                {
                    await Task.Run(() => clientHandler.Client.ExecuteQuery(query = createCopy, parameters: null));
                }
                catch (GoogleApiException)
                {
                    await Task.Run(() => clientHandler.Client.ExecuteQuery(query = dropCopy, parameters: null));
                    await Task.Run(() => clientHandler.Client.ExecuteQuery(query = createCopy, parameters: null));
                }

                await Task.Run(() => clientHandler.Client.ExecuteQuery(query = dropOrigin, parameters: null));
                await Task.Run(() => clientHandler.Client.ExecuteQuery(query = createOrigin, parameters: null));
                await Task.Run(() => clientHandler.Client.ExecuteQuery(query = dropCopy, parameters: null));
            }
            catch (GoogleApiException e)
            {
                string textError = string.Join("\n", e.Error.Errors.Select(e => e.Message));

                WorkBench.ConsoleWriteErrorLine($"The following query has got an exception from Google BigQuery:\n{query}\n");
                WorkBench.ConsoleWriteHighlightedLine($"---> With errors:\n{textError}\n", e);
                return;
            }

            WorkBench.ConsoleWriteLine($"[{WorkBench.UtcNow.ToShortDateString()} {WorkBench.UtcNow.ToLongTimeString()} UTC] Table TRUNCATED.");
        }

        private async Task<bool> InsertDataAsync(ArrayEnumerator initialData, string projectId, string datasetId, Type entityType, string dataSetType, bool appendOnly)
        {
            if (entityType is null)
            {
                WorkBench.ConsoleWriteErrorLine($"The seed file does not have a corresponding LightModel class. CHECK NAMES AND CASES.");
                return false;
            }

            bool success = true;
            int records = 0;

            WorkBench.ConsoleWriteLine($"[{WorkBench.UtcNow.ToShortDateString()} {WorkBench.UtcNow.ToLongTimeString()} UTC] Found {initialData.Count()} record(s) from dataset '{dataSetType}' to be seeded into entity '{projectId}.{datasetId}.{entityType.Name.ToLower()}'");

            ILightModel entity = default;
            List<string> rows = new();
            JsonElement lastSuccessfulRecord = default;
            try
            {
                foreach (JsonElement record in initialData)
                {
                    lastSuccessfulRecord = record;
                    entity = record.Deserialize(entityType, LightRepositorySerialization.JsonDefault) as ILightModel;
                    rows.Add(entity.ToJsonString());
                    records++;
                }

                var dataset = clientHandler.Client.GetDataset(datasetId);
                TableReference tableRef = dataset.GetTableReference(tableId: entityType.Name.ToLower());

                BigQueryJob loadJob = clientHandler.Client.UploadJson(tableRef, null, rows);

                BigQueryJob completedJob = await clientHandler.Client.PollJobUntilCompletedAsync(loadJob.Reference.JobId);

                if (completedJob.Status?.Errors?.Count > 0)
                    throw new LightException(string.Join("; ", completedJob.Status.Errors.Select(e => e.Message)));
            }
            catch (Exception e)
            {
                WorkBench.ConsoleWriteErrorLine($"JSON failed to deserialize to '{entityType.Name}':");
                WorkBench.ConsoleWriteErrorLine(lastSuccessfulRecord.ToJsonString(true));
                WorkBench.ConsoleWriteErrorLine($"Exception: {e.Message}");
                success = false;
            }

            string successMessage = success ? "SUCCESSFULLY inserted" : "was STOPPED due to database error";
            string recordMessage = records > 1 ? "records" : "record";
            string logMessage = $"Seeding process {successMessage} {records} {recordMessage} from dataset '{dataSetType}' into entity '{projectId}.{datasetId}.{entityType.Name.ToLower()}'";

            if (appendOnly & !success)
            {
                // A 0 record insertion means that another instance (another K8s pod) had already seeded (appended) the dataset.
                // If the dataset was partially inserted it means that it has inconsistences.
                if (records > 0)
                {
                    WorkBench.ConsoleWriteErrorLine(logMessage);
                    WorkBench.ConsoleWriteHighlightedLine($"Check the dataset (seed file) and the data already inserted in the DB.");
                }
                else
                    WorkBench.ConsoleWriteLine($"[{WorkBench.UtcNow.ToShortDateString()} {WorkBench.UtcNow.ToLongTimeString()} UTC] Records had already been seeded. Remove the dataset seed file in next release so to stop seeing this message.");
            }
            else
                WorkBench.ConsoleWriteLine($"[{WorkBench.UtcNow.ToShortDateString()} {WorkBench.UtcNow.ToLongTimeString()} UTC] {logMessage}");

            WorkBench.ConsoleWriteLine();

            return success;
        }

        /// <summary>
        /// Method to run Health Check for BigQuery
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override LightHealth.HealthCheckStatus HealthCheck(string serviceKey, string value)
        {
            try
            {
                return LightHealth.HealthCheckStatus.Healthy;
            }
            catch
            {
                return LightHealth.HealthCheckStatus.Unhealthy;
            }
        }
        #endregion

        #region OLTP methods not implemented
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<T> GetByIdAsync<T>(string id, string partitionKey = null)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<T> UpdateAsync<T>(T model)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<IEnumerable<T>> UpdateAsync<T>(List<T> listModels)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<ILightAttachment> SaveAttachmentAsync<T>(string entityId, string fileName, Stream attachment, string partitionKey = null)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for managing attachments.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<ILightAttachment> ReplaceAttachmentAsync<T>(string entityId, string fileName, Stream attachment, string partitionKey = null)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for managing attachments.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task AppendToAttachmentAsync<T>(string entityId, string fileName, Stream block, int blockNumber, string partitionKey = null)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for managing attachments.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<ILightAttachment> GetAttachmentAsync<T>(string entityId, string fileName, string partitionKey = null) 
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for managing attachments.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<Stream> GetAttachmentBlockAsync<T>(string entityId, string fileName, int blockNumber, string partitionKey = null)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for managing attachments.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<ILightAttachment> DeleteAttachmentAsync<T>(string entityId, string fileName, string partitionKey = null)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for managing attachments.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<long> CountAsync<T>()
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<long> CountAsync<T>(Expression<Func<T, bool>> filter)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<T> DeleteAsync<T>(string entityId, string partitionKey = null)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override IEnumerable<T> Get<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy = null, bool descending = false)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override IEnumerable<T> GetAll<T>(Expression<Func<T, object>> orderBy = null, bool descending = false)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override async Task<ILightPaging<T>> GetByPageAsync<T>(Expression<Func<T, bool>> filter, ILightPagingParms pagingParms, Expression<Func<T, object>> orderBy = null, bool descending = false)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }

        /// <summary>
        /// OLTP method NOT IMPLEMENTED - Do not use with BigQuery repository
        /// </summary>
        public override void SetMediaStorage(ILightMediaStorage mediaStorage)
        {
            throw new NotImplementedException("BigQuery is not an OLAP and thus is not fitted for row level operations.");
        }
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        #endregion
    }
}