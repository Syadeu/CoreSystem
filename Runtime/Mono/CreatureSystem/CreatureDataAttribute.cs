using System;

namespace Syadeu.Mono.Creature
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CreatureDataAttribute : Attribute
    {
        public string SingleToneName = null;
        public string DataArrayName = null;
    }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class CreatureStatDataAttribute : Attribute
    {
        public int Idx = 0;
    }
}