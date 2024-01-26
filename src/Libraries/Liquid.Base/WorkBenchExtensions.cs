using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Liquid
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    public static class WorkBenchExtensions
    {
        public static object CloneService(this object objSource)
        {
            //Get the type of source object and create a new instance of that type...
            Type typeSource = objSource?.GetType();

            object objTarget = Activator.CreateInstance(typeSource);

            //Get all the properties of source object type
            PropertyInfo[] propertyInfo = typeSource.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            //Assign all source property to taget object 's properties
            foreach (PropertyInfo property in propertyInfo)
            {
                //Check whether property can be written to
                if (property.CanWrite)
                    //check whether property type is value type, enum or string type
                    if (property.PropertyType.IsValueType || property.PropertyType.IsEnum || property.PropertyType.Equals(typeof(System.String)))
                        property.SetValue(objTarget, property.GetValue(objSource, null), null);

                    //else property type is object/complex types, so need to recursively call this method until the end of the tree is reached
                    else
                    {
                        object objPropertyValue = property.GetValue(objSource, null);

                        if (objPropertyValue is null)
                            property.SetValue(objTarget, null, null);
                        else
                            property.SetValue(objTarget, objPropertyValue.CloneService(), null);
                    }
            }
            return objTarget;
        }

        /// <summary>
        /// Performs command from commandline arguments 
        /// (to be called from Program.cs at Program.Main() method)
        /// </summary>
        /// <param name="host">IwebHost (built) instance</param>
        /// <param name="args">Commandline arguments</param>
        /// <param name="isReactiveHub">Indication whether the microservice works as a ReactiveHub</param>
        /// <returns>True when microservice should continue (start running)</returns>
        public static bool ProcessCommands(this IWebHost host, string[] args, bool isReactiveHub = false)
        {
            string first = args?.FirstOrDefault();

            if (string.IsNullOrEmpty(first))
                return true;

            else
            {
                string[] others = args?.Length == 1 ? Array.Empty<string>() : args.ToList().GetRange(1, args.Length - 1).ToArray();

                Console.WriteLine("");
                if (first == "GenerateSwagger")
                    return FactorySetupCommand(host, first, others, isReactiveHub).Execute();

                else
                {
                    Console.WriteLine("Unknown arguments: {0}", args);
                    Console.WriteLine("");
                    Console.WriteLine("Valid arguments are:");
                    Console.WriteLine("");
                    Console.WriteLine("'dotnet run GenerateSwagger [fullDomainName] [releaseName]' to generate OpenAPI specification as swagger.json file in the storage for 'devops artifacts'.");
                    Console.WriteLine("'dotnet run' (without arguments) to start the microservice");
                    return false;
                }
            }
        }

        private static IWorkBenchCommand FactorySetupCommand(IWebHost host, string first, string[] others, bool isReactiveHub)
        {
            foreach (string assemblyPath in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "Liquid*.dll"))
                foreach (Type t in Assembly.LoadFrom(assemblyPath).GetTypes())
                    if (t.Name == $"{first}Command")
                        return Activator.CreateInstance(t, host, others, isReactiveHub) as IWorkBenchCommand;

            return default;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
