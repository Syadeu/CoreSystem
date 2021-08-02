using Syadeu.Database;
using System.Collections.Concurrent;

namespace Syadeu.Presentation
{
    public sealed class DataContainerSystem : PresentationSystemEntity<DataContainerSystem>
    {
        public override bool EnableBeforePresentation => false;
        public override bool EnableOnPresentation => false;
        public override bool EnableAfterPresentation => false;

        private readonly ConcurrentDictionary<Hash, object> m_DataContainer = new ConcurrentDictionary<Hash, object>();

        public static Hash ToDataHash(string value) => Hash.NewHash(value, Hash.Algorithm.FNV1a64);

        public void Enqueue(Hash key, object value) => m_DataContainer.TryAdd(key, value);
        public void Enqueue(string key, object value) => Enqueue(ToDataHash(key), value);

        public object Dequeue(Hash key)
        {
            m_DataContainer.TryRemove(key, out var value);
            return value;
        }
        public object Dequeue(string key) => Dequeue(ToDataHash(key));
        public T Dequeue<T>(Hash key) => (T)Dequeue(key);
        public T Dequeue<T>(string key) => (T)Dequeue(key);
    }
}
