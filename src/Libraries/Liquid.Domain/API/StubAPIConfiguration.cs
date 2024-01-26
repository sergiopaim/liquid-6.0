using FluentValidation;
using Liquid.Base;
using System;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Liquid.Runtime
{
    internal class TopLevelCallStubAPIConfiguration : LightConfig<TopLevelCallStubAPIConfiguration>
    {
        public CallStubAPIConfiguration Call { get; set; }

        public override void Validate()
        {
            RuleFor(x => x.Call).NotEmpty().WithError("The Call property should be informed on Stub API settings");
        }
    }

    internal class CallStubAPIConfiguration : LightConfig<CallStubAPIConfiguration>
    {
        public RequestStubAPIConfiguration Request { get; set; }
        public DomainResponse Response { get; set; }
        public HttpStatusCode StatusCode { get; set; }

        public override void Validate()
        {
            RuleFor(x => x.Request).NotEmpty().WithError("The Request property should be informed on Stub API settings");
            RuleFor(x => x.Response).NotEmpty().WithError("The Response property should be informed on Stub API settings");
            RuleFor(x => x.StatusCode).NotEmpty().WithError("The StatusCode property should be informed on Stub API settings");
        }
    }

    internal class RequestStubAPIConfiguration : LightConfig<RequestStubAPIConfiguration>
    {
        private static readonly string matchAnythingSymbol = Regex.Escape("{{*}}");

        public string Method { get; set; }
        public string Suffix { get; set; }
        public string UserId { get; set; }
        public JsonDocument Body { get; set; }

        public WorkBenchServiceHttp HttpMethod
        {
            get => Enum.Parse<WorkBenchServiceHttp>(Method.ToUpper());
        }

        public bool IsMatch(string suffix, WorkBenchServiceHttp method, string jsonBody, string userId = null)
        {
            if (HttpMethod != method)
                return false;

            if (string.IsNullOrEmpty(userId))
                userId = "1234567"; // Forces a pattern match to an any user id

            var userIdPattern = Regex.Escape(UserId).Replace(matchAnythingSymbol, ".+");
            if (!Regex.IsMatch(userId, userIdPattern, RegexOptions.IgnoreCase))
                return false;

            var suffixPattern = Regex.Escape(Suffix).Replace(matchAnythingSymbol, ".+");
            if (!Regex.IsMatch(suffix, suffixPattern, RegexOptions.IgnoreCase))
                return false;

            if (Body is not null)
            {
                var requestBody = JsonDocument.Parse(jsonBody).RootElement;
                foreach (var propInStub in Body.RootElement.EnumerateObject())
                {
                    var propInRequest = requestBody.Property(propInStub.Name[..1].ToLower() + propInStub.Name[1..]).AsString();
                    if (string.IsNullOrEmpty(propInRequest))
                        return false;

                    var bodyPattern = Regex.Escape(propInStub.Value.ToString()).Replace(matchAnythingSymbol, ".+");
                    if (!Regex.IsMatch(propInRequest, bodyPattern, RegexOptions.IgnoreCase))
                        return false;
                }
            }
            return true;
        }

        public override void Validate()
        {
            RuleFor(x => x.Method).NotEmpty().WithError("The Method property should be informed on Stub API settings");
            RuleFor(x => x.Suffix).NotEmpty().WithError("The Suffix property should be informed on Stub API settings");
        }
    }
}