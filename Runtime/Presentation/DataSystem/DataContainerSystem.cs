using Syadeu.Collections;
using Syadeu.Internal;
using System.Collections.Concurrent;

namespace Syadeu.Presentation.Data
{
    /// <summary>
    /// 시스템 동기화를 위해 임시로 데이터를 저장할 수 있는 시스템입니다.
    /// </summary>
    public sealed class DataContainerSystem : PresentationSystemEntity<DataContainerSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly ConcurrentDictionary<Hash, object> m_DataContainer = new ConcurrentDictionary<Hash, object>();

        public static Hash ToDataHash(string value) => Hash.NewHash(value, Hash.Algorithm.FNV1a64);

        public bool HasValue(Hash key) => m_DataContainer.ContainsKey(key);
        public bool HasValue(string key) => HasValue(ToDataHash(key));

        public void Enqueue(Hash key, object value) => m_DataContainer.TryAdd(key, value);
        public void Enqueue(string key, object value) => Enqueue(ToDataHash(key), value);

        public object Dequeue(Hash key)
        {
            m_DataContainer.TryRemove(key, out var value);
            if (value == null)
            {
                throw new System.Exception("key not found");
            }
            return value;
        }
        public object Dequeue(string key) => Dequeue(ToDataHash(key));
        public T Dequeue<T>(Hash key)
        {
            object value = Dequeue(key);
            if (!value.GetType().Equals(TypeHelper.TypeOf<T>.Type))
            {
                throw new System.Exception($"Type mismatch. Value is {value.GetType().Name} but requested as {TypeHelper.TypeOf<T>.Name}");
            }
            return (T)value;
        }
        public T Dequeue<T>(string key)
        {
            object value = Dequeue(key);
            if (!value.GetType().Equals(TypeHelper.TypeOf<T>.Type))
            {
                throw new System.Exception($"Type mismatch. Value is {TypeHelper.ToString(value.GetType())} but requested as {TypeHelper.TypeOf<T>.ToString()}");
            }
            return (T)value;
        }
    }
}
