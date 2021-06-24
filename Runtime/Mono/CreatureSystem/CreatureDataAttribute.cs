using System;

namespace Syadeu.Mono.Creature
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CreatureDataAttribute : Attribute
    {
        [Obsolete] public string SingleToneName = null;
        [Obsolete] public string DataArrayName = null;
    }
}