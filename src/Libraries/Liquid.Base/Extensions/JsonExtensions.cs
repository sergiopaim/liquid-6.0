using System;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Liquid.Base
{    /// <summary>
     /// Implement Extensions of all objects
     /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Convert object content before send to server
        /// </summary>
        /// <param name="value">Object content</param>
        /// <returns></returns>
        public static ByteArrayContent ConvertToByteArrayContent(this object value)
        {
            var buffer = Encoding.UTF8.GetBytes(value.ToJsonString());
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            return byteContent;
        }

        /// <summary>
        /// Deserializes the string as JsonDocument.
        /// </summary>
        /// <returns>A JsonDocument representation of the object.</returns>
        public static JsonDocument ToJsonDocument(this string value)
        {
            return JsonDocument.Parse(value);
        }

        /// <summary>
        /// Serializes the object as JsonDocument.
        /// </summary>
        /// <returns>A JsonDocument representation of the object.</returns>
        public static JsonDocument ToJsonDocument(this object value)
        {
            return JsonDocument.Parse(value.ToJsonString());
        }

        /// <summary>
        /// Serializes the object as JsonNode.
        /// </summary>
        /// <returns>A JsonNode representation of the object.</returns>
        public static JsonNode ToJsonNode(this object value)
        {
            return JsonNode.Parse(value.ToJsonString());
        }

        /// <summary>
        /// Serializes the object to a indented JSON string with CamelCase.
        /// </summary>
        /// <returns>A JSON string representation of the object.</returns>
        /// <param name="value">The type to be serialized</param>
        /// <param name="writeIndented">Indication whether the generated JSON string must be indented</param>
        /// <returns></returns>
        public static string ToJsonString<T>(this T value, bool writeIndented = false)
        {
            if (value is null)
                return JsonSerializer.Serialize(value, writeIndented
                                                            ? LightGeneralSerialization.WriteIndented
                                                            : LightGeneralSerialization.Default);

            return JsonSerializer.Serialize(value, value.GetType(), writeIndented
                                                                         ? LightGeneralSerialization.WriteIndented
                                                                         : LightGeneralSerialization.Default);
        }

        /// <summary>
        /// Serializes the object to a JSON representing Json string with CamelCase.
        /// </summary>
        /// <returns>A JSON string representation of the object.</returns>
        public static byte[] ToJsonBytes<T>(this T value)
        {
            return Encoding.UTF8.GetBytes(value.ToJsonString());
        }

        /// <summary>
        /// Deserializes a json string as a T
        /// </summary>
        /// <param name="value">The json document in string format</param>
        /// <returns>The document as T</returns>
        public static T ToObject<T>(this string value)
        {
            return JsonSerializer.Deserialize<T>(value, LightGeneralSerialization.IgnoreCase);
        }

        /// <summary>
        /// Gets a JsonDocument root property as a T
        /// </summary>
        /// <param name="doc">The json document</param>
        /// <returns>The document as T</returns>
        public static T ToObject<T>(this JsonDocument doc)
        {
            try
            {
                return doc.Deserialize<T>(LightGeneralSerialization.IgnoreCase);
            }
            catch (JsonException e)
            {
                if (doc.RootElement.ValueKind == JsonValueKind.String &&
                    string.IsNullOrEmpty(doc.RootElement.AsString()))
                    return default;

                e.FilterRelevantStackTrace();
                throw;
            }
        }

        /// <summary>
        /// Gets a JsonNode as a T
        /// </summary>
        /// <param name="node">The json node</param>
        /// <returns>The Json node as T</returns>
        public static T ToObject<T>(this JsonNode node)
        {
            try
            {
                return node.Deserialize<T>(LightGeneralSerialization.IgnoreCase);
            }
            catch (Exception e)
            {
                if (typeof(T) != typeof(string))
                {
                    e.FilterRelevantStackTrace();
                    throw;
                }

                return (T)(object)node.ToString();
            }
        }

        /// <summary>
        /// Gets a JsonDocument from a JsonElement
        /// </summary>
        /// <param name="element">The json element</param>
        /// <returns>The JsonElement content as JsonDocument</returns>
        public static JsonDocument ToJsonDocument(this JsonElement element)
        {
            return JsonSerializer.SerializeToDocument(element, LightGeneralSerialization.Default);
        }

        /// <summary>
        /// Gets a JsonElement root property as a T
        /// </summary>
        /// <param name="element">The json element</param>
        /// <returns>The property as T</returns>
        public static T ToObject<T>(this JsonElement element)
        {
            try
            {
                return element.Deserialize<T>(LightGeneralSerialization.IgnoreCase);
            }
            catch (JsonException e)
            {
                if (element.ValueKind == JsonValueKind.String &&
                    string.IsNullOrEmpty(element.AsString()))
                    return default;

                e.FilterRelevantStackTrace();
                throw;
            }
        }

        /// <summary>
        /// Gets a JsonDocument root property as a JsonElement
        /// </summary>
        /// <param name="doc">The json document</param>
        /// <param name="propName">The property name</param>
        /// <returns>The property as JsonElement</returns>
        public static JsonElement Property(this JsonDocument doc, string propName)
        {
            try
            {
                return doc.RootElement.GetProperty(propName);
            }
            catch
            {
                return JsonSerializer.SerializeToElement("");
            }
        }

        /// <summary>
        /// Gets a JsonElement property as a JsonElement
        /// </summary>
        /// <param name="element">The json element</param>
        /// <param name="propName">The property name</param>
        /// <returns>The property as JsonElement</returns>
        public static JsonElement Property(this JsonElement element, string propName)
        {
            try
            {
                return element.GetProperty(propName);
            }
            catch
            {
                return JsonSerializer.SerializeToElement("");
            }
        }

        /// <summary>
        /// Gets a JsonElement value as string
        /// </summary>
        /// <param name="element">The json element</param>
        public static string AsString(this JsonElement element)
        {
            try
            {
                return element.GetString();
            }
            catch (Exception e)
            {
                if (element.ValueKind == JsonValueKind.Null)
                    return null;

                if (element.ValueKind == JsonValueKind.Number ||
                    element.ValueKind == JsonValueKind.True ||
                    element.ValueKind == JsonValueKind.False)
                    return element.ToString();

                e.FilterRelevantStackTrace();
                throw;
            }
        }

        /// <summary>
        /// Gets a JsonDocument as string
        /// </summary>
        /// <param name="doc">The json document </param>
        public static string AsString(this JsonDocument doc)
        {
            return doc.RootElement.AsString();
        }

        /// <summary>
        /// Gets a JsonElement value as DateTime
        /// </summary>
        /// <param name="element">The json element</param>
        public static DateTime AsDateTime(this JsonElement element)
        {
            try
            {
                return element.GetDateTime();
            }
            catch (Exception e)
            {
                if (element.ValueKind == JsonValueKind.Null)
                    return DateTime.MinValue;

                e.FilterRelevantStackTrace();
                throw;
            }
        }

        /// <summary>
        /// Gets a JsonElement value as bool
        /// </summary>
        /// <param name="element">The json element</param>
        public static bool? AsBoolean(this JsonElement element)
        {
            try
            {
                return element.GetBoolean();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a JsonElement value as double
        /// </summary>
        /// <param name="element">The json element</param>
        public static double? AsDouble(this JsonElement element)
        {
            try
            {
                return element.GetDouble();
            }
            catch (Exception e1)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Null)
                        return null;

                    if (element.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(element.GetString()))
                        return null;

                    return double.Parse(element.GetString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    e1.FilterRelevantStackTrace();
                    throw e1;
                }
            }
        }

        /// <summary>
        /// Gets a JsonElement value as Int16
        /// </summary>
        /// <param name="element">The json element</param>
        public static Int16? AsInt16(this JsonElement element)
        {
            try
            {
                return element.GetInt16();
            }
            catch (Exception e1)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Null)
                        return null;

                    if (element.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(element.GetString()))
                        return null;

                    return Int16.Parse(element.GetString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    e1.FilterRelevantStackTrace();
                    throw e1;
                }
            }
        }

        /// <summary>
        /// Gets a JsonElement value as Int32
        /// </summary>
        /// <param name="element">The json element</param>
        public static Int32? AsInt(this JsonElement element)
        {
            try
            {
                return element.GetInt32();
            }
            catch (Exception e1)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Null)
                        return null;

                    if (element.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(element.GetString()))
                        return null;

                    return Int32.Parse(element.GetString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    e1.FilterRelevantStackTrace();
                    throw e1;
                }
            }
        }

        /// <summary>
        /// Gets a JsonElement value as Int64
        /// </summary>
        /// <param name="element">The json element</param>
        public static Int64? AsInt64(this JsonElement element)
        {
            try
            {
                return element.GetInt64();
            }
            catch (Exception e1)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Null)
                        return null;

                    if (element.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(element.GetString()))
                        return null;

                    return Int64.Parse(element.GetString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    e1.FilterRelevantStackTrace();
                    throw e1;
                }
            }
        }

        /// <summary>
        /// Gets a JsonElement value as Decimal
        /// </summary>
        /// <param name="element">The json element</param>
        public static decimal? AsDecimal(this JsonElement element)
        {
            try
            {
                return element.GetDecimal();
            }
            catch (Exception e1)
            {
                try
                {
                    if (element.ValueKind == JsonValueKind.Null)
                        return null;

                    if (element.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(element.GetString()))
                        return null;

                    return decimal.Parse(element.GetString(), CultureInfo.InvariantCulture);
                }
                catch
                {
                    e1.FilterRelevantStackTrace();
                    throw e1;
                }
            }
        }

        /// <summary>
        /// Checks if a JsonNode is a JsonArray
        /// </summary>
        /// <param name="node">The json node</param>
        public static bool IsArray(this JsonNode node)
        {
            if (node is null)
                return false;

            try
            {
                node.AsArray();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
        /// <summary>
        /// Checks if a JsonNode is a JsonObject
        /// </summary>
        /// <param name="node">The json node</param>
        public static bool IsObject(this JsonNode node)
        {
            if (node is null)
                return false;

            try
            {
                node.AsObject();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a JsonNode is a integer
        /// </summary>
        /// <param name="node">The json node</param>
        public static bool IsInteger(this JsonNode node)
        {
            if (node is null)
                return false;

            try
            {
                node.GetValue<Int64>();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a JsonNode is a double
        /// </summary>
        /// <param name="node">The json node</param>
        public static bool IsDouble(this JsonNode node)
        {
            if (node is null)
                return false;

            try
            {
                node.GetValue<double>();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a JsonNode is a DateTime
        /// </summary>
        /// <param name="node">The json node</param>
        public static bool IsDateTime(this JsonNode node)
        {
            if (node is null)
                return false;

            try
            {
                node.GetValue<DateTime>();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}