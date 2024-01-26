using System;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Attribute used for connect a Queue.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class JobAttribute : Attribute
    {
        public LightJobFrequency Frequency { get; }
        public int? DayOfMonth { get; }
        public DayOfWeek? DayOfWeek { get; }
        public int? Hour { get; }
        public int? Minute { get; }
        public int PartitionCount { get; }

        public JobAttribute(string frequency, int dayOfMonth = -1, string dayOfWeek = null, int hour = -1, int minute = -1, int partitionCount = 1)
        {
            frequency = frequency[0..1].ToLower() + frequency[1..];

            if (!LightJobFrequency.IsValid(frequency))
                throw new ArgumentException($"Invalid frequency code {frequency}.", nameof(frequency));
            if (dayOfMonth != -1 && Math.Clamp(dayOfMonth, 1, 31) != dayOfMonth)
                throw new ArgumentException($"Invalid day of month value {dayOfMonth}. Valid range: [1, 31]", nameof(dayOfMonth));
            if (hour != -1 && Math.Clamp(hour, 0, 23) != hour)
                throw new ArgumentException($"Invalid hour value {hour}. Valid range: [0, 23]", nameof(hour));
            if (minute != -1 && Math.Clamp(minute, 0, 59) != minute)
                throw new ArgumentException($"Invalid minute value {minute}. Valid range: [0, 59]", nameof(minute));

            Frequency = LightJobFrequency.OfCode(frequency);
            DayOfMonth = dayOfMonth == -1 ? (int?)null : dayOfMonth;
            DayOfWeek = !string.IsNullOrEmpty(dayOfWeek) ? Enum.Parse<DayOfWeek>(dayOfWeek) : (DayOfWeek?)null;
            Hour = hour == -1 ? (int?)null : hour;
            Minute = minute == -1 ? (int?)null : minute;
            PartitionCount = partitionCount;
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
