using Liquid.Interfaces;

namespace Liquid.Domain
{
    /// <summary>
    /// Entity that carrys the error code and critic type 
    /// to the inputErrors to build an InvalidInputLightException
    /// </summary>
    public class Critic : ICritic
    {
        /// <summary>
        /// The code of the business critic (language independent)
        /// </summary>
        public string Code { get; set; }
        /// <summary>
        /// The localized message of the business critic)
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// The (severity) type of the business critic (1, 2 or 3)
        /// </summary>
        public CriticType Type { get; set; }
        /// <summary>
        /// The label of the (severity) type of the business critic (Error, Warning or Info)
        /// </summary>
        public string Label { get => Type.ToString(); }
        /// <summary>
        /// Adding a business error to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        public void AddError(string code)
        {
            Code = code;
            Message = code;
            Type = CriticType.Error;
        }
        /// <summary>
        /// Adding a business error to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        /// <param name="message"></param>
        public void AddError(string code, string message)
        {
            Code = code;
            Message = message;
            Type = CriticType.Error;
        }
        /// <summary>
        /// Adding a business info to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        public void AddInfo(string code)
        {
            Code = code;
            Message = code;
            Type = CriticType.Info;
        }
        /// <summary>
        /// Adding a business info to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        /// <param name="message"></param>
        public void AddInfo(string code, string message)
        {
            Code = code;
            Message = message;
            Type = CriticType.Info;
        }
        /// <summary>
        /// Adding a business warning to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        public void AddWarning(string code)
        {
            Code = code;
            Message = code;
            Type = CriticType.Warning;
        }
        /// <summary>
        /// Adding a business warning to the critic
        /// </summary>
        /// <param name="code">Code to be added to critic</param>
        /// <param name="message"></param>
        public void AddWarning(string code, string message)
        {
            Code = code;
            Message = message;
            Type = CriticType.Warning;
        }
    }
}