using System;

namespace Syadeu.Mono.Creature
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class CreatureDataAttribute : Attribute { }
}