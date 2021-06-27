using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

using System;

namespace Syadeu.Database
{
    internal class BaseSpecifiedConcreteClassConverter : DefaultContractResolver
    {
        public static readonly JsonSerializerSettings SpecifiedSubclassConversion
            = new JsonSerializerSettings() { ContractResolver = new BaseSpecifiedConcreteClassConverter() };

        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            if (typeof(ValuePair).IsAssignableFrom(objectType) && !objectType.IsAbstract)
                return null; // pretend TableSortRuleConvert is not specified (thus avoiding a stack overflow)
            return base.ResolveContractConverter(objectType);
        }
    }
}
