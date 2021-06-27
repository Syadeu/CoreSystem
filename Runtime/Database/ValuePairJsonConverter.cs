using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

namespace Syadeu.Database
{
    internal class ValuePairJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;

        public override bool CanConvert(Type objectType) => objectType == typeof(ValuePair);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotImplementedException();

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            if (!jo.TryGetValue("m_Value", out JToken value))
            {
                return JsonConvert.DeserializeObject<ValueNull>(jo.ToString(),
                    BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            }

            if (value.Type == JTokenType.Boolean)
            {
                return JsonConvert.DeserializeObject<SerializableBoolValuePair>(jo.ToString(),
                    BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            }
            else if (value.Type == JTokenType.Float)
            {
                return JsonConvert.DeserializeObject<SerializableDoubleValuePair>(jo.ToString(),
                    BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            }
            else if (value.Type == JTokenType.Integer)
            {
                return JsonConvert.DeserializeObject<SerializableIntValuePair>(jo.ToString(),
                    BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            }
            else if (value.Type == JTokenType.String)
            {
                return JsonConvert.DeserializeObject<SerializableStringValuePair>(jo.ToString(),
                    BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            }
            else if (value.Type == JTokenType.Array)
            {
                var temp = JsonConvert.DeserializeObject<SerializableArrayValuePair>(jo.ToString(),
                    BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);

                if (temp.m_Value.Count > 0 && temp.m_Value[0].GetType().GetElementType() == null)
                {
                    if (int.TryParse(temp.m_Value[0].ToString(), out int _))
                    {
                        List<int> tempList = new List<int>();
                        for (int i = 0; i < temp.m_Value.Count; i++)
                        {
                            tempList.Add(int.Parse(temp.m_Value[i].ToString()));
                        }
                        temp.m_Value = tempList;
                    }
                    else if (double.TryParse(temp.m_Value[0].ToString(), out double _))
                    {
                        List<double> tempList = new List<double>();
                        for (int i = 0; i < temp.m_Value.Count; i++)
                        {
                            tempList.Add(double.Parse(temp.m_Value[i].ToString()));
                        }
                        temp.m_Value = tempList;
                    }
                    else if (bool.TryParse(temp.m_Value[0].ToString(), out bool _))
                    {
                        List<bool> tempList = new List<bool>();
                        for (int i = 0; i < temp.m_Value.Count; i++)
                        {
                            tempList.Add(bool.Parse(temp.m_Value[i].ToString()));
                        }
                        temp.m_Value = tempList;
                    }
                    else
                    {
                        List<string> tempList = new List<string>();
                        for (int i = 0; i < temp.m_Value.Count; i++)
                        {
                            tempList.Add(temp.m_Value[i].ToString());
                        }
                        temp.m_Value = tempList;
                    }
                }

                return temp;
            }
            else
            {
                return JsonConvert.DeserializeObject<ValueNull>(jo.ToString(),
                    BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            }
        }
    }
}
