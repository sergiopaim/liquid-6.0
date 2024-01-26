using Liquid.Base;
using Liquid.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Liquid.Domain.API
{
    /// <summary>
    /// This class is a wrapper around HttpWebRequest in APIWrapper
    /// It simplifies the process by abstracting the details of the HttpWebRequest 
    /// </summary>
    public abstract class AbstractApiWrapper : ICloneable
    {
        /// <summary>
        /// The endpoint address
        /// </summary>
        protected string Endpoint { get; set; } = string.Empty;
        /// <summary>
        /// The API sufix
        /// </summary>
        protected string Suffix { get; set; } = string.Empty;
        /// <summary>
        /// The authentication token
        /// </summary>
        protected string Token { get; set; } = string.Empty;
        /// <summary>
        /// The api name
        /// </summary>
        protected string ApiName { get; set; } = string.Empty;
        /// <summary>
        /// The indication whether a stup should be used in testing environments
        /// </summary>
        protected bool Stub { get; set; }
        /// <summary>
        /// The light domain that provides transactional context
        /// </summary>
        public LightDomain LightDomain { get; set; }

        /// <summary>
        /// Creates a shallow copy of the instance
        /// </summary>
        /// <returns>Returns a shallow copy for the object instance</returns>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Workaround for handling transient stub values
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsTransientValueFromStub(string value)
        {
            return value == StubHandler.TRANSIENT_VALUE;
        }

        /// <summary>
        /// The hostURL for a given api
        /// </summary>
        /// <param name="apiName">The name of the api</param>
        /// <returns></returns>
        protected static string HostUrl(string apiName)
        {
            var config = LightConfigurator.LoadConfig<ApiConfiguration>(apiName);
            if (config is not null)
            {
                string host = config.Host;
                int port = config.Port ?? -1;
                string suffix = config.Suffix ?? string.Empty;
                return $"{host}{((port > 0) ? ":" + port.ToString() : "")}{((!string.IsNullOrEmpty(suffix)) ? suffix + "/" : "")}";
            }
            else
                return null;
        }

        /// <summary>
        /// This method authenticates and authorizes each requests by hostname, port and token
        /// </summary>
        /// <param name="hostName">host that serves the API</param>
        /// <param name="port">endpoint of connection</param>
        /// <param name="suffix">trailing path of the API</param>
        /// <param name="token">token authentication</param>
        /// <param name="lightDomain">LightDomain entity</param>
        /// <param name="stub">Flag to activate or not the stub</param>
        protected AbstractApiWrapper(string hostName, int port, string suffix, string token, LightDomain lightDomain, Boolean stub = false)
        {
            Token = token;
            Endpoint = $"{hostName}{((port > 0) ? ":" + port.ToString() + "/" : "/")}{((!string.IsNullOrEmpty(suffix)) ? suffix + "/" : "")}";
            // Defines apiname for use of stubs according to hostname
            ApiName = hostName?.Replace("http://", "");
            LightDomain = lightDomain;
            Suffix = suffix;
            Stub = stub;
        }

        /// <summary>
        /// This method authenticates and authorizes each requests by apiName and Token
        /// </summary>
        /// <param name="apiName">name of the API</param>
        /// <param name="token">token authentication</param>
        protected AbstractApiWrapper(string apiName, string token)
        {
            ApiName = apiName;
            if (token is not null)
                Token = token;

            // verify the section configuration through API configuration by api name
            // to build the ENDPOINT
            var config = LightConfigurator.LoadConfig<ApiConfiguration>(apiName);
            if (config is not null)
            {
                string host = config.Host;
                int port = config.Port ?? -1;
                string suffix = config.Suffix ?? string.Empty;
                Suffix = suffix;
                Endpoint = $"{host}{((port > 0) ? ":" + port.ToString() + "/" : "/")}{((!string.IsNullOrEmpty(suffix)) ? suffix + "/" : "")}";
                Stub = config.Stub;
            }
        }

        /// <summary>
        /// Empty constructor
        /// </summary>
        protected AbstractApiWrapper() { }

        /// <summary>
        /// Constructs the serviceRoute to the APIWrapper
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <returns>String ENDPOINT</returns>
        protected string MakeUri(string serviceRoute)
        {
            // Removes any duplicate and leading slashes
            serviceRoute = serviceRoute?.Replace("//", "/")
                                       .TrimStart('/');
            return $"{Endpoint}{serviceRoute}";
        }

        /// <summary>
        /// Request for use with circuit breaker
        /// </summary>
        /// <param name="URL"></param>
        /// <param name="httpClient"></param>
        /// <param name="http"></param>
        /// <param name="requestBody"></param>
        /// <returns></returns>
        internal async Task<HttpResponseMessage> Request(string URL, HttpClient httpClient, WorkBenchServiceHttp http, ByteArrayContent requestBody = null)
        {
            if ((WorkBench.IsDevelopmentEnvironment && Stub) || WorkBench.ShouldStubMicroserviceCalls)
                return await CallStubAsync(URL, http, requestBody);

            HttpResponseMessage responseMessage = await ResilientRequestAsync(URL, httpClient, http, requestBody)
                                   ?? throw new LightException($"Invalid HTTP operation for {URL}: {http}");
            if (responseMessage.StatusCode != HttpStatusCode.OK &&
                responseMessage.StatusCode != HttpStatusCode.NoContent &&
                responseMessage.StatusCode != HttpStatusCode.Conflict &&
                responseMessage.StatusCode != HttpStatusCode.TooManyRequests)
                throw new ApiLightException($"Failed to send {http} request to {URL}. Returned status code {responseMessage.StatusCode}\r\n" +
                                            $"Response message: {responseMessage.ToJsonString()}");

            return responseMessage;
        }

        private async Task<HttpResponseMessage> CallStubAsync(string URL, WorkBenchServiceHttp http, ByteArrayContent requestBody)
        {
            var stubFileName = ApiName.Contains(':')
                                   ? ApiName.Split(':')[1]
                                   : ApiName;
            stubFileName = stubFileName.ToLower();
            stubFileName = $"{stubFileName[..1].ToUpper()}{stubFileName[1..]}";

            var errPrefix = $"Workbench could not make the {http} request for {URL} by stub file {stubFileName}: ";

            var stubConfig = StubHandler.GetStubData<List<TopLevelCallStubAPIConfiguration>>(stubFileName, subDirectory: "Stub")
                                ?? throw new LightException(errPrefix + "configuration file not found");

            var jsonBody = requestBody?.ReadAsStringAsync().Result ?? "{}";
            var userId = (JwtSecurityCustom.DecodeToken(Token)?.Identity as ClaimsIdentity)?.FindFirst("sub")?.Value;

            CallStubAPIConfiguration call = (stubConfig.FirstOrDefault(c => c.Call.Request.IsMatch(URL, http, jsonBody, userId))?.Call)
                                                ?? throw new LightException($"{errPrefix} no match found in stub file. Check HTTP method, endpoint, token, and body. " +
                                                                            $"Body sent: {jsonBody}");
            return await Task.FromResult(new HttpResponseMessage()
                                             {
                                                 StatusCode = call.StatusCode,
                                                 Content = new StringContent(call.Response.ToJsonString(), System.Text.Encoding.UTF8),
                                             });
        }

        private static async Task<HttpResponseMessage> ResilientRequestAsync(string URL, HttpClient httpClient, WorkBenchServiceHttp http, ByteArrayContent requestBody, int tryNum = 1)
        {
            const int MAX_RETRIES = 3;

            HttpResponseMessage response = null;

            try
            {
                switch (http)
                {
                    case WorkBenchServiceHttp.GET:
                        response = await httpClient.GetAsync(URL);
                        break;
                    case WorkBenchServiceHttp.POST:
                        response = await httpClient.PostAsync(URL, requestBody);
                        break;
                    case WorkBenchServiceHttp.PUT:
                        response = await httpClient.PutAsync(URL, requestBody);
                        break;
                    case WorkBenchServiceHttp.DELETE:
                        response = await httpClient.DeleteAsync(URL);
                        break;
                }

                if ((response.StatusCode == HttpStatusCode.BadGateway ||
                     response.StatusCode == HttpStatusCode.ServiceUnavailable) && tryNum <= MAX_RETRIES)
                {
                    Thread.Sleep(2000 * tryNum * tryNum);
                    return await ResilientRequestAsync(URL, httpClient, http, requestBody, ++tryNum);
                }
            }
            catch (Exception e)
            {
                if (e.Message.StartsWith("Connection refused") ||
                    e.Message.StartsWith("Name or service not known"))
                {
                    Thread.Sleep(2000 * tryNum * tryNum);
                    return await ResilientRequestAsync(URL, httpClient, http, requestBody, ++tryNum);
                }

                e.FilterRelevantStackTrace();
                throw;
            }
            
            return response;
        }
    }
}
