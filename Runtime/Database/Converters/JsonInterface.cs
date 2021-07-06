using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Syadeu.Database.Converters
{
    public static class JsonInterface
    {
        public static ValuePairContainer Read(TextReader rdr)
        {
            JObject jo = JObject.Parse(rdr.ToString());
            ValuePairContainer output;

            using (var iter = jo.GetEnumerator())
            {
                List<ValuePair> values = new List<ValuePair>();
                while (iter.MoveNext())
                {
                    values.Add(ToValuePair(iter.Current.Key, iter.Current.Value));
                }
                output = new ValuePairContainer(values.ToArray());
            }

            return output;
        }
        private static ValuePair ToValuePair(string key, JToken jToken)
        {
            ValuePair output;
            switch (jToken.Type)
            {
                default:
                case JTokenType.None:
                case JTokenType.Bytes:
                case JTokenType.Constructor:
                case JTokenType.Property:
                    throw new System.Exception($"not supported json type of {jToken.Type}");
                case JTokenType.Object:
                    JObject jo = (JObject)jToken;
                    using (var iter = jo.GetEnumerator())
                    {
                        List<ValuePair> valuePairs = new List<ValuePair>();
                        while (iter.MoveNext())
                        {
                            valuePairs.Add(ToValuePair(iter.Current.Key, iter.Current.Value));
                        }
                        output = ValuePair.Object(key, valuePairs.ToArray());
                    }

                    break;
                case JTokenType.Array:
                    JArray arr = jToken as JArray;
                    if (arr.Count == 0)
                    {
                        output = ValuePair.Array(key, null);
                    }
                    else
                    {
                        IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(ToValue(arr[0]).GetType()));
                        for (int i = 0; i < arr.Count; i++)
                        {
                            list.Add(ToValue(arr[i]));
                        }
                        output = ValuePair.Array(key, list);
                    }

                    break;
                case JTokenType.Integer:
                    output = ValuePair.Int(key, jToken.ToObject<int>());
                    break;
                case JTokenType.Float:
                    output = ValuePair.Double(key, jToken.ToObject<double>());
                    break;
                case JTokenType.Boolean:
                    output = ValuePair.Bool(key, jToken.ToObject<bool>());
                    break;
                case JTokenType.Null:
                    output = new ValueNull(key);
                    break;
                case JTokenType.String:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                case JTokenType.Comment:
                    output = ValuePair.String(key, jToken.ToString());
                    break;
            }
            return output;
        }
        private static object ToValue(JToken jToken)
        {
            object output;
            switch (jToken.Type)
            {
                default:
                case JTokenType.None:
                case JTokenType.Bytes:
                case JTokenType.Constructor:
                case JTokenType.Property:
                case JTokenType.Object:
                case JTokenType.Array:
                    throw new System.Exception($"not supported json type of {jToken.Type}");
                case JTokenType.Integer:
                    output = jToken.ToObject<int>();
                    break;
                case JTokenType.Float:
                    output = jToken.ToObject<double>();
                    break;
                case JTokenType.Boolean:
                    output = jToken.ToObject<bool>();
                    break;
                case JTokenType.Null:
                    output = null;
                    break;
                case JTokenType.String:
                case JTokenType.Undefined:
                case JTokenType.Date:
                case JTokenType.Raw:
                case JTokenType.Guid:
                case JTokenType.Uri:
                case JTokenType.TimeSpan:
                case JTokenType.Comment:
                    output = jToken.ToString();
                    break;
            }
            return output;
        }
    }
}
