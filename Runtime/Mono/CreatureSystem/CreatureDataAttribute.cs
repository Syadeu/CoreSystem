using System;

namespace Syadeu.Mono.Creature
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class CreatureDataAttribute : Attribute
    {
        public string SingleToneName = null;
        public string DataArrayName = null;
    }
}