using FluentValidation;
using Liquid.Base;
using Liquid.Interfaces;
using Liquid.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Liquid.Repository
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// General ValueObject class to compose Model and ViewModel classes
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LightValueObject<T> : ILightValueObject where T : LightValueObject<T>, ILightValueObject, new()
    {
        /// <summary>
        /// 
        /// </summary>
        protected LightValueObject()
        {
            ValidateInstances();
        }

        [SwaggerIgnore]
        [JsonIgnore]
        public Dictionary<string, object[]> InputErrors { get; set; } = new();
        public bool ShouldSerializeInputErrors() { return false; }

        /// <summary>
        /// Factories an instance of LightValueObject from a ILightViewModel instance
        /// </summary>
        /// <param name="lightViewModel">The LightViewModel instance to be mapped onto a new LightValueObject instance</param>
        /// <returns>A new LightValueObject instance</returns>
        public static T FactoryFrom(ILightViewModel lightViewModel)
        {
            var valueObject = new T();
            valueObject.MapFrom(lightViewModel);
            return valueObject;
        }

        /// <summary>
        /// The method receive the error code to add on errors list of validation.
        /// </summary>
        /// <param name="error">The error code</param>
        protected void AddModelValidationErrorCode(string error)
        {
            InputErrors.TryAdd(error, null);
        }

        /// <summary>
        /// The method receive the error code to add on errors list of validation.
        /// </summary>
        /// <param name="error">The error code</param>
        /// <param name="message">The code message</param>
        protected void AddModelValidationErrorCode(string error, string message)
        {
            InputErrors.TryAdd(error, new[] { message });
        }

        /// <summary>
        /// The method receive the error code to add on errors list of validation.
        /// </summary>
        /// <param name="error">The error code</param>
        /// <param name="args">Arguments to interpolate</param>
        protected void AddModelValidationErrorCode(string error, params object[] args)
        {
            InputErrors.TryAdd(error, args);
        }

        /// <summary>
        /// The properties used to return the InputValidator.
        /// </summary>
        [SwaggerIgnore]
        [JsonIgnore]
        public Validator<T> Validator { get; } = new();
        public bool ShouldSerializeValidator() { return false; }

        /// <summary>
        /// The method used to input validation of ViewModel.
        /// </summary>
        ///  <remarks>Must be implemented in each derived class.</remarks>
        public abstract void Validate();

        /// <summary>
        /// The method used to input validation of ViewModel on FluentValidation.
        /// </summary>
        /// <typeparam name="TProperty"></typeparam>
        /// <param name="expression"></param>
        /// <returns></returns>
        protected IRuleBuilderInitial<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
        {
            return Validator.RuleFor(expression);
        }

        /// <summary>
        /// Method used for mapping between Model to ViewModel 
        /// </summary>
        /// <param name="toMapData">ViewModel object to map</param>
        /// <param name="mapNulls">Indicates whether null values should be mapped</param>
        /// <returns></returns>
        public void MapFrom(ILightViewModel toMapData, bool mapNulls = false)
        {
            this.DynamicMapper(toMapData, mapNulls);
        }

        private void DynamicMapper(dynamic toMapData, bool mapNulls)
        {
            if (toMapData is null)
                return;

            try
            {

                //By reflection, browse viewModel by identifying all attributes and lists for validation.  
                foreach (FieldInfo toMapField in toMapData.GetType().GetFields())
                {
                    if ((toMapField.Name == "TransactionContext" && toMapField.FieldType.Name == "ILightContext") ||
                        (toMapField.Name == "Validator" && toMapField.FieldType.BaseType.Name.StartsWith("AbstractValidator")))
                        continue;

                    FindAndSetField(this, toMapField, toMapField.GetValue(toMapData), mapNulls);
                }

                //By reflection, browse viewModel by identifying all attributes and lists for validation.  
                foreach (PropertyInfo toMapProp in toMapData.GetType().GetProperties())
                {
                    if ((toMapProp.Name == "TransactionContext" && toMapProp.PropertyType.Name == "ILightContext") ||
                        (toMapProp.Name == "Validator" && toMapProp.PropertyType.BaseType.Name.StartsWith("AbstractValidator")))
                        continue;

                    FindAndSetProperty(this, toMapProp, toMapProp.GetValue(toMapData), mapNulls);
                }
            }
            catch (Exception e)
            {
                throw new LightException("Error during dynamic mapping objects", e);
            }
        }

        private static void FindAndSetField(dynamic thisData, FieldInfo toMapField, dynamic valueToMap, bool mapNulls)
        {
            if (valueToMap is null && !mapNulls)
                return;

            //By reflection, browse viewModel by identifying all attributes and lists for validation.  
            foreach (FieldInfo thisField in thisData.GetType().GetFields())
            {
                if (thisField.Name.Equals(toMapField.Name))
                {
                    if (thisField.FieldType.FullName.Equals(toMapField.FieldType.FullName))
                    {
                        thisField.SetValue(thisData, valueToMap);
                        break;
                    }
                    else if (thisField.FieldType.Namespace == "System.Collections.Generic" &&
                             toMapField.FieldType.Namespace == "System.Collections.Generic")
                    {
                        var thisList = thisField.GetValue(thisData);
                        if (thisList is null)
                        {
                            thisList = thisField.FieldType.GetConstructors()[0].Invoke(default);
                            thisField.SetValue(thisData, thisList);
                        }
                        MapFieldList(thisField, thisList, toMapField, valueToMap);
                    }
                    else if (toMapField.FieldType.BaseType.Name.Contains("LightViewModel"))
                    {
                        var methodInfo = thisField.FieldType.GetMethod("MapFrom");
                        if (methodInfo is not null)
                        {
                            var thisValue = thisField.GetValue(thisData);
                            if (thisValue is null)
                            {
                                thisValue = thisField.FieldType.GetConstructors()[0].Invoke(default);
                                thisField.SetValue(thisData, thisValue);
                            }

                            methodInfo.Invoke(thisValue, new object[] { valueToMap, mapNulls });
                            break;
                        }
                    }
                    else if (toMapField.FieldType.BaseType?.Name.Contains("LightEnum") == true || 
                             toMapField.FieldType.BaseType?.Name.Contains("LightLocalizedEnum") == true)
                    {
                        thisField.SetValue(thisData, valueToMap.ToString());
                        break;
                    }
                    else if (toMapField.FieldType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Position") &&
                             thisField.FieldType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Point"))
                    {
                        thisField.SetValue(thisData, valueToMap = thisField.FieldType.GetConstructors()[1].Invoke(new[] { valueToMap }));
                        break;
                    }
                }
            }
        }

        private static void FindAndSetProperty(dynamic thisData, PropertyInfo toMapProp, dynamic valueToMap, bool mapNulls)
        {
            if (valueToMap is null && !mapNulls)
                return;

            //By reflection, browse viewModel by identifying all attributes and lists for validation.  
            foreach (PropertyInfo thisProp in thisData.GetType().GetProperties())
            {
                if (thisProp.Name.Equals(toMapProp.Name))
                {
                    if (thisProp.PropertyType.FullName.Equals(toMapProp.PropertyType.FullName))
                    {
                        thisProp.SetValue(thisData, valueToMap);
                        break;
                    }
                    else if (thisProp.PropertyType.Namespace == "System.Collections.Generic" &&
                             toMapProp.PropertyType.Namespace == "System.Collections.Generic")
                    {
                        var thisList = thisProp.GetValue(thisData);
                        if (thisList is null)
                        {
                            thisList = thisProp.PropertyType.GetConstructors()[0].Invoke(default);
                            thisProp.SetValue(thisData, thisList);
                        }
                        MapPropertyList(thisProp, thisList, toMapProp, valueToMap);
                    }
                    else if (toMapProp.PropertyType.BaseType.Name.Contains("LightViewModel"))
                    {
                        var methodInfo = thisProp.PropertyType.GetMethod("MapFrom");
                        if (methodInfo is not null)
                        {
                            var thisValue = thisProp.GetValue(thisData);
                            if (thisValue is null)
                            {
                                thisValue = thisProp.PropertyType.GetConstructors()[0].Invoke(default);
                                thisProp.SetValue(thisData, thisValue);
                            }

                            methodInfo.Invoke(thisValue, new object[] { valueToMap, mapNulls });
                            break;
                        }
                    }
                    else if (toMapProp.PropertyType.BaseType?.Name.Contains("LightEnum") == true || 
                             toMapProp.PropertyType.BaseType?.Name.Contains("LightLocalizedEnum") == true)
                    {
                        thisProp.SetValue(thisData, valueToMap.ToString());
                        break;
                    }
                    else if (toMapProp.PropertyType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Position") &&
                             thisProp.PropertyType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Point"))
                    {
                        thisProp.SetValue(thisData, valueToMap = thisProp.PropertyType.GetConstructors()[1].Invoke(new[] { valueToMap }));
                        break;
                    }
                }
            }
        }

        private static void MapFieldList(FieldInfo thisField, dynamic thisList, FieldInfo toMapField, dynamic listToMap)
        {
            var thisListType = thisField.FieldType.GetGenericArguments()[0];
            var ToMapListType = toMapField.FieldType.GetGenericArguments()[0];

            MethodInfo methodInfo = null;
            foreach (var staticMethod in thisListType.BaseType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (staticMethod.Name == "FactoryFrom" && staticMethod.GetParameters().ToList().Find(p => p.ParameterType.IsAssignableFrom(ToMapListType.BaseType)) is not null)
                {
                    methodInfo = staticMethod;
                    break;
                }
            };

            if (methodInfo is not null)
            {
                thisField.FieldType.GetMethod("Clear").Invoke(thisList, null);
                foreach (var itemToMap in listToMap)
                {
                    var thisItem = methodInfo.Invoke(null, new object[] { itemToMap });
                    thisField.FieldType.GetMethod("Add").Invoke(thisList, new[] { thisItem });
                }
            }
        }

        private static void MapPropertyList(PropertyInfo thisProp, dynamic thisList, PropertyInfo toMapProp, dynamic listToMap)
        {
            var thisListType = thisProp.PropertyType.GetGenericArguments()[0];
            var ToMapListType = toMapProp.PropertyType.GetGenericArguments()[0];

            MethodInfo methodInfo = null;
            foreach (var staticMethod in thisListType.BaseType.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                if (staticMethod.Name == "FactoryFrom" && staticMethod.GetParameters().ToList().Find(p => p.ParameterType.IsAssignableFrom(ToMapListType.BaseType)) is not null)
                {
                    methodInfo = staticMethod;
                    break;
                }
            };

            if (methodInfo is not null)
            {
                thisProp.PropertyType.GetMethod("Clear").Invoke(thisList, null);
                foreach (var itemToMap in listToMap)
                {
                    var thisItem = methodInfo.Invoke(null, new object[] { itemToMap });
                    thisProp.PropertyType.GetMethod("Add").Invoke(thisList, new[] { thisItem });
                }
            }
        }

        private void ValidateInstances()
        {
            if (!typeof(T).IsInstanceOfType(this))
            {
                throw new TypeInitializationException($"**** CHECK declaration of type {GetType().FullName}", null);
            }
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
