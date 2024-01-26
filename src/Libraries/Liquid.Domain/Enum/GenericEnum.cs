namespace Liquid.Domain
{
    /// <summary>
    /// Generic enum for deserializing LightEnums from multiple times
    /// </summary>
    public class GenericEnum
    {
        /// <summary>
        /// Creates a GenericEnum with code and label
        /// </summary>
        /// <param name="code"></param>
        /// <param name="label"></param>
        public GenericEnum(string code, string label)
        {
            Code = code;
            Label = label;
        }
        /// <summary>
        /// Creates a GenericEnum with code only
        /// </summary>
        /// <param name="code"></param>
        public GenericEnum(string code)
        {
            Code = code;
        }
        /// <summary>
        /// Creates a GenericEnum with code only
        /// </summary>
        public GenericEnum() { }

        /// <summary>
        /// The code of the enum 
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// The localized label associated to the enum code
        /// </summary>
        public string Label { get; set; }
    }
}