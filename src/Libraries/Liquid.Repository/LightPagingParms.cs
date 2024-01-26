using Liquid.Interfaces;

namespace Liquid.Repository
{
    /// <summary>
    /// Paging parameters
    /// </summary>
    public class LightPagingParms : ILightPagingParms
    {
        private const int MAX_ITEM_PER_PAGE = 1000;
        private int itemsPerPage = 20;
        /// <summary>
        /// Default pagin parms (first page and 20 items per page)
        /// </summary>
        public static LightPagingParms Default { get; } = new(20, null);
        /// <summary>
        /// Number of registers per page
        /// </summary>
        public int ItemsPerPage
        {
            get => itemsPerPage;
            set => itemsPerPage = value < MAX_ITEM_PER_PAGE ? value : MAX_ITEM_PER_PAGE;
        }
        /// <summary>
        /// Database index of the current page to get the next one
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Creates a new lightPagingParms
        /// </summary>
        /// <param name="itemsPerPage">Number of items per page (if ommited, 20 items will be considered)</param>
        /// <param name="continuationToken">The token for the next page (if ommited, the first page will be returned)</param>
        public LightPagingParms (int? itemsPerPage, string continuationToken)
        {
            ItemsPerPage = itemsPerPage ?? 0;
            ContinuationToken = continuationToken;
        }
    }
}