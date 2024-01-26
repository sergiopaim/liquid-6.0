using Liquid.Base;
using Liquid.Base.Test;
using Liquid.Domain.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Liquid.Domain.API
{
    /// <summary>
    /// This class provide a simple way to consume rest services in a generic way. 
    /// There is no abstraction to business protocols.
    /// </summary>
    public class ApiWrapper : AbstractApiWrapper
    {
        private class Operation { public string Id { get; set; } }
        private readonly Operation operation = new();

        /// <summary>
        /// The id of the last operation
        /// </summary>
        public string OperationId => operation.Id;

        /// <summary>
        /// Initilizes API from fixed hostname and port
        /// </summary>
        /// <param name="hostName">host that serves the API</param>
        /// <param name="port">endpoint of connection</param>
        /// <param name="suffix">api trailing path</param>
        public ApiWrapper(string hostName, int port, string suffix) : this(hostName, port, suffix, null) { }

        /// <summary>
        /// Initilizes API from fixed hostname, port and token
        /// </summary>
        /// <param name="hostName">host that serves the API</param>
        /// <param name="port">endpoint of connection</param>
        /// <param name="token">token authentication</param>
        /// <param name="suffix">api trailing path</param>
        public ApiWrapper(string hostName, int port, string suffix, string token) : base(hostName, port, suffix, token, null) { }

        /// <summary>
        /// Initilizes API from fixed APIName
        /// </summary>
        /// <param name="apiName">name of the API</param>
        public ApiWrapper(string apiName) : this(apiName, null) { }

        /// <summary>
        /// Initilizes API from fixed APIName and Token
        /// </summary>
        /// <param name="apiName">name of the API</param>
        /// <param name="token">token authentication</param>
        public ApiWrapper(string apiName, string token) : base($"{nameof(ApiWrapper)}:{apiName}", token) { }

        /// <summary>
        /// Returns the url for the host of the API
        /// </summary>
        /// <param name="apiName">name of the API</param>
        public static new string HostUrl(string apiName)
        {
            return AbstractApiWrapper.HostUrl($"{nameof(ApiWrapper)}:{apiName}");
        }

        /// <summary>
        /// Sets the authentication token.
        /// </summary>
        /// <param name="token">The token to be set.</param>
        protected void SetToken(string token)
        {
            Token = token;
        }

        /// <summary>
        /// Creates a copy of an <c>ApiWrapper</c> instance with a new authentication token set.
        /// </summary>
        /// <param name="token">The token to be set.</param>
        /// <returns>The instance copy.</returns>
        public ApiWrapper WithToken(string token)
        {
            var clone = Clone() as ApiWrapper;
            clone.SetToken(token);
            return clone;
        }

        /// <summary>
        /// Creates a copy of an <c>ApiWrapper</c> instance with a new authentication token for the role from authorizations.json
        /// </summary>
        /// <param name="role">The role name stored in authorizations.json (for use in LightTests ONLY).</param>
        /// <returns>The instance copy.</returns>
        public ApiWrapper WithRole(string role)
        {
            var clone = Clone() as ApiWrapper;
            clone.SetToken(LightUnitTest.GetAuthorization(role));
            return clone;
        }

        /// <summary>
        /// Creates a copy of an <c>ApiWrapper</c> instance without authentication token (anonymously)
        /// </summary>
        /// <returns>The instance copy.</returns>
        public ApiWrapper Anonymously()
        {
            var clone = Clone() as ApiWrapper;
            clone.SetToken(null);
            return clone;
        }

        /// <summary>
        /// Send a POST or PUT operation expecting a result from a JSON type.
        /// </summary>
        /// <param name="operation">possible operations per HTTP specification</param>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers">pass additional information with the request or the response</param>
        /// <returns></returns>
        protected async Task<HttpResponseMessageWrapper<JsonDocument>> SendAsync(string operation, string serviceRoute, HttpContent body, Dictionary<string, string> headers)
        {
            return await SendAsync<JsonDocument>(operation, serviceRoute, body, headers);
        }

        /// <summary>
        /// Send a POST or PUT operation expecting a result from a JSON type.
        /// </summary>
        /// <param name="operation">possible operations per HTTP specification</param>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers">pass additional information with the request or the response</param>
        /// <returns></returns>
        protected async Task<HttpResponseMessageWrapper<T>> SendAsync<T>(string operation, string serviceRoute, HttpContent body, Dictionary<string, string> headers)
        {
            serviceRoute = serviceRoute?.Replace(Environment.NewLine, string.Empty)
                                       .Replace("\"", "'");

            using HttpClient client = new();

            client.DefaultRequestHeaders
                  .Accept
                  .Add(new MediaTypeWithQualityHeaderValue("application/json"));

            AdjustableClock.ApplyHeader(client);

            if (!string.IsNullOrEmpty(Token))
                client.DefaultRequestHeaders.Add("Authorization", Token);

            headers?.AsParallel().ForAll(item => { client.DefaultRequestHeaders.Add(item.Key, item.Value); });

            dynamic rawResponse = operation switch
            {
                "GET" => await client.GetAsync(MakeUri(serviceRoute)),
                "POST" => await client.PostAsync(MakeUri(serviceRoute), body),
                "PUT" => await client.PutAsync(MakeUri(serviceRoute), body),
                "DELETE" => await client.DeleteAsync(MakeUri(serviceRoute)),
                _ => throw new LightException($"Invalid operation {operation}."),
            };

            HttpResponseMessageWrapper<T> response = new(rawResponse);
            SetOperation(response);

            return response;
        }

        /// <summary>
        /// Send a DELETE operation 
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="headers">pass additional information with the request or the response</param>
        /// <returns></returns>
        public HttpResponseMessageWrapper<JsonDocument> Delete(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Delete<JsonDocument>(serviceRoute, headers);
        }

        private HttpResponseMessageWrapper<JsonDocument> Send(string operation, string serviceRoute, [Optional] JsonDocument body, Dictionary<string, string> headers)
        {
            return Send<JsonDocument>(operation, serviceRoute, body, headers);
        }

        private HttpResponseMessageWrapper<T> Send<T>(string operation, string serviceRoute, [Optional] JsonDocument body, Dictionary<string, string> headers)
        {
            using HttpContent content = new StringContent(body is null
                                                              ? "{}"
                                                              : body.ToJsonString(),
                                                          Encoding.UTF8, "application/json");
            return SendAsync<T>(operation, serviceRoute, content, headers).Result;
        }

        /// <summary>
        /// Methods that gets the object that processes requests for the route.
        /// </summary>
        /// <typeparam name="T">generic type</typeparam>
        /// <param name="serviceRoute">specified route prefix to connect to the service </param>
        /// <param name="headers">pass additional information with the request or the response</param>
        /// <returns></returns>
        public HttpResponseMessageWrapper<T> Get<T>(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Send<T>("GET", serviceRoute, headers: headers);
        }

        /// <summary>
        /// Methods Sync that gets the object that processes requests for the route.
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="headers">pass additional information with the request or the response</param>
        /// <returns>Returns the object as a JToken</returns>
        public HttpResponseMessageWrapper<JsonDocument> Get(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Get<JsonDocument>(serviceRoute, headers);
        }

        /// <summary>
        /// Method Sync that POST the object that processes requests for the route and returns the service response as a JToken.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public HttpResponseMessageWrapper<T> Post<T>(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
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
        public HttpResponseMessageWrapper<JsonDocument> Post(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
        {
            return Post<JsonDocument>(serviceRoute, body, headers);
        }

        /// <summary>
        /// Method Sync that POST the object that processes requests for the route and returns a Typed response.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="body">all the information enclosed in the message body in the JSON format</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public HttpResponseMessageWrapper<T> Put<T>(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
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
        public HttpResponseMessageWrapper<JsonDocument> Put(string serviceRoute, [Optional] JsonDocument body, [Optional] Dictionary<string, string> headers)
        {
            return Put<JsonDocument>(serviceRoute, body, headers);
        }

        /// <summary>
        /// Method Sync that DELETE the object that processes requests for the route and returns the service response as a JToken.
        /// </summary>
        /// <param name="serviceRoute">specified route prefix to connect to the service</param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public HttpResponseMessageWrapper<T> Delete<T>(string serviceRoute, [Optional] Dictionary<string, string> headers)
        {
            return Send<T>("DELETE", serviceRoute, headers: headers);
        }

        private void SetOperation<T>(HttpResponseMessageWrapper<T> response)
        {
            if (typeof(T) == typeof(DomainResponse))
            {
                operation.Id = (response.Content as DomainResponse)?.OperationId;
            }
        }
    }
}