using System.Net;

namespace Liquid.Domain.API
{
    /// <summary>
    /// Helper for the <c>HttpWebRequest</c> class.
    /// Properly retrieves http responses instead of throwing exceptions.
    /// </summary>
    public static class LightHttpWebRequest
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

        public static WebResponse GetResponse(WebRequest request)
        {
            try
            {
                return request?.GetResponse();
            }
            catch (WebException wex)
            {
                if (wex.Response is not null)
                {
                    return wex.Response;
                }
                throw new WebException("`WebException` caught but `Response` was null.");
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
