using Syadeu.Database;
using Syadeu.Mono;
using UnityEngine;

namespace Syadeu.Presentation
{
    [RequireComponent(typeof(RecycleableMonobehaviour))]
    public sealed class ProxyMonoComponent : MonoBehaviour
    {
        internal Hash m_GameObject;

        public T GetDataComponent<T>() where T : DataComponentEntity, new()
        {
            return PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjects[m_GameObject]
                .GetComponent<T>();
        }

        public void Destory()
        {
            PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjects[m_GameObject].Destory();
            GetComponent<RecycleableMonobehaviour>().Terminate();
        }
    }
}
