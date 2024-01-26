using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Liquid.Interfaces
{
    /// <summary>
    /// Public interface for all NoSql Database Implementations
    /// </summary>
    public interface ILightRepository : IWorkBenchHealthCheck
    {
        /// <summary>
        /// Migrate attachments to AutoPartitionKey
        /// </summary>
        Task MigrateAttachmentsToAutoPartitionAsync<T>(T entity) where T : ILightModel;
        /// <summary>
        /// Set Media Storage for attachments
        /// </summary>
        /// <param name="mediaStorage">The ILightMediaStorage cartridge</param>
        void SetMediaStorage(ILightMediaStorage mediaStorage);
        /// <summary>
        /// Add data
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="model">Domain Model</param>
        /// <returns>Returns the result of the operation</returns>
        Task<T> AddAsync<T>(T model) where T : ILightModel;
        /// <summary>
        /// Add data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listModels"></param>
        /// <returns>Returns the result of the operation</returns>
        Task<IEnumerable<T>> AddAsync<T>(List<T> listModels) where T : ILightModel;
        /// <summary>
        /// Update data
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="model">Domain Model</param>
        /// <returns>Returns the result of the operation</returns>
        Task<T> UpdateAsync<T>(T model) where T : ILightModel;
        /// <summary>
        /// Update data
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="listModels"></param>
        /// <returns>Returns the result of the operation</returns>
        Task<IEnumerable<T>> UpdateAsync<T>(List<T> listModels) where T : ILightModel;
        /// <summary>
        /// Saves the attachment
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="attachment">the attachment</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns></returns>
        Task<ILightAttachment> SaveAttachmentAsync<T>(string entityId, string fileName, Stream attachment, string partitionKey= null) where T : ILightModel;
        /// <summary>
        /// Replaces a existing attachment
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="attachment">the attachment</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns></returns>
        Task<ILightAttachment> ReplaceAttachmentAsync<T>(string entityId, string fileName, Stream attachment, string partitionKey = null) where T : ILightModel;
        /// <summary>
        /// Appends or replaces a block to the attachment
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="block">the attachment block</param>
        /// <param name="blockNumber">The (positive) id number of the block </param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Indication of success</returns>
        Task AppendToAttachmentAsync<T>(string entityId, string fileName, Stream block, int blockNumber, string partitionKey= null) where T : ILightModel;
        /// <summary>
        /// List the Attachments by id
        /// </summary>
        /// <param name="entityId">attatchment id</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>A list of attachments</returns>
        Task<IEnumerable<ILightAttachment>> ListAttachmentsByIdAsync<T>(string entityId, string partitionKey= null) where T : ILightModel;
        /// <summary>
        /// Get the attatchment by id and filename
        /// </summary>
        /// <param name="entityId">attachment id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>The attachment type LightAttachment</returns>
        Task<ILightAttachment> GetAttachmentAsync<T>(string entityId, string fileName, string partitionKey= null) where T : ILightModel;
        /// <summary>
        /// Appends or replaces a block to the attachment
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="blockNumber">The (positive) id number of the block </param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Indication of success</returns>
        Task<Stream> GetAttachmentBlockAsync<T>(string entityId, string fileName, int blockNumber, string partitionKey = null) where T : ILightModel;
        /// <summary>
        /// Deletes the especific attachment
        /// </summary>
        /// <param name="entityId">Id of the attachment</param>
        /// <param name="fileName">name of the file</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Returns the result of the operation</returns>
        Task<ILightAttachment> DeleteAttachmentAsync<T>(string entityId, string fileName, string partitionKey= null) where T : ILightModel;
        /// <summary>
        /// Returns the result of the operation
        /// </summary>
        /// <param name="entityId">Id of the document</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>Returns the result of the operation</returns>
        Task<T> DeleteAsync<T>(string entityId, string partitionKey= null) where T : ILightModel, new();
        /// <summary>
        /// Counts the quantity of documents
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <returns>Quantity of Documents</returns>
        Task<long> CountAsync<T>() where T : ILightModel;
        /// <summary>
        /// Counts the quantity of documents
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="filter">lambda expression to filter</param>
        /// <returns>Quantity of Document</returns>
        Task<long> CountAsync<T>(Expression<Func<T, bool>> filter) where T : ILightModel;

        /// <summary>
        /// Expand general SQL prototypes as SQL sintax specific for the repository database
        /// </summary>
        /// <param name="query">SQL DML commands</param>
        /// <returns></returns>
        string ExpandQuery(string query);
        /// <summary>
        /// Get data by sql query
        /// </summary>
        /// <param name="query">SQL statement with parameterized values</param>
        /// <returns></returns>
        IEnumerable<T> Query<T>(string query);
        /// <summary>
        /// Get data by sql query
        /// </summary>
        /// <param name="query">SQL statement with parameterized values</param>
        /// <returns></returns>
        Task<IEnumerable<T>> QueryAsync<T>(string query);
        /// <summary>
        /// Get data by sql query in paginated form
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="query">SQL statement with parameterized values</param>
        /// <param name="pagingParms">Paging parameters (if ommited, default is first page and 20 itemsper page)</param>
        /// <returns>Paginated list of entities</returns>
        Task<ILightPaging<T>> QueryByPageAsync<T>(string query, ILightPagingParms pagingParms);
        /// <summary>
        /// Expand general DML prototypes as DLM sintax specific for the repository database
        /// </summary>
        /// <param name="command">SQL DML commands</param>
        /// <returns></returns>
        string ExpandCommand(string command);
        /// <summary>
        /// Submit DML commands to database
        /// </summary>
        /// <param name="command">SQL DML commands</param>
        /// <returns></returns>
        Task CommandAsync(string command);
        /// <summary>
        /// Mount a SQL query for agregating insert and update data events by a time sliding window
        /// </summary>
        /// <param name="selectedColumns">List of projected columns from the event agregation</param>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        string SlidingUpsertSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime);
        /// <summary>
        /// Mount a SQL query for agregating delete data events by a time sliding window
        /// </summary>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        string SlidingDeleteSql(Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime);
        /// <summary>
        /// Mount a SQL query for agregating delete data events by a time sliding window
        /// </summary>
        /// <param name="selectedColumns">List of projected columns from the event agregation</param>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        string SlidingDeleteSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime);
        /// <summary>
        /// Mount a SQL query for agregating all data events by a time sliding window
        /// </summary>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        string SlidingAllSql(Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime);
        /// <summary>
        /// Mount a SQL query for agregating all data events by a time sliding window
        /// </summary>
        /// <param name="selectedColumns">List of projected columns from the event agregation</param>
        /// <param name="eventTable">The LightDataEventModel type related to the DB table/DB container</param>
        /// <param name="partitionColumns">The columns for partitioning the slide window agregation</param>
        /// <param name="fromDateTime">The period starting for filtering the date and time the events were ingested</param>
        /// <param name="toDateTime">The period ending for filtering the date and time the events were ingested</param>
        /// <returns></returns>
        string SlidingAllSql(string selectedColumns, Type eventTable, string partitionColumns, DateTime fromDateTime, DateTime toDateTime);
        /// <summary>
        /// Get the document by id
        /// </summary>
        /// <param name="entityId">document id</param>
        /// <param name="partitionKey">the partition key of the repository. If ommited, the document Id is used instead</param>
        /// <returns>generic type of document</returns>
        Task<T> GetByIdAsync<T>(string entityId, string partitionKey= null) where T : ILightModel, new();
        /// <summary>
        /// While in abstract layer, this method calls the LightEvent per CRUD operations in repository
        /// </summary>
        /// <typeparam name="T">The LightModel type</typeparam>
        /// <param name="entityName">The name of data entity for DataLake ingestion</param>
        /// <param name="model">The LightModel instance</param>
        /// <param name="dataEventCommandCode">The data manipulation operation code from LightDataEventCMD type</param>
        /// <param name="ingestedAt">The dateTime the data was ingested (optional -> default DateTime.Now)</param>
        /// <returns></returns>
        Task SendModelAsDataEventAsync<T>(string entityName, T model, string dataEventCommandCode, DateTime? ingestedAt = null) where T : ILightModel;
        /// <summary>
        /// While in abstract layer, this method calls the LightEvent per CRUD operations in repository
        /// </summary>
        /// <typeparam name="T">The LightViewModel type</typeparam>
        /// <param name="entityName">The name of data entity for DataLake ingestion</param>
        /// <param name="viewModel">The LightViewModel instance</param>
        /// <param name="dataEventCommandCode">The data manipulation operation code from LightDataEventCMD type</param>
        /// <param name="ingestedAt">The dateTime the data was ingested (optional -> default DateTime.Now)</param>
        /// <returns></returns>
        Task SendViewModelAsDataEventAsync<T>(string entityName, T viewModel, string dataEventCommandCode, DateTime? ingestedAt = null) where T : ILightViewModel;

        /// <summary>
        /// Gets specific LightDomain entities stored in the repository 
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="filter">lambda expression to be use in where clause of the query</param>
        /// <param name="orderBy">lambda expression to be use in order clause of the query</param>
        /// <param name="descending">True when the order is descending</param>
        /// <returns>List of LightDomain entities</returns>
        IEnumerable<T> Get<T>(Expression<Func<T, bool>> filter, Expression<Func<T, object>> orderBy = null, bool descending = false) where T : ILightModel;

        /// <summary>
        /// Gets all LightDomain entities stored in the repository 
        /// </summary>
        /// <typeparam name="T">LightDomain type</typeparam>
        /// <param name="orderBy">lambda expression to be use in order clause of the query</param>
        /// <param name="descending">True when the order is descending</param>
        /// <returns>List of LightDomain entities</returns>
        IEnumerable<T> GetAll<T>(Expression<Func<T, object>> orderBy = null, bool descending = false) where T : ILightModel;

        /// <summary>
        /// Gets specific LightDomain entities stored in the repository in paginated form
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="filter">filter to fetch the object</param>
        /// <param name="pagingParms">Paging parameters (if ommited, default is first page and 20 itemsper page)</param>
        /// <param name="orderBy">lambda expression to be use in order clause of the query</param>
        /// <param name="descending">True when the order is descending</param>
        /// <returns>Paginated list of LightDomain entities</returns>
        Task<ILightPaging<T>> GetByPageAsync<T>(Expression<Func<T, bool>> filter, ILightPagingParms pagingParms, Expression<Func<T, object>> orderBy = null, bool descending = false) where T : ILightModel;
        /// <summary>
        /// Reset all data in database
        /// </summary>
        /// <param name="dataSetType">Type of the dataset (e.g. Unit)</param>
        /// <param name="force">Indication of weather to force the reseed (recreating the database) or not</param>
        /// <returns></returns>
        Task<bool> ReseedDataAsync(string dataSetType, bool force = false);

    }
}
