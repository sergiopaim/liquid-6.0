namespace Liquid.Domain
{
    /// <summary>
    /// Abstract enumeration type with localized labels
    /// </summary>
    /// <typeparam name="T">The type of the enum</typeparam>
    public abstract class LightLocalizedEnum<T> : LightEnum<T> where T : LightLocalizedEnum<T>
    {
        /// <summary>
        /// The localized label associated to the enum code
        /// </summary>
        public string Label => LightLocalizer.Localize(GetType().Name.ToUpper() + "_" + Code.ToUpper());

        /// <summary>
        /// Creates an enum instance for the given code
        /// </summary>
        /// <param name="code">the enum code</param>
        protected LightLocalizedEnum(string code) : base(code) { }
    }
}