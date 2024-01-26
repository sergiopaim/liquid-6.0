using Liquid.Base;
using Liquid.Interfaces;
using System;
using System.Collections.Generic;

namespace Liquid.Repository
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LightPaging<T> : ILightPaging<T>
    {
        /// <summary>
        /// Entity data
        /// </summary>
        public ICollection<T> Data { get; set; }
        /// <summary>
        /// Number of registers per page
        /// </summary>
        public int ItemsPerPage { get; set; }
        /// <summary>
        /// Database index of the current page to get the next one
        /// </summary>
        public string ContinuationToken { get; set; }

        /// <summary>
        /// Creates a new LightPaging of LightViewModel from a LightPaging
        /// </summary>
        /// <typeparam name="U">A LightViewModel type</typeparam>
        /// <param name="origin">The origin LightPaging</param>
        /// <param name="conversion">The function to convert ILightModel instances to T instances <para>NOTE: if null is returned, no T instance from ILightModel instance will be inserted in the returned LightPaging.Data</para></param>
        /// <returns>The new LightPaging</returns>
        public static LightPaging<T> FactoryFrom<U>(ILightPaging<U> origin, Func<U, T> conversion) where U : ILightModel
        {
            if (origin is null)
                throw new LightException("origin parameter cannot be null");
            if (conversion is null)
                throw new LightException("lambda expression parameter cannot be null");

            var newPaging = new LightPaging<T>
            {
                Data = new List<T>(),
                ItemsPerPage = origin.ItemsPerPage,
                ContinuationToken = origin.ContinuationToken
            };

            foreach (var item in origin.Data)
            {
                var converted = conversion.Invoke(item);

                if (converted is not null)
                    newPaging.Data.Add(converted);
            }

            return newPaging;
        }
    }
}
