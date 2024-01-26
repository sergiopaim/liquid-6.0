using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Threading;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
namespace Liquid.Base.Test
{
    public static class AdjustableClock
    {
        const string DISPLACEMENT_HEADER = "Clock-Displacement";
        const string DISPLACEMENT_QUERY = "clockDisplacement";
        private static readonly AsyncLocal<long?> displacement = new();

        public static long? Displacement
        {
            get 
            {
                if (WorkBench.IsProductionEnvironment)
                    return null;
                return displacement.Value;
            }
            set
            {
                if (!WorkBench.IsProductionEnvironment)
                    displacement.Value = value;
            }
        }

        public static DateTime UtcNow
        {
            get
            {
                if (Displacement.HasValue)
                    return DateTime.UtcNow.AddMilliseconds(Displacement.Value);
                else
                    return DateTime.UtcNow;
            }

            internal set
            {
                if (value == default)
                    Displacement = null;
                else
                    Displacement = (long?)Math.Round((value - DateTime.UtcNow).TotalMilliseconds);
            }
        }

        public static DateTime Today => UtcNow.Date;
        public static DateTime Now => UtcNow.ToLocalTime();

        public static void AdjustByRequest(HttpRequest request)
        {
            if (WorkBench.IsProductionEnvironment)
                return;

            string requestValue = null;
            if (request.Headers[DISPLACEMENT_HEADER].Count == 1)
                requestValue = request.Headers[DISPLACEMENT_HEADER].ToString();
            else if (request.Query[DISPLACEMENT_QUERY].Count == 1)
                requestValue = request.Query[DISPLACEMENT_QUERY].ToString();

            if (requestValue is not null)
            {
                if (long.TryParse(requestValue, out long longValue))
                    Displacement = longValue;
                else if (DateTime.TryParse(requestValue, out DateTime datetimeValue))
                    UtcNow = datetimeValue;
            }
        }

        public static void ApplyHeader(HttpClient client)
        {
            if (!WorkBench.IsProductionEnvironment && Displacement.HasValue)
                client.DefaultRequestHeaders.Add(DISPLACEMENT_HEADER, Displacement.Value.ToString());
        }
    }
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member