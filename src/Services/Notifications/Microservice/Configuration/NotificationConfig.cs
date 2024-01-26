using FluentValidation;
using Liquid;
using Liquid.Runtime;
using System;
using System.Collections.Generic;

namespace Microservice.Configuration
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public class NotificationConfig : LightConfig<NotificationConfig>
    {
        public string AWSAcessKeyId { get; set; }
        public string AWSSecretAccessKey { get; set; }

        public string VapidSubject { get; set; }
        public string VapidPublicKey { get; set; }
        public string VapidPrivateKey { get; set; }
        public string TextGatewayKey { get; set; }
        public string TextSender { get; set; }
        public bool TextSendToTestUsers { get; set; }
        public string TextPhoneForTestUsers { get; set; }

        public override void Validate()
        {
            RuleFor(v => v.AWSAcessKeyId).NotEmpty().WithError($"AWS Access Key cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");
            RuleFor(v => v.AWSSecretAccessKey).NotEmpty().WithError($"AWS Secret Key cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");

            RuleFor(v => v.VapidSubject).NotEmpty().WithError($"Vapid Subject cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");
            RuleFor(v => v.VapidPublicKey).NotEmpty().WithError($"Vapid Public Key cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");
            RuleFor(v => v.VapidPrivateKey).NotEmpty().WithError($"Vapid Private Key cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");

            RuleFor(v => v.TextGatewayKey).NotEmpty().WithError($"Text Gateway Key cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");
            if (TextSendToTestUsers == true)
                RuleFor(v => v.TextPhoneForTestUsers).NotEmpty().WithError($"Text Phone for Test Users cannot be empty in the 'Notification' config entry in app.settings.{WorkBench.EnvironmentName} file.");
        }

        private static readonly NotificationConfig _value = LightConfigurator.LoadConfig<NotificationConfig>("Notification");

#pragma warning disable IDE1006 // Naming Styles
        public static string awsAcessKeyId => _value?.AWSAcessKeyId;
        public static string awsSecretAccessKey => _value?.AWSSecretAccessKey;
        public static string vapidSubject => _value?.VapidSubject;
        public static string vapidPublicKey => _value?.VapidPublicKey;
        public static string vapidPrivateKey => _value?.VapidPrivateKey;
        public static string textGatewayKey => _value?.TextGatewayKey;
        public static string textSender => _value?.TextSender ?? "";
        public static bool? textSendToTestUsers => _value?.TextSendToTestUsers;
        public static string textPhoneForTestUsers => _value?.TextPhoneForTestUsers;
    }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}