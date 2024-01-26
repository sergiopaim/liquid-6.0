using Liquid.Base;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Runtime;
using Liquid.Runtime.Telemetry;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Liquid.Repository
{
    /// <summary>
    /// The Cosmos DB LightRepository
    /// This class is a wrapper around CosmosDBRepository setting up the client to make calls to Cosmos DB. 
    /// </summary>
    public abstract class LightRepository : ILightRepository
    {
        /// <summary>
        /// Disable Validation of Model
        /// </summary>
        public bool IgnoreValidate { get; set; }
        /// <summary>
        /// Set Media Storage for attachments
        /// </summary>
        /// <param name="mediaStorage">The ILightMediaStorage cartridge</param>
        public abstract void SetMediaStorage(ILightMediaStorage mediaStorage);
        /// <summary>
        /// Indicates whether setup is happening
        /// </summary>
        protected bool Setup { get; set; }
        /// <summary>
        /// Wrapper when change the collection name
        /// </summary>
        protected Action<string, string> Action { get; set; }

        private static readonly Dictionary<Type, bool> analyticalSources = new();

        /// <summary>
        /// Initialize the repository
        /// </summary>
        public abstract void Initialize();
        /// <summary>
        /// Check if Cosmos DB Repository has been correcty setup 
        /// </summary>
        public void CheckSetup<T>()
        {
            if (!Setup)
                throw new BadRepositoryInitializationLightException(GetType().Name);
        }

        /// <summary>
        /// Method to validate and check rules
        /// </summary>
        /// <param name="model"></param>
        protected void ValidateAndCheck(dynamic model)
        {
            if (!IgnoreValidate)
            {
                Dictionary<string, object[]> _modelValidationErrors = new();
                Validate(model, _modelValidationErrors);
                if (_modelValidationErrors.Count > 0)
                    throw new InvalidModelLightException(_modelValidationErrors);
            }
        }

        /// <summary>
        /// Method to validate and check rules
        /// </summary>
        /// <param name="model"></param>
        private void ValidateAndCheckList(dynamic model)
        {
            if (!IgnoreValidate)
                foreach (var x in model)
                    ValidateAndCheck(x);
        }

        /// <summary>
        /// Evaluates the validation rules of the Model class (and its agregrates) and raise errors accordingly.
        /// </summary>
        /// <param name="model">The Model to input validation</param>
        /// <param name="modelValidationErrors">List of errors</param>
        private void Validate(dynamic model, Dictionary<string, object[]> modelValidationErrors)
        {
            model.InputErrors = modelValidationErrors;
            model.Validate();
            ResultValidation result = model.Validator.Validate(model);
            if (!result.IsValid)
                foreach (var error in result.Errors)
                {
                    if (!modelValidationErrors.ContainsKey(error.Key))
                        modelValidationErrors.TryAdd(error.Key, new object[] { error.Value } );
                }
            //By reflection, browse viewModel by identifying all attributes and lists for validation.  
            foreach (PropertyInfo fieldInfo in model.GetType().GetProperties())
            {
                dynamic child = fieldInfo.GetValue(model);

                //When the child is a list, validate each of its members  
                if (child is IList)
                {
                    var children = (IList)fieldInfo.GetValue(model);
                    foreach (var item in children)
                    {
                        if (item is not null)
                        {
                            var baseType = item.GetType().BaseType;
                            //Check, if the property is a Light ViewModel, only they will validation Lights ViewModel
                            if ((baseType != typeof(object))
                                 && (baseType != typeof(System.ValueType))
                                 && (baseType.IsGenericType &&
                                    (baseType.GetGenericTypeDefinition() == typeof(LightModel<>) ||
                                     baseType.GetGenericTypeDefinition() == typeof(LightOptimisticModel<>) ||
                                     baseType.GetGenericTypeDefinition() == typeof(LightDataEventModel<>) ||
                                     baseType.GetGenericTypeDefinition() == typeof(LightValueObject<>))))
                            {
                                dynamic obj = item;
                                //Check, if the attribute is null for verification of the type.
                                if (obj is not null)
                                    Validate(obj, modelValidationErrors);
                            }
                        }
                    }
                }
                else
                {
                    //Otherwise, validate the very child once. 
                    if (child is not null)
                    {
                        var baseType = child.GetType().BaseType;
                        //Check, if the property is a Light ViewModel, only they will validation Lights ViewModel
                        if ((baseType != typeof(object))
                           && (baseType != typeof(System.ValueType))
                           && (baseType.IsGenericType &&
                              (baseType.GetGenericTypeDefinition() == typeof(LightModel<>) ||
                               baseType.GetGenericTypeDefinition() == typeof(LightOptimisticModel<>) ||
                               baseType.GetGenericTypeDefinition() == typeof(LightDataEventModel<>) ||
                               baseType.GetGenericTypeDefinition() == typeof(LightValueObject<>))))
                        {
                            Validate(child, modelValidationErrors);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Add data
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="model">Domain Model</param>
        /// <returns>Returns the result of the operation</returns>
        public virtual Task<T> AddAsync<T>(T model) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            ValidateAndCheck(model);

            return Task.FromResult<T>(model);
        }

        /// <summary>
        /// Update data
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="model">Domain Model</param>
        /// <returns>Returns the result of the operation</returns>
        public virtual Task<T> UpdateAsync<T>(T model) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            ValidateAndCheck(model);

            return Task.FromResult<T>(model);
        }
        /// <summary>
        /// Add data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listModels"></param>
        /// <returns>Returns the result of the operation</returns>
        public virtual Task<IEnumerable<T>> AddAsync<T>(List<T> listModels) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            ValidateAndCheckList(listModels ?? new List<T>());

            return Task.FromResult<IEnumerable<T>>(listModels);
        }

        /// <summary>
        /// Update data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listModels"></param>
        /// <returns>Returns the result of the operation</returns>
        public virtual Task<IEnumerable<T>> UpdateAsync<T>(List<T> listModels) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            ValidateAndCheckList(listModels ?? new List<T>());

            return Task.FromResult<IEnumerable<T>>(listModels);
        }

        /// <summary>
        /// Returns the result of the operation
        /// </summary>
        /// <param name="entityId">Id of the document</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Returns the result of the operation</returns>
        public virtual Task<T> DeleteAsync<T>(string entityId, string partitionKey = null) where T : ILightModel, new()
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            return Task.FromResult(new T());
        }

        /// <summary>
        /// Saves the attachment
        /// Calls the base class because there may be some generic behavior in it
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="attachment">the attachment</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns></returns>
        public virtual Task<ILightAttachment> SaveAttachmentAsync<T>(string entityId, string fileName, Stream attachment, string partitionKey = null) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            return Task.FromResult<ILightAttachment>(new LightAttachment());
        }

        /// <summary>
        /// Migrate attachments to AutoPartitionKey
        /// </summary>
        public virtual async Task MigrateAttachmentsToAutoPartitionAsync<T>(T entity) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            await Task.FromResult<object>(null);
        }

        /// <summary>
        /// Replaces a existing attachment
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="attachment">the attachment</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns></returns>
        public virtual Task<ILightAttachment> ReplaceAttachmentAsync<T>(string entityId, string fileName, Stream attachment, string partitionKey = null) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            return Task.FromResult<ILightAttachment>(new LightAttachment());
        }

        /// <summary>
        /// Appends or replaces a block to the attachment
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="block">the attachment block</param>
        /// <param name="blockNumber">The (positive) id number of the block </param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Indication of success</returns>
        public virtual Task AppendToAttachmentAsync<T>(string entityId, string fileName, Stream block, int blockNumber, string partitionKey = null) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            return Task.FromResult<object>(null);
        }

        /// <summary>
        /// List the Attachments by id
        /// Calls the base class because there may be some generic behavior in it
        /// </summary>
        /// <param name="entityId">attatchment id</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>A list of attachments</returns>
        public virtual Task<IEnumerable<ILightAttachment>> ListAttachmentsByIdAsync<T>(string entityId, string partitionKey = null) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
                              //
            return Task.FromResult<IEnumerable<ILightAttachment>>(new List<ILightAttachment>());
        }

        /// <summary>
        /// Get the attatchment by id and filename
        /// Calls the base class because there may be some generic behavior in it
        /// </summary>
        /// <param name="entityId">attachment id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>The attachment type LightAttachment</returns>
        public virtual Task<ILightAttachment> GetAttachmentAsync<T>(string entityId, string fileName, string partitionKey = null) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            return Task.FromResult<ILightAttachment>(new LightAttachment());
        }

        /// <summary>
        /// Appends or replaces a block to the attachment
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="blockNumber">The (positive) id number of the block </param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Indication of success</returns>
        public virtual Task<Stream> GetAttachmentBlockAsync<T>(string entityId, string fileName, int blockNumber, string partitionKey = null) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            return Task.FromResult<Stream>(new MemoryStream());
        }

        /// <summary>
        /// Deletes the especific attachment
        /// </summary>
        /// <param name="entityId">Id of the attachment</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Returns the result of the operation</returns>
        public virtual Task<ILightAttachment> DeleteAttachmentAsync<T>(string entityId, string fileName, string partitionKey = null) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix

            return Task.FromResult<ILightAttachment>(null);
        }

        /// <summary>
        /// Calls the base class because there may be some generic behavior in it
        /// Counts the quantity of documents
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <returns>Quantity of Documents</returns>
        public virtual Task<long> CountAsync<T>() where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            long ret = 0;
            return Task.FromResult(ret);
        }

        /// <summary>
        /// Calls the base class because there may be some generic behavior in it
        /// Counts the quantity of documents
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="filter">lambda expression to filter</param>
        /// <returns>Quantity of Document</returns>
        public virtual Task<long> CountAsync<T>(Expression<Func<T, bool>> filter) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            long ret = 0;
            return Task.FromResult(ret);
        }

        /// <summary>
        /// Get data by sql query
        /// </summary>
        /// <param name="query">SQL statement with parameterized values</param>
        /// <returns></returns>
        public virtual IEnumerable<T> Query<T>(string query)
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            return new List<T>();
        }
        /// <summary>
        /// Expand general SQL prototypes as SQL sintax specific for the repository database
        /// </summary>
        /// <param name="query">SQL DML commands</param>
        /// <returns></returns>
        public abstract string ExpandQuery(string query);
        /// <summary>
        /// Expand general DML prototypes as DLM sintax specific for the repository database
        /// </summary>
        /// <param name="command">SQL DML commands</param>
        /// <returns></returns>
        public abstract string ExpandCommand(string command);

        /// <summary>
        /// Get data by sql query
        /// </summary>
        /// <param name="query">SQL statement with parameterized values</param>
        /// <returns></returns>
        public virtual Task<IEnumerable<T>> QueryAsync<T>(string query)
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            return Task.FromResult<IEnumerable<T>>(new List<T>());
        }

        /// <summary>
        /// Get data by sql query in paginated form
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="query">SQL statement with parameterized values</param>
        /// <param name="pagingParms">Paging parameters (if ommited, default is first page and 20 itemsper page)</param>
        /// <returns>Paginated list of entities</returns>
        public virtual Task<ILightPaging<T>> QueryByPageAsync<T>(string query, ILightPagingParms pagingParms) 
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            return Task.FromResult<ILightPaging<T>>(new LightPaging<T>());
        }

        /// <summary>
        /// Submit DML commands to database
        /// </summary>
        /// <param name="command">SQL DML commands</param>
        /// <returns></returns>
        public virtual Task CommandAsync(string command)
        {
            CheckSetup<ILightDomain>();  //Prevents direct instantiation of a repository without a sufix
            return Task.FromResult(new List<ILightDomain>());
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
        public virtual string SlidingUpsertSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            return default;
        }

        /// <summary>
        /// Mount a SQL query for agregating delete data events by a time sliding window
        /// </summary>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        public virtual string SlidingDeleteSql(Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            return default;
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
        public virtual string SlidingDeleteSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            return default;
        }
        /// <summary>
        /// Mount a SQL query for agregating all data events by a time sliding window
        /// </summary>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        public virtual string SlidingAllSql(Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            return default;
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
        public virtual string SlidingAllSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime)
        {
            return default;
        }
        /// <summary>
        /// Get the document by id
        /// Calls the base class because there may be some generic behavior in it
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>generic type of document</returns>
        public virtual Task<T> GetByIdAsync<T>(string entityId, string partitionKey = null) where T : ILightModel, new()
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix 
            return Task.FromResult<T>(new T());
        }

        /// <summary>
        /// Gets specific LightDomain entities stored in the repository 
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="filter">lambda expression to be use in where clause of the query</param>
        /// <param name="orderBy">lambda expression to be use in order clause of the query</param>
        /// <param name="descending">True when the order is descending</param>
        /// <returns>List of LightDomain entities</returns>
        public virtual IEnumerable<T> Get<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy, bool descending = false) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            return new List<T>().AsEnumerable();
        }

        /// <summary>
        /// Gets all LightDomain entities stored in the repository 
        /// </summary>
        /// <typeparam name="T">LightDomain type</typeparam>
        /// <param name="orderBy">lambda expression to be use in order clause of the query</param>
        /// <param name="descending">True when the order is descending</param>
        /// <returns>List of LightDomain entities</returns>
        public virtual IEnumerable<T> GetAll<T>(Expression<Func<T, object>> orderBy, bool descending = false) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            return new List<T>().AsEnumerable();
        }

        /// <summary>
        /// Gets specific LightDomain entities stored in the repository in paginated form
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="filter">filter to fetch the object</param>
        /// <param name="pagingParms">Paging parameters (if ommited, default is first page and 20 itemsper page)</param>
        /// <param name="orderBy">lambda expression to be use in order clause of the query</param>
        /// <param name="descending">True when the order is descending</param>
        /// <returns>Paginated list of LightDomain entities</returns>
        public virtual Task<ILightPaging<T>> GetByPageAsync<T>(Expression<Func<T, bool>> filter, ILightPagingParms pagingParms, Expression<Func<T, object>> orderBy = null, bool descending = false) where T : ILightModel
        {
            CheckSetup<T>();  //Prevents direct instantiation of a repository without a sufix
            return Task.FromResult<ILightPaging<T>>(new LightPaging<T>());
        }

        /// <summary>
        /// Reset all data in database
        /// </summary>
        /// <param name="dataSetType">Type of the dataset (e.g. Unit)</param>
        /// <param name="force">Indication of weather to force the reseed (recreating the database) or not</param>
        /// <returns></returns>
        public abstract Task<bool> ReseedDataAsync(string dataSetType = "Unit", bool force = false);

        /// <summary>
        /// Get the list of entities from seed files
        /// </summary>
        /// <returns></returns>
        protected static string[] GetEntityNamesFromReseedFiles()
        {
            return StubHandler.GetEntitysWithFilesToSeed();
        }

        /// <summary>
        /// Get the list of entities from lightModel classes
        /// </summary>
        /// <returns></returns>
        protected static string[] GetEntityNamesFromLightModelTypes()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return (from assembly in assemblies
                    where !assembly.IsDynamic
                    from type in assembly.ExportedTypes
                    where ((type.BaseType?.Name.StartsWith("LightModel") == true ||
                           type.BaseType?.Name.StartsWith("LightOptimisticModel") == true ||
                           type.BaseType?.Name.StartsWith("LightDataEventModel") == true) &&
                           !type.Name.StartsWith("Light"))
                    select type.Name).ToArray();
        }

        /// <summary>
        /// Checks if the lightModel type has the AnalyticalSource attribute
        /// </summary>
        /// <returns></returns>
        protected static bool IsAnalyticalSource(string typeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return (from assembly in assemblies
                    where !assembly.IsDynamic
                    from type in assembly.ExportedTypes
                    where ((type.BaseType?.Name.StartsWith("LightModel") == true ||
                            type.BaseType?.Name.StartsWith("LightOptimisticModel") == true ||
                            type.BaseType?.Name.StartsWith("LightDataEventModel") == true) &&
                            type.Name == typeName &&
                            !type.Name.StartsWith("Light"))
                    select Attribute.IsDefined(type, typeof(AnalyticalSourceAttribute))).FirstOrDefault();
        }

        /// <summary>
        /// Checks if the lightModel type has the AnalyticalSource attribute
        /// </summary>
        /// <returns></returns>
        protected static bool IsAnalyticalSource<T>()
        {
            if (!analyticalSources.TryGetValue(typeof(T), out bool isAnalyticalSource))
            {
                isAnalyticalSource = IsAnalyticalSource(typeof(T).Name);
                analyticalSources.TryAdd(typeof(T), isAnalyticalSource);
            }
         
            return isAnalyticalSource;
        }

        /// <summary>
        /// Indicates wheter model entity type is a script defined one
        /// </summary>
        /// <param name="modelEntityTypeName">The name of the LightModel class</param>
        /// <returns>True if it is a script defined source</returns>
        protected static bool IsScriptDefined(string modelEntityTypeName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            var scriptDefinedEntity = (from assembly in assemblies
                                       where !assembly.IsDynamic
                                       from type in assembly.ExportedTypes
                                       where (type.BaseType?.Name.StartsWith("LightModel") == true ||
                                              type.BaseType?.Name.StartsWith("LightOptimisticModel") == true ||
                                              type.BaseType?.Name.StartsWith("LightDataEventModel") == true) &&
                                              type.Name == modelEntityTypeName &&
                                              Attribute.IsDefined(type, typeof(ScriptDefinedAttribute))
                                       select type).FirstOrDefault();

            return scriptDefinedEntity is not null;
        }

        /// <summary>
        /// Gets the type of a LightModelEntity
        /// </summary>
        /// <param name="entityName"></param>
        /// <returns></returns>
        protected static Type GetEntityType(string entityName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return (from assembly in assemblies
                    where !assembly.IsDynamic
                    from type in assembly.ExportedTypes
                    where type.Name == entityName &&
                          (type.BaseType?.Name.StartsWith("LightModel") == true ||
                           type.BaseType?.Name.StartsWith("LightOptimisticModel") == true ||
                           type.BaseType?.Name.StartsWith("LightDataEventModel") == true)
                    select type).FirstOrDefault();
        }

        /// <summary>
        /// Get seed data file 
        /// </summary>
        /// <param name="dataFileName">name of data file</param>
        /// <param name="dataSetType">Type of dataset (Unit, Integration, System, etc.)</param>
        /// <returns></returns>
        public virtual JsonDocument GetSeedData(string dataFileName, string dataSetType)
        {
            dynamic data = StubHandler.GetStubData<JsonDocument>(dataFileName, suffix: dataSetType, subDirectory: "Seed");
            return data;
        }

        /// <summary>
        /// Abstract method to force inherithed class to implement Health Check Method.
        /// </summary>
        /// <param name="serviceKey"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract LightHealth.HealthCheckStatus HealthCheck(string serviceKey, string value);

        /// <summary>
        /// While in abstract layer, this method calls the LightEvent per CRUD operations in repository
        /// </summary>
        /// <typeparam name="T">The LightModel type</typeparam>
        /// <param name="entityName">The name of data entity for DataLake ingestion</param>
        /// <param name="model">The LightModel instance</param>
        /// <param name="dataEventCommandCode">The data manipulation operation code from LightDataEventCMD type</param>
        /// <param name="ingestedAt">The dateTime the data was ingested (optional -> default DateTime.Now)</param>
        /// <returns></returns>
        public Task SendModelAsDataEventAsync<T>(string entityName, T model, string dataEventCommandCode, DateTime? ingestedAt = null) where T : ILightModel
        {
            return SendDataEvent(entityName, model, LightDataEventCMD.OfCode(dataEventCommandCode), ingestedAt ?? DateTime.MinValue);
        }

        /// <summary>
        /// While in abstract layer, this method calls the LightEvent per CRUD operations in repository
        /// </summary>
        /// <typeparam name="T">The LightModel type</typeparam>
        /// <param name="entityName">The name of data entity for DataLake ingestion</param>
        /// <param name="viewModel">Name of the database entity (with prefix)</param>
        /// <param name="dataEventCommandCode">The data manipulation operation code from LightDataEventCMD type</param>
        /// <param name="ingestedAt">The dateTime the data was ingested (optional -> default DateTime.Now)</param>
        /// <returns></returns>
        public Task SendViewModelAsDataEventAsync<T>(string entityName, T viewModel, string dataEventCommandCode, DateTime? ingestedAt = null) where T : ILightViewModel
        {
            return SendDataEvent(entityName, viewModel, LightDataEventCMD.OfCode(dataEventCommandCode), ingestedAt ?? DateTime.MinValue);
        }

        /// <summary>
        /// While in abstract layer, this method calls the LightEvent per CRUD operations in repository
        /// </summary>
        /// <typeparam name="T">The LightModel type</typeparam>
        /// <param name="entityName">Name of the database entity (with prefix)</param>
        /// <param name="model">The LightModel instance</param>
        /// <param name="dataEventCommand">The data manipulation operation</param>
        /// <param name="ingestedAt">The dateTime the data was ingested (optional -> default DateTime.Now)</param>
        /// <returns></returns>
        public static Task SendModelAsDataEventAsync<T>(string entityName, T model, LightDataEventCMD dataEventCommand, DateTime? ingestedAt = null) where T : ILightModel
        {
            return SendDataEvent(entityName, model, dataEventCommand, ingestedAt ?? DateTime.MinValue);
        }

        /// <summary>
        /// Checks if a type is a LightOptimistic subtype
        /// </summary>
        /// <param name="toCheck">The type to check</param>
        /// <returns></returns>
        protected static bool IsOptimisticConcurrency(Type toCheck)
        {
            while (toCheck is not null && toCheck != typeof(object))
            {
                var current = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (typeof(LightOptimisticModel<>) == current)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        private static Task SendDataEvent<T>(string entityName, T data, LightDataEventCMD dataEventCommand, DateTime ingestedAt) 
        {
            if (string.IsNullOrEmpty(entityName))
                throw new LightException("Entity name can not be empty");

            try
            {
                var Bus = (MessageBrokerWrapper)WorkBench.GetRegisteredService(WorkBenchServiceType.DataHub);

                if (Bus is not null)
                {
                    var dataEvent = new LightDataEvent<T>
                    {
                        EntityName = entityName.ToLower(),
                        CommandType = dataEventCommand?.Code,
                        Payload = data,
                        IngestedAt = ingestedAt != DateTime.MinValue
                                         ? ingestedAt.ToUniversalTime()
                                         : WorkBench.UtcNow
                    };

                    Bus.SendToTopicAsync(dataEvent);
                }
                else
                    throw new LightException("Event hub for data events is not defined. Call WorkBench.UseDataHub<BUS_SERVICE_CLASS>() on Startup.cs");
            }
            catch (LightException)
            {
                throw;
            }
            catch (Exception e)
            {
                ((LightTelemetry)WorkBench.Telemetry).TrackException(e);
            }

            return Task.FromResult<T>(default);
        }

        /// <summary>
        /// Returns the partition key path for the lightModel type 
        /// </summary>
        /// <param name="modelName">The name of a lightModel type</param>
        /// <returns>The partition key path in camelCase format</returns>
        protected static string GetPartitionPath(string modelName)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var type = (from assembly in assemblies
                        where !assembly.IsDynamic
                        from t in assembly.ExportedTypes
                        where t.Name == modelName &&
                              (t.BaseType?.Name.StartsWith("LightModel") == true ||
                               t.BaseType?.Name.StartsWith("LightOptimisticModel") == true ||
                               t.BaseType?.Name.StartsWith("LightDataEventModel") == true)
                        select t).FirstOrDefault();

            

            var autoPkProp = Attribute.GetCustomAttribute(type, typeof(AutoPartitionKeyAttribute));
            var pkProp = type.GetProperties()
                             .Where(p => Attribute.IsDefined(p, typeof(PartitionKeyAttribute))).FirstOrDefault();

            if (pkProp is not null && autoPkProp is not null)
                throw new LightException($"The should be only one property defined as partitionKey. Check {modelName} for PartitionKey or AutoPartitionKey attributes");


            string partitionKey = autoPkProp is null
                                     ? pkProp?.Name ?? "id"
                                     : "_apk";

            //Always forces camelCase
            return "/" + partitionKey[..1].ToLower() + partitionKey[1..];
        }

        /// <summary>
        /// Generates a partition key derived from the id
        /// </summary>
        /// <param name="id">The id of the object</param>
        /// <param name="numPartitions">The number of partitions the key should be picked among</param>
        /// <returns></returns>
        public static string GeneratePartitionKey(string id, long numPartitions = 1000)
        {
            using var sha256 = SHA256.Create();
            // Compute the SHA-256 hash of the GUID as a byte array
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(id));
            // Convert the hash bytes to a BigInteger to ensure we can handle the full 256-bit hash value
            BigInteger hashInt = new(hashBytes);
            // Ensure that the partition value is positive by taking the absolute value and casting to ulong
            ulong partition = (ulong)(BigInteger.Abs(hashInt) % numPartitions);
            // Convert the partition to a string and return it as the partition key
            return partition.ToString();
        }

        /// <summary>
        /// Calculates the AutoPartition value if the LightModel has AutoPartitionKey attribute
        /// </summary>
        /// <typeparam name="T">ILightModel type</typeparam>
        /// <param name="model">ILightModel object</param>
        protected static void SetIfAutoPartition<T>(T model) where T : ILightModel
        {
            SetIfAutoPartition(typeof(T), model);
        }

        /// <summary>
        /// Calculates the AutoPartition value if the LightModel has AutoPartitionKey attribute
        /// </summary>
        /// <param name="entityType">ILightModel type</param>
        /// <param name="model">ILightModel object</param>
        protected static void SetIfAutoPartition(Type entityType, object model)
        {
            var autoPk = Attribute.GetCustomAttribute(entityType, typeof(AutoPartitionKeyAttribute)) as AutoPartitionKeyAttribute;
            if (autoPk is not null)
                (model as ILightModel).AutoPartition = GeneratePartitionKey((model as ILightModel).Id, autoPk.NumberOfPartitions);
        }

        /// <summary>
        /// Gets the default partition for the ILightModel type
        /// </summary>
        /// <typeparam name="T">ILightModel type</typeparam>
        /// <param name="entityId">The id of the entity</param>
        /// <returns></returns>
        protected static string GetDefaultPartition<T>(string entityId) where T : ILightModel
        {
            var autoPk = Attribute.GetCustomAttribute(typeof(T), typeof(AutoPartitionKeyAttribute)) as AutoPartitionKeyAttribute;
            if (autoPk is not null)
                return GeneratePartitionKey(entityId, autoPk.NumberOfPartitions);
            else
                return entityId;
        }

        /// <summary>
        /// Generates an short Id if the 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <returns></returns>
        protected static bool SetIfShortId<T>(T model) where T : ILightModel
        {
            bool isShortId = Attribute.IsDefined(typeof(T), typeof(ShortnerIdAttribute));
            if (isShortId && string.IsNullOrEmpty((model as ILightModel).Id))
                (model as ILightModel).Id = Base62.Encode();
            return isShortId;
        }
    }

    /// <summary>
    /// An indication whether a LightModel property is used as partition key of the LightRepository
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class PartitionKeyAttribute : Attribute { }

    /// <summary>
    /// An indication whether a LightModel uses the AutoPartition property as partition key 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AutoPartitionKeyAttribute : Attribute 
    {
        /// <summary>
        /// The number of partitions among with the key should be picked
        /// </summary>
        public long NumberOfPartitions { get; set; } = 1000;
    }

    /// <summary>
    /// An indication whether a LightModel class is a source data to analytical process of the DataLake
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class AnalyticalSourceAttribute : Attribute { }

    /// <summary>
    /// An indication whether a LightModel class has ids used to compose short URLs
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ShortnerIdAttribute : Attribute { }

    /// <summary>
    /// Entity specific configurations for the repository
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class RepositoryConfigAttribute : Attribute
    {
        /// <summary>
        /// The repository configuration suffix. If omitted, the default repository configuration will be considered
        /// </summary>
        public string Suffix { get; set; }
    }

    /// <summary>
    /// An indication whether a LightModel class is created only through external scripts
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ScriptDefinedAttribute : Attribute { }
}
