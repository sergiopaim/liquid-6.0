using Liquid.Base;
using Liquid.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Liquid.Domain.API
{
    /// <summary>
    /// This API provide a simple way to consume rest services
    /// </summary>
    public class LightApi : AbstractApiWrapper
    {
        private ICriticHandler CritictHandler { get; set; }

        /// <summary>
        /// Initilizes API from fixed hostname, port and token
        /// </summary>
        /// <param name="hostName">host that serves the API</param>
        /// <param name="port">endpoint of connection</param>
        /// <param name="suffix"></param>
        /// <param name="token">token authentication</param>
        /// <param name="lightDomain">LightDomain entity</param>        
        public LightApi(string hostName, int port, string suffix, string token, LightDomain lightDomain) : base(hostName, port, suffix, token, lightDomain) { }

        /// <summary>
        /// Initilizes API from fixed APIName and Token
        /// </summary>
        /// <param name="apiName">name of the API</param>
        /// <param name="token">token authentication</param>
        public LightApi(string apiName, string token) : base($"{nameof(LightApi)}:{apiName}", token) { }

        /// <summary>
        /// Initilizes API from fixed APIName and Token
        /// </summary>
        /// <param name="apiName">name of the API</param>
        /// <param name="token">token authentication</param>
        /// <param name="lightDomain">The calling LightDomain object</param>
        public LightApi(string apiName, string token, ILightDomain lightDomain) : this(apiName, token)
        {
            LightDomain = (LightDomain)lightDomain;
            CritictHandler = lightDomain?.CritictHandler;
        }

        /// <summary>
        /// Returns the url for the host of the API
        /// </summary>
        /// <param name="apiName">name of the API</param>
        public static new string HostUrl(string apiName)
        {
            return AbstractApiWrapper.HostUrl($"{nameof(LightApi)}:{apiName}");
        }

        private T HandleResult<T>(JsonDocument response, HttpStatusCode statusCode)
        {
            var domainResponse = response.Deserialize<DomainResponse>(LightGeneralSerialization.Default);
            if (domainResponse is not null)
            {
                if (LightDomain is not null)
                    CritictHandler = LightDomain.CritictHandler;

                if (domainResponse.Critics is not null)
                    CritictHandler?.Critics.AddRange(domainResponse.Critics);

                if (statusCode == HttpStatusCode.NoContent)
                    CritictHandler.StatusCode = StatusCode.NoContent;
                else if (statusCode == HttpStatusCode.Conflict)
                    CritictHandler.StatusCode = StatusCode.Conflict;

                if (domainResponse.Payload is not null)
                    try
                    {
                        return domainResponse.Payload.ToObject<T>();
                    }
                    catch
                    {
                        return "[]".ToJsonDocument().ToObject<T>();
                    }
            }

            return default;
        }

        /// <summary>
        /// Send a POST or PUT operation expecting a result from a JSON type.
        /// </summary>
        protected async Task<T> SendAsync<T>(string operation, string serviceRoute, JsonDocument body, Dictionary<string, string> headers)
        {
            serviceRoute = serviceRoute?.Replace(Environment.NewLine, string.Empty).Replace("\"", "'");
            var uri = MakeUri(serviceRoute);

            HttpResponseMessage response = null;
            using HttpClient httpClient = new();
            try
            {
                httpClient.DefaultRequestHeaders.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json; charset=utf-8");
                httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", CultureInfo.CurrentUICulture.Name);
                if (!string.IsNullOrEmpty(LightDomain?.Context?.OperationId))
                    httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Operation-Id", LightDomain.Context.OperationId);
                if (!string.IsNullOrEmpty(Token))
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);

                headers?.AsParallel().ForAll(item =>
                {
                    httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                });

                response = operation switch
                {
                    "GET" => await Request(uri, httpClient, WorkBenchServiceHttp.GET),
                    "POST" => await Request(uri, httpClient, WorkBenchServiceHttp.POST, body.ConvertToByteArrayContent()),
                    "PUT" => await Request(uri, httpClient, WorkBenchServiceHttp.PUT, body.ConvertToByteArrayContent()),
                    "DELETE" => await Request(uri, httpClient, WorkBenchServiceHttp.DELETE),
                    _ => throw new LightException($"Invalid operation {operation}."),
                };

                if (typeof(T).Equals(typeof(HttpResponseMessage)))
                {
                    return (T)Convert.ChangeType(response, typeof(T));
                }

                var content = response.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(content))
                    content = "{}";

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    throw new TimeoutException($"Calling {operation} {uri}");

                return HandleResult<T>(JsonDocument.Parse(content), response.StatusCode);
            }
            catch (Exception ex)
            {
                if (ex is TimeoutException)
                    throw;

                string exBody = body is null
                    ? "null"
                    : body.ToJsonString();
                StringBuilder sb = new();

                headers?.AsParallel().ForAll(item =>
                    sb.AppendLine($"{item.Key} = {item.Value}")
                );

                throw new LightException("********* ERROR ON API CALL *********\r\n\r\n" +
                                        $"OP: {operation}\r\n\r\n" +
                                        $"SERVICE_ROUTE: {uri}\r\n\r\n" +
                                        $"BODY: {exBody}\r\n\r\n" +
                                        $"HTTP_RESPONSE: {response?.Content?.ReadAsStringAsync().Result}\r\n\r\n" +
                                        $"EXCEPTION: {ex.Message}\r\n\r\n" +
                                        $"STACK_TRACE: {ex.StackTrace}\r\n\r\n");
            }
        }

        /// <summary>
        /// Send a POST or PUT operation expecting a result from a JSON type.
        /// </summary>
        protected async Task<JsonDocument> SendAsync(string operation, string serviceRoute, JsonDocument body, Dictionary<string, string> headers)
        {
            return await SendAsync<JsonDocument>(operation, serviceRoute, body, headers);
        }

        /// <summary>
        /// Performs an HTTP operation expecting a result from a defined type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers">pass additional information with the request or the response</param>
        /// <returns></returns>
        private T Send<T>(string operation, string serviceRoute, [Optional] JsonDocument body, Dictionary<string, string> headers)
        {
            try
            {
                return SendAsync<T>(operation, serviceRoute, body, headers).Result;
            }
            catch ( AggregateException e )
            {
                if (e.InnerException is TimeoutException)
                    throw e.InnerException;
            }

            return default;
        }

        /// <summary>
        /// Performs an HTTP operation expecting a JSON result.
        /// </summary>
        /// <param name="operation"></param>Http
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers">pass additional information with the request or the response</param>
        /// <returns></returns>
        private JsonDocument Send(string operation, string serviceRoute, [Optional] JsonDocument body, Dictionary<string, string> headers)
        {
            return Send<JsonDocument>(operation, serviceRoute, body, headers);
        }

        /// <summary>
        ///  Methods Sync that GETs the object that processes requests for the route.
        /// </summary>
        /// <typeparam name="T">Type of the return</typeparam>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="headers"></param>
        /// <returns>Returns object data to the APIWrapping</returns>
        public T Get<T>(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Send<T>("GET", serviceRoute, headers: headers);
        }

        /// <summary>
        /// Methods that gets the object that processes requests for the route.
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="headers"></param>
        /// <returns>Returns the JSON object in text</returns>
        public JsonDocument Get(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Get<JsonDocument>(serviceRoute, headers);
        }

        /// <summary>
        /// Method Sync that POST the object that processes requests for the route and returns a Typed response.
        /// </summary>
        /// <typeparam name="T">Type of the return</typeparam>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public T Post<T>(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
        {
            return Send<T>("POST", serviceRoute, body, headers);
        }

        /// <summary>
        /// Method Sync that POST the object that processes requests for the route and returns the service response as a JToken.
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public JsonDocument Post(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
        {
            return Post<JsonDocument>(serviceRoute, body, headers);
        }


        /// <summary>
        /// Method Sync that POST the object that processes requests for the route and returns a Typed response.
        /// </summary>
        /// <typeparam name="T">Type of the return</typeparam>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public T Put<T>(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
        {
            return Send<T>("PUT", serviceRoute, body, headers);
        }

        /// <summary>
        /// Method Sync that POST the object that processes requests for the route and returns the service response as a JToken.
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public JsonDocument Put(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
        {
            return Put<JsonDocument>(serviceRoute, body, headers);
        }


        /// <summary>
        /// Method Sync that DELETE the object that processes requests for the route and returns the service response as T.
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <typeparam name="T">Type of the return</typeparam>
        /// <param name="headers"></param>
        /// <returns></returns>
        public T Delete<T>(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Send<T>("DELETE", serviceRoute, headers: headers);
        }

        /// <summary>
        /// Method Sync that DELETE the object that processes requests for the route and returns the service response as a JToken.
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public JsonDocument Delete(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Delete<JsonDocument>(serviceRoute, headers);
        }
    }
}
