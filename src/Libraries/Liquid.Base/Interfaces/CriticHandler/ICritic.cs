namespace Liquid.Interfaces
{
    /// <summary>
    /// Enum delegates to create the Critic Type, There are three types, Error, Warning and Info
    /// </summary> 
    public enum CriticType 
    {
        /// <summary>
        /// Error messages related to business rules violation
        /// </summary>
        Error = 1,
        /// <summary>
        /// Warning messages related to business rules or outcomes
        /// </summary>
        Warning = 2, 
        /// <summary>
        /// Information messages related to business rules or outcomes
        /// </summary>
        Info = 3
    };

    /// <summary>
    /// Interface delegates to create the Critic
    /// </summary> 
    public interface ICritic
    {
        /// <summary>
        /// The code of the business critic (language independent)
        /// </summary>
        public string Code { get; }
        /// <summary>
        /// The localized message of the business critic)
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// The (severity) type of the business critic (1, 2 or 3)
        /// </summary>
        public CriticType Type { get; }
        /// <summary>
        /// The label of the (severity) type of the business critic (Error, Warning or Info)
        /// </summary>
        string Label { get; }
        /// <summary>
        /// Adding a business error to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        void AddError(string code);
        /// <summary>
        /// Adding a business info to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        void AddInfo(string code);
        /// <summary>
        /// Adding a business warning to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        void AddWarning(string code);
    }
}