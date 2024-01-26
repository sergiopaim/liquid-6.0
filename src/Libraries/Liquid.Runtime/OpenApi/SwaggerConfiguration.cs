using FluentValidation;
using System.Collections.Generic;

namespace Liquid.Runtime
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Model of Swagger Configuration details
    /// </summary>
    public class SwaggerConfiguration : LightConfig<SwaggerConfiguration>
    {
        public string BasePath { get; set; }
        public string ActiveVersion { get; set; }
        public List<SwaggerVersion> Versions { get; set; }
        public string[] Schemes { get; set; }
        public string[] ExcludingSwaggerList { get; set; }

        public override void Validate()
        {
            RuleFor(s => s.BasePath).NotEmpty().WithError("BasePath property should be informed on Swagger settings");
            RuleFor(s => s.ActiveVersion).NotEmpty().WithError("ActiveVersion propertiy should be informed on Swagger settings");
            RuleFor(s => s.Versions).NotEmpty().WithError("Versions property should be informed on Swagger settings");
        }
    }

    /// <summary>
    /// Model of Swagger Version details
    /// </summary>
    public class SwaggerVersion : LightConfig<SwaggerVersion>
    {
        public string Name { get; set; }
        public SwaggerInfo Info { get; set; }
        public bool IsActiveVersion { get; set; }

        public override void Validate()
        {
            RuleFor(i => i.Name).NotEmpty().WithError("Name property of SwaggerVersion should be informed on Swagger settings");
            RuleFor(i => i.Info).NotEmpty().WithError("Info property of SwaggerVersion should be informed on Swagger settings");
        }
    }

    /// <summary>
    /// Model of Swagger Info details
    /// </summary>
    public class SwaggerInfo : LightConfig<SwaggerInfo>
    {
        public string Version { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public override void Validate()
        { 
            RuleFor(i => i.Version).NotEmpty().WithError("Version property of SwaggerVersion.SwaggerInfo should be informed on Swagger settings");
            RuleFor(i => i.Title).NotEmpty().WithError("Title property of SwaggerVersion.SwaggerInfo should be informed on Swagger settings");
            RuleFor(i => i.Description).NotEmpty().WithError("Description property of SwaggerVersion.SwaggerInfo should be informed on Swagger settings");
        }
    }

    /// <summary>
    /// Model of Swagger Contact details
    /// </summary>
    public class SwaggerContact : LightConfig<SwaggerContact>
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Url { get; set; }
        public override void Validate()
        {
        }
    }

    /// <summary>
    /// Model of Swagger License details
    /// </summary>
    public class SwaggerLicense : LightConfig<SwaggerLicense>
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public override void Validate()
        {
           
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}