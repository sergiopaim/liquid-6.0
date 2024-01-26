using Liquid.Domain;
using Liquid.Interfaces;
using System;

namespace Liquid.Activation
{
    /// <summary>
    /// Class created to apply a event inheritance to use a liquid framework
    /// </summary> 
    public abstract class LightReactiveEvent<TEvent> : LightViewModel<TEvent>, ILightReactiveEvent
        where TEvent : LightReactiveEvent<TEvent>, ILightReactiveEvent, new()
    { }

}