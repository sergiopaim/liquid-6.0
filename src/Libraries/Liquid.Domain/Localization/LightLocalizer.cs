using Liquid.Base;
using Liquid.Runtime;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Liquid.Domain
{
    /// <summary>
    /// This class is responsible to provide localization capability over json files
    /// </summary>
    public class LightLocalizer : IStringLocalizer
    {
        private static readonly LightLocalizer localizer = new();
        private static readonly Dictionary<string, JsonDocument> cachedResources = LoadResources();

        private const string baseName = "Resources";
        private const string fileName = "localization";

        private static Dictionary<string, JsonDocument> LoadResources()
        {
            Dictionary<string, JsonDocument> resources = new();
            var resourceFileLocations = LocalizerUtil.ExpandPaths(Path.Join(baseName, fileName), string.Empty).ToList();
            var config = LightConfigurator.LoadConfig<LocalizationConfig>("Localization");

            foreach (var cultureSuffix in config.SupportedCultures)
            {
                string resourcePath = null;
                bool found = false;
                foreach (var resourceFileLocation in resourceFileLocations)
                {
                    resourcePath = resourceFileLocation + "." + cultureSuffix.ToLower() + ".json";
                    if (File.Exists(resourcePath))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new LightException($"Missing localization file: {resourcePath}");

                // Found a resource file path: attempt to parse it into a JsonDocument.
                try
                {
                    resources.Add(cultureSuffix.ToLower(),
                                  JsonDocument.Parse(File.ReadAllText(resourcePath, Encoding.UTF8),
                                                     new JsonDocumentOptions() { CommentHandling = JsonCommentHandling.Skip }));
                }
                catch (Exception e)
                {
                    throw new LightException($"Invalid Json format of file: {resourcePath}", new Exception(e.Message));
                }
            }

            return resources;
        }

        /// <summary>
        /// Converts a code into a localized string
        /// </summary>
        /// <param name="code">Code to localize</param>
        /// <returns></returns>
        public static string Localize(string code)
        {
            CultureInfo culture = string.IsNullOrEmpty(CultureInfo.CurrentUICulture?.Name) ? new CultureInfo("en") : CultureInfo.CurrentUICulture;

            var localizedInFramework = Properties.Localization.ResourceManager.GetString(code, culture);

            return localizedInFramework ?? localizer[code];
        }

        /// <summary>
        /// Converts a code into a localized and interpolated string
        /// </summary>
        /// <param name="code">Code to localize</param>
        /// <param name="args">Arguments to interpolate</param>
        /// <returns></returns>
        public static string Localize(string code, params object[] args)
        {
            if (args is not null)
                try
                {
                    return string.Format(Localize(code), args);
                }
                catch
                {
                    throw new LightException("Badly formed interpolation string and args. Check number of parameters in the string matches the number of args params.");
                }
            else
                return Localize(code);
        }

        /// <summary>
        /// Gets a string instance based on a localization entry name
        /// </summary>
        /// <param name="name">The name of string to localize</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual LocalizedString this[string name]
        {
            get
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var value = GetLocalizedString(name, CultureInfo.CurrentUICulture);
                return new(name, value ?? name, resourceNotFound: value is null);
            }
        }

        /// <summary>
        /// Gets a string instance based on a localization entry name and corresponding arguments
        /// </summary>
        /// <param name="name">The name of string to localize</param>
        /// <param name="arguments">The array of arguments to interpolate</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public virtual LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                var format = GetLocalizedString(name, CultureInfo.CurrentUICulture);
                var value = string.Format(format ?? name, arguments);
                return new(name, value, resourceNotFound: format is null);
            }
        }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public virtual IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        {
            return GetAllStrings(includeParentCultures, CultureInfo.CurrentUICulture);
        }

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0060 // Remove unused parameter
        protected static IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures, CultureInfo culture)
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore IDE0079 // Remove unnecessary suppression
        {
            if (culture is null)
            {
                throw new ArgumentNullException(nameof(culture));
            }
            throw new NotImplementedException();
        }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        /// <summary>
        /// Gets the localized string 
        /// </summary>
        /// <param name="name">The name of string to localize</param>
        /// <param name="currentCulture">The culture to localize</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        protected static string GetLocalizedString(string name, CultureInfo currentCulture)
        {
            string value = null;
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // Attempts to get resource with the given name from the resource object. if not found, try parent
            // resource object until parent culture begets himself.
            CultureInfo previousCulture;
            do
            {
                var resourceObject = cachedResources.FirstOrDefault(r => r.Key == currentCulture.Name.ToLower()).Value;
                if (resourceObject is not null)
                {
                    value = resourceObject.Property(name).AsString();
                    break;
                }

                // Consults parent culture.
                previousCulture = currentCulture;
                currentCulture = currentCulture?.Parent;

            } while (previousCulture != currentCulture);

            return string.IsNullOrEmpty(value) ? name : value;
        }
    }

    static class LocalizerUtil
    {
        public static string TrimPrefix(string name, string prefix)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (prefix is null) throw new ArgumentNullException(nameof(prefix));

            if (name.StartsWith(prefix, StringComparison.Ordinal))
            {
                return name[prefix.Length..];
            }
            return name;
        }

        public static IEnumerable<string> ExpandPaths(string name, string baseName)
        {
            if (name is null) throw new ArgumentNullException(nameof(name));
            if (baseName is null) throw new ArgumentNullException(nameof(baseName));

            return ExpandPathIterator(name, baseName);
        }


        private static IEnumerable<string> ExpandPathIterator(string name, string baseName)
        {
            StringBuilder expansion = new();

            // Start replacing periods, starting at the beginning.
            var components = name.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < components.Length; i++)
            {
                for (var j = 0; j < components.Length; j++)
                {
                    expansion.Append(components[j]);

                    AppendSeparator(expansion, i, j);
                }
                // Remove trailing period.
                yield return expansion.Remove(expansion.Length - 1, 1).ToString();
                expansion.Clear();
            }

            // Do the same with the name where baseName prefix is removed.
            var nameWithoutPrefix = TrimPrefix(name, baseName);
            if (nameWithoutPrefix != string.Empty && nameWithoutPrefix != name)
            {
                nameWithoutPrefix = nameWithoutPrefix[1..];
                var componentsWithoutPrefix = nameWithoutPrefix.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < componentsWithoutPrefix.Length; i++)
                {
                    for (var j = 0; j < componentsWithoutPrefix.Length; j++)
                    {
                        expansion.Append(componentsWithoutPrefix[j]);

                        AppendSeparator(expansion, i, j);
                    }
                    // Remove trailing period.
                    yield return expansion.Remove(expansion.Length - 1, 1).ToString();
                    expansion.Clear();
                }
            }
        }

        private static void AppendSeparator(StringBuilder expansion, int i, int j)
        {
            var separator = j < i ? Path.DirectorySeparatorChar : '.';
            expansion.Append(separator);
        }
    }
}