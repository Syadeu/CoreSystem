using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;

namespace Syadeu.Database
{
    internal class BaseSpecifiedConcreteClassConverter<T> : DefaultContractResolver
    {
        public static readonly JsonSerializerSettings SpecifiedSubclassConversion
            = new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter<T>() };

        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(T).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null; // pretend TableSortRuleConvert is not specified (thus avoiding a stack overflow)
            return base.ResolveContractConverter(objectType);
        }
    }
}
