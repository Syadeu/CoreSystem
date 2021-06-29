using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;
using System.Collections;
using System.Collections.Generic;

namespace Syadeu.Database
{
    internal class ValuePairJsonConverter : JsonConverter
    {
        public override bool CanWrite => true;
        public override bool CanRead => true;

        public override bool CanConvert(Type objectType) => objectType == typeof(ValuePair);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            ValuePair valuePair = (ValuePair)value;
            ValueType valueType = valuePair.GetValueType();

            writer.WriteStartObject();
            writer.WritePropertyName("ValueType");
            writer.WriteValue(valueType.ToString());

            writer.WritePropertyName("Name");
            writer.WriteValue(valuePair.Name);

            switch (valueType)
            {
                case ValueType.Int32:
                    writer.WritePropertyName("Value");
                    writer.WriteValue(valuePair.GetValue<int>());
                    break;
                case ValueType.Double:
                    writer.WritePropertyName("Value");
                    writer.WriteValue(valuePair.GetValue<double>());
                    break;
                case ValueType.String:
                    writer.WritePropertyName("Value");
                    writer.WriteValue(valuePair.GetValue<string>());
                    break;
                case ValueType.Boolean:
                    writer.WritePropertyName("Value");
                    writer.WriteValue(valuePair.GetValue<bool>());
                    break;
                case ValueType.Array:
                    SerializableArrayValuePair arr = valuePair as SerializableArrayValuePair;

                    writer.WritePropertyName("Value");
                    writer.WriteStartArray();

                    for (int i = 0; i < arr.m_Value.Count; i++)
                    {
                        writer.WriteValue(arr.m_Value[i]);
                    }

                    writer.WriteEndArray();
                    break;
                //case ValueType.Delegate:
                //    break;
                default:
                    break;
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);

            ValuePair valuePair;
            
            ValueType valueType = (ValueType)Enum.Parse(typeof(ValueType), jo["ValueType"].ToString());
            string name = jo["Name"].ToString();
            JToken valueToken = jo["Value"];

            switch (valueType)
            {
                case ValueType.Int32:
                    valuePair = ValuePair.Int(name, valueToken.ToObject<int>());
                    break;
                case ValueType.Double:
                    valuePair = ValuePair.Double(name, valueToken.ToObject<double>());
                    break;
                case ValueType.String:
                    valuePair = ValuePair.String(name, valueToken.ToString());
                    break;
                case ValueType.Boolean:
                    valuePair = ValuePair.Bool(name, valueToken.ToObject<bool>());
                    break;
                case ValueType.Array:
                    IList temp = valueToken as JArray;
                    if (temp != null && temp.Count > 0 && temp[0].GetType().GetElementType() == null)
                    {
                        if (int.TryParse(temp[0].ToString(), out int _))
                        {
                            List<int> tempList = new List<int>();
                            for (int i = 0; i < temp.Count; i++)
                            {
                                tempList.Add(int.Parse(temp[i].ToString()));
                            }
                            temp = tempList;
                        }
                        else if (double.TryParse(temp[0].ToString(), out double _))
                        {
                            List<double> tempList = new List<double>();
                            for (int i = 0; i < temp.Count; i++)
                            {
                                tempList.Add(double.Parse(temp[i].ToString()));
                            }
                            temp = tempList;
                        }
                        else if (bool.TryParse(temp[0].ToString(), out bool _))
                        {
                            List<bool> tempList = new List<bool>();
                            for (int i = 0; i < temp.Count; i++)
                            {
                                tempList.Add(bool.Parse(temp[i].ToString()));
                            }
                            temp = tempList;
                        }
                        else
                        {
                            List<string> tempList = new List<string>();
                            for (int i = 0; i < temp.Count; i++)
                            {
                                tempList.Add(temp[i].ToString());
                            }
                            temp = tempList;
                        }
                    }
                    valuePair = ValuePair.Array(name, temp);
                    break;
                //case ValueType.Delegate:
                //    valuePair = ValuePair.ac
                //    break;
                default:
                    valuePair = new ValueNull(name);
                    break;
            }

            return valuePair;
            //if (!jo.TryGetValue("m_Value", out JToken value))
            //{
            //    return JsonConvert.DeserializeObject<ValueNull>(jo.ToString(),
            //        BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            //}

            //if (value.Type == JTokenType.Boolean)
            //{
            //    return JsonConvert.DeserializeObject<SerializableBoolValuePair>(jo.ToString(),
            //        BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            //}
            //else if (value.Type == JTokenType.Float)
            //{
            //    return JsonConvert.DeserializeObject<SerializableDoubleValuePair>(jo.ToString(),
            //        BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            //}
            //else if (value.Type == JTokenType.Integer)
            //{
            //    return JsonConvert.DeserializeObject<SerializableIntValuePair>(jo.ToString(),
            //        BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            //}
            //else if (value.Type == JTokenType.String)
            //{
            //    return JsonConvert.DeserializeObject<SerializableStringValuePair>(jo.ToString(),
            //        BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            //}
            //else if (value.Type == JTokenType.Array)
            //{
            //    var temp = JsonConvert.DeserializeObject<SerializableArrayValuePair>(jo.ToString(),
            //        BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);

            //    if (temp.m_Value.Count > 0 && temp.m_Value[0].GetType().GetElementType() == null)
            //    {
            //        if (int.TryParse(temp.m_Value[0].ToString(), out int _))
            //        {
            //            List<int> tempList = new List<int>();
            //            for (int i = 0; i < temp.m_Value.Count; i++)
            //            {
            //                tempList.Add(int.Parse(temp.m_Value[i].ToString()));
            //            }
            //            temp.m_Value = tempList;
            //        }
            //        else if (double.TryParse(temp.m_Value[0].ToString(), out double _))
            //        {
            //            List<double> tempList = new List<double>();
            //            for (int i = 0; i < temp.m_Value.Count; i++)
            //            {
            //                tempList.Add(double.Parse(temp.m_Value[i].ToString()));
            //            }
            //            temp.m_Value = tempList;
            //        }
            //        else if (bool.TryParse(temp.m_Value[0].ToString(), out bool _))
            //        {
            //            List<bool> tempList = new List<bool>();
            //            for (int i = 0; i < temp.m_Value.Count; i++)
            //            {
            //                tempList.Add(bool.Parse(temp.m_Value[i].ToString()));
            //            }
            //            temp.m_Value = tempList;
            //        }
            //        else
            //        {
            //            List<string> tempList = new List<string>();
            //            for (int i = 0; i < temp.m_Value.Count; i++)
            //            {
            //                tempList.Add(temp.m_Value[i].ToString());
            //            }
            //            temp.m_Value = tempList;
            //        }
            //    }

            //    return temp;
            //}
            //else
            //{
            //    return JsonConvert.DeserializeObject<ValueNull>(jo.ToString(),
            //        BaseSpecifiedConcreteClassConverter.SpecifiedSubclassConversion);
            //}
        }
    }
}
