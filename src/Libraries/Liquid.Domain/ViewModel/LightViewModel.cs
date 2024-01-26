using FluentValidation;
using Liquid.Base;
using Liquid.Interfaces;
using Liquid.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Resources;
using System.Text.Json.Serialization;

[assembly: NeutralResourcesLanguage("en")]

namespace Liquid.Domain
{
    /// <summary>
    /// General data class to communicate through REST API
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class LightViewModel<T> : ILightViewModel where T : LightViewModel<T>, ILightViewModel, new()
    {
        /// <summary>
        /// Constructor
        /// </summary>
        protected LightViewModel()
        {
            ValidateInstances();
        }

        /// <summary>
        /// Factories an instance of LightViewModel from a ILightModel instance
        /// </summary>
        /// <param name="lightModel">The LightModel instance to be mapped onto a new LightViewModel instance</param>
        /// <returns>A new LightViewModel instance</returns>
        public static T FactoryFrom(ILightModel lightModel)
        {
            var viewModel = new T();
            viewModel.MapFrom(lightModel);
            return viewModel;
        }

        /// <summary>
        /// Factories an instance of LightViewModel from a ILightModel instance
        /// </summary>
        /// <param name="lightValueObject">The LightValueObject instance to be mapped onto a new LightViewModel instance</param>
        /// <returns>A new LightViewModel instance</returns>
        public static T FactoryFrom(ILightValueObject lightValueObject)
        {
            var viewModel = new T();
            viewModel.MapFrom(lightValueObject);
            return viewModel;
        }

        /// <summary>
        /// Factories an instance of LightViewModel from another ILightViewModel instance
        /// </summary>
        /// <param name="lightViewModel">The LightViewModel instance to be mapped onto a new LightViewModel instance</param>
        /// <returns>A new LightViewModel instance</returns>
        public static T FactoryFrom(ILightViewModel lightViewModel)
        {
            var viewModel = new T();
            viewModel.MapFrom(lightViewModel);
            return viewModel;
        }

        /// <summary>
        /// Validation errors 
        /// </summary>
        [SwaggerIgnore]
        [JsonIgnore]
        public Dictionary<string, object[]> InputErrors { get; set; }

        /// <summary>
        /// The method receive the error code to add on errors list of input validation.
        /// </summary>
        /// <param name="error">The code error</param>
        protected void AddInputError(string error)
        {
            InputErrors.TryAdd(error, null);
        }

        /// <summary>
        /// The method receive the error code to add on errors list of input validation.
        /// </summary>
        /// <param name="error">The error code</param>
        /// <param name="message">The code message</param>
        protected void AddInputError(string error, string message)
        {
            InputErrors.TryAdd(error, new[] { message });
        }

        /// <summary>
        /// The method receive the error code to add on errors list of input validation.
        /// </summary>
        /// <param name="error">The error code</param>
        /// <param name="args">Arguments to interpolate</param>
        protected void AddInputError(string error, params object[] args)
        {
            InputErrors.TryAdd(error, args);
        }

        /// <summary>
        /// The properties used to return the InputValidator.
        /// </summary>
        [SwaggerIgnore]
        [JsonIgnore]
        public InputValidator<T> Validator { get; } = new();

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
        ///  Method used to map data from ViewModel to Model.
        /// </summary>
        /// <param name="toMapData">Model object to map</param>
        /// <param name="mapNulls">Indicates whether null values should be mapped</param>
        /// <returns></returns>
        public void MapFrom(ILightModel toMapData, bool mapNulls = false)
        {
            this.DynamicMapper(toMapData, mapNulls);
        }

        /// <summary>
        /// Method used to map data from ViewModel to ViewModel.
        /// </summary>
        /// <param name="toMapData">ViewModel object to map</param>
        /// <param name="mapNulls">Indicates whether null values should be mapped</param>
        /// <returns></returns>
        public void MapFrom(ILightViewModel toMapData, bool mapNulls = false)
        {
            this.DynamicMapper(toMapData, mapNulls);
        }

        /// <summary>
        /// Method used to map data from ViewModel to ViewModel.
        /// </summary>
        /// <param name="toMapData">ValueObject object to map</param>
        /// <param name="mapNulls">Indicates whether null values should be mapped</param>
        /// <returns></returns>
        public void MapFrom(ILightValueObject toMapData, bool mapNulls = false)
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
                    else if (toMapField.FieldType.BaseType.Name.Contains("LightModel") ||
                             toMapField.FieldType.BaseType.Name.Contains("LightOptimisticModel") ||
                             toMapField.FieldType.BaseType.Name.Contains("LightDataEventModel") ||
                             toMapField.FieldType.BaseType.Name.Contains("LightValueObject") ||
                             toMapField.FieldType.BaseType.Name.Contains("LightViewModel"))
                    {
                        MethodInfo methodInfo = null;
                        foreach (var method in thisField.FieldType.BaseType.GetMethods())
                        {
                            if (method.Name == "MapFrom" && method.GetParameters().ToList().Find(p => p.ParameterType.IsAssignableFrom(toMapField.FieldType.BaseType)) is not null)
                            {
                                methodInfo = method;
                                break;
                            }
                        };
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
                    else if (thisField.FieldType.BaseType?.Name.Contains("LightEnum") == true)
                    {
                        thisField.SetValue(thisData, Convert.ChangeType(thisField.FieldType.BaseType.GetMethod("OfCode").Invoke(thisData, new[] { valueToMap }), thisField.FieldType));
                        break;
                    }
                    else if (thisField.FieldType.BaseType?.Name.Contains("LightLocalizedEnum") == true)
                    {
                        thisField.SetValue(thisData, Convert.ChangeType(thisField.FieldType.BaseType.BaseType.GetMethod("OfCode").Invoke(thisData, new[] { valueToMap }), thisField.FieldType));
                        break;
                    }

                    else if (toMapField.FieldType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Point") &&
                             thisField.FieldType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Position"))
                    {
                        thisField.SetValue(thisData, valueToMap?.Position);
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
                    else if (toMapProp.PropertyType.BaseType.Name.Contains("LightModel") ||
                             toMapProp.PropertyType.BaseType.Name.Contains("LightOptimisticModel") ||
                             toMapProp.PropertyType.BaseType.Name.Contains("LightDataEventModel") ||
                             toMapProp.PropertyType.BaseType.Name.Contains("LightValueObject") ||
                             toMapProp.PropertyType.BaseType.Name.Contains("LightViewModel"))
                    {
                        MethodInfo methodInfo = null;
                        foreach (var method in thisProp.PropertyType.BaseType.GetMethods())
                        {
                            if (method.Name == "MapFrom" && method.GetParameters().ToList().Find(p => p.ParameterType.IsAssignableFrom(toMapProp.PropertyType.BaseType)) is not null)
                            {
                                methodInfo = method;
                                break;
                            }
                        };

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
                    else if (thisProp.PropertyType.BaseType?.Name.Contains("LightEnum") == true)
                    {
                        thisProp.SetValue(thisData, Convert.ChangeType(thisProp.PropertyType.BaseType.GetMethod("OfCode").Invoke(thisData, new[] { valueToMap }), thisProp.PropertyType));
                        break;
                    }
                    else if (thisProp.PropertyType.BaseType?.Name.Contains("LightLocalizedEnum") == true)
                    {
                        thisProp.SetValue(thisData, Convert.ChangeType(thisProp.PropertyType.BaseType.BaseType.GetMethod("OfCode").Invoke(thisData, new[] { valueToMap }), thisProp.PropertyType));
                        break;
                    }
                    else if (toMapProp.PropertyType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Point") &&
                             thisProp.PropertyType.FullName.StartsWith("Microsoft.Azure.Cosmos.Spatial.Position"))
                    {
                        thisProp.SetValue(thisData, valueToMap?.Position);
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
}
