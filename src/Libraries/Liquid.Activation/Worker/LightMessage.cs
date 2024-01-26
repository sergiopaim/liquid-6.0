using Liquid.Base;
using Liquid.Base.Test;
using Liquid.Domain;
using Liquid.Interfaces;
using Liquid.Runtime;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Json.Serialization;

namespace Liquid.Activation
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
    /// <summary>
    /// Class created to apply a message inheritance to use a liquid framework
    /// </summary> 
    public abstract class LightMessage<TMessage, TCommand> : LightViewModel<TMessage>, ILightMessage
        where TMessage : LightMessage<TMessage, TCommand>, ILightMessage, new()
        where TCommand : ILightEnum
    {
        private ILightContext _context;
        private TCommand _commandType;

        public string CommandType
        {
            get => _commandType.Code;
            set
            {
                var command = Activator.CreateInstance(typeof(TCommand), value);
                _commandType = (TCommand)command;
            }
        }

        [JsonIgnore]
        public ILightContext TransactionContext
        {
            get
            {
                if (_context is null)
                {
                    CheckContext(TokenJwt);
                }
                return _context;
            }
            set
            {
                _context = value;
            }
        }

        public string OperationId
        {
            get
            {
                CheckContext(null);
                return _context.OperationId;
            }
            set
            {
                CheckContext(null);
                _context.OperationId = value;
            }
        }

        public virtual string TokenJwt
        {
            get
            {
                CheckContext(null);
                string ret = JwtSecurityCustom.GetJwtToken((ClaimsIdentity)TransactionContext?.User?.Identity);

                if (string.IsNullOrEmpty(ret))
                    return JwtSecurityCustom.Config?.SysAdminJWT;
                else
                    return ret;
            }
            set
            {
                CheckContext(value);
            }
        }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public long? ClockDisplacement
        {
            get => AdjustableClock.Displacement;
            set => AdjustableClock.Displacement = value;
        }

        /// <summary>
        /// Verify if context was received otherwise create the context with mock
        /// </summary>
        /// <param name="token">Token</param> 
        private void CheckContext(string token)
        {
            _context ??= new LightContext
            {
                OperationId = WorkBench.GenerateNewOperationId()
            };
            _context.User ??= JwtSecurityCustom.DecodeToken(token);
        }

        /// <summary>
        /// Returns the list of UserProperties for use in topic subscription filters
        /// </summary>
        /// <returns>List of properties as pairs of key and value</returns>
        public virtual Dictionary<string, object> GetUserProperties()
        {
            return new();
        }
    }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}