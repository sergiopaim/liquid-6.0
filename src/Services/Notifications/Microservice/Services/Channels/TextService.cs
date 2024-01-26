using Liquid;
using Liquid.Base;
using Liquid.Domain;
using Liquid.Domain.API;
using Liquid.Platform;
using Microservice.Configuration;
using Microservice.Models;
using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Microservice.Services
{
    internal class TextService : LightService
    {
        internal async Task<DomainResponse> SendAsync(ShortTextMSG msg)
        {
            Config userConfig = null;

            if (!string.IsNullOrEmpty(msg.UserId))
            {
                userConfig = await Service<ConfigService>().GetConfigById(msg.UserId);
                if (userConfig is null)
                    throw new BusinessLightException($"userId {msg.UserId} has no notification config data");
            }
            else if (msg.Type != NotificationType.Direct.Code)
                return BadRequest("userId must be informed for notification types other then direct");
            else if (string.IsNullOrEmpty(msg.Phone))
                return BadRequest("direct notifications must have phone number");

            return SendAsync(userConfig, msg);
        }

        internal DomainResponse SendAsync(Config userConfig, ShortTextMSG msg)
        {
            Telemetry.TrackEvent("Send Text", userConfig?.Id ?? msg.Phone);

            var toNumber = string.IsNullOrEmpty(msg.Phone)
                   ? userConfig?.PhoneChannel?.Phone
                   : msg.Phone;

            if (string.IsNullOrEmpty(toNumber))
                return BusinessWarning("PHONE_NUMBER_IS_NULL_WARN", userConfig?.Id ?? msg.UserId);

            toNumber = toNumber.Replace("+", "")
                               .Replace("-", "")
                               .Replace("(", "")
                               .Replace(")", "")
                               .Replace(" ", "");

            var message = ApplyMacros(msg.Message, msg.ShowSender);

            if (WorkBench.IsDevelopmentEnvironment ||
                WorkBench.IsIntegrationEnvironment ||
                WorkBench.IsQualityEnvironment ||
                WorkBench.IsDemonstrationEnvironment)
            {
                if (userConfig is null && toNumber.StartsWith("5599"))
                {
                    CaptureText(toNumber, message);
                    return Response();
                }

                if (userConfig?.EmailChannel.Email.EndsWith("@members.com") == true ||
                    userConfig?.EmailChannel.Email.EndsWith("@your-dev-domain.onmicrosoft.com") == true)
                {
                    CaptureText(userConfig, toNumber, message);

                    if (NotificationConfig.textSendToTestUsers == true)
                        toNumber = NotificationConfig.textPhoneForTestUsers;
                    else
                        return Response();
                }
            }

            string apiErrorMsg = null;
            JsonDocument response = default;
            ApiWrapper api = new("TEXT_GATEWAY");

            try
            {
                var result = api.Get<JsonDocument>($"send?key={NotificationConfig.textGatewayKey}&type=9&number={toNumber}&msg={message}&flash=1&refer={Context.OperationId}");
                response = result.Content;

                if (result.StatusCode != HttpStatusCode.OK ||
                    response.Property("situacao").AsString() != "OK")
                {
                    apiErrorMsg = response.ToJsonString();
                }
            }
            catch (Exception)
            {
                Thread.Sleep(5000);
                //Performs ONE API call retry after 5 seconds
                try
                {
                    var result = api.Get<JsonDocument>($"send?key={NotificationConfig.textGatewayKey}&type=9&number={toNumber}&msg={message}");
                    response = result.Content;

                    if (result.StatusCode != HttpStatusCode.OK ||
                        response.Property("situacao").AsString() != "OK")
                    {
                        apiErrorMsg = response?.ToJsonString();
                    }
                }
                catch (Exception e)
                {
                    apiErrorMsg = e.ToString();
                }
            }

            if (apiErrorMsg is null)
            {
                Telemetry.TrackTrace($"toNumber: {toNumber}");
                return Response(data: new { messageId = response.Property("id").AsString() });
            }
            else
            {
                Telemetry.TrackTrace($"send?##KEY##&type=9&number={toNumber}&msg={message}");
                Telemetry.TrackTrace($"ErrorMessage:\n'{apiErrorMsg}'");
                throw new BusinessLightException("SMS_GATEWAY_UNAVAILABLE");
            }
        }

        private async void CaptureText(string toPhone, string message)
        {
            var textAsEmailMSG = FactoryLightMessage<EmailMSG>(EmailCMD.Send);

            textAsEmailMSG.Type = NotificationType.Direct.Code;
            textAsEmailMSG.Email = "any@persons.com";
            textAsEmailMSG.Subject = LightLocalizer.Localize("TEXT_CAPTURED", toPhone);
            textAsEmailMSG.Message = message;

            Console.WriteLine($"{textAsEmailMSG.Subject}: {textAsEmailMSG.Message}");
            await Service<EmailService>().SendAsync(textAsEmailMSG);
        }

        private async void CaptureText(Config userConfig, string toPhone, string message)
        {
            var textAsEmailMSG = FactoryLightMessage<EmailMSG>(EmailCMD.Send);

            var oldlanguage = FormatterByProfile.SetCurrentLanguage(userConfig?.Language);

            textAsEmailMSG.UserId = userConfig?.Id;
            textAsEmailMSG.Type = NotificationType.Account.Code;
            textAsEmailMSG.Subject = LightLocalizer.Localize("TEXT_CAPTURED", toPhone);
            textAsEmailMSG.Message = message;

            FormatterByProfile.SetCurrentLanguage(oldlanguage);

            Console.WriteLine($"{textAsEmailMSG.Subject}: {textAsEmailMSG.Message}");
            await Service<EmailService>().SendAsync(userConfig, textAsEmailMSG);
        }

        private static string ApplyMacros(string text, bool showSender)
        {
            /* When SMS bug gets fixed or SMSDev is replaced, 
             * call
            
            return NotificationConfig.textHeader + PlatformServices.ExpandAppUrls(text);
            
             * and remove the workaround bellow */

            // Workaround for SMSDev gateway bug when shortening https URIs 
            foreach (var appUrl in PlatformServices.AppURLs)
            {
                string url = appUrl.Value.ToString();

                if (url.Substring(url.Length - 1, 1) == "/")
                    url = url[0..^1];

                url = url.Replace("https://", "http:///");

                text = text.Replace("{" + appUrl.Key + "}", url, StringComparison.InvariantCulture);
            }

            if (showSender)
                text = NotificationConfig.textSender + text;

            return text;
        }
    }
}