using Syadeu.Database;
using Syadeu.Mono;
using UnityEngine;

namespace Syadeu.Presentation
{
    [RequireComponent(typeof(RecycleableMonobehaviour))]
    public sealed class ProxyMonoComponent : MonoBehaviour
    {
        internal Hash m_GameObject;

        public DataGameObject GetDataGameObject()
        {
            unsafe
            {
                return *PresentationSystem<GameObjectProxySystem>.System.GetDataGameObjectPointer(m_GameObject);
            }
        }
        public T GetDataComponent<T>() where T : DataComponentEntity, new()
        {
            return GetDataGameObject().GetComponent<T>();
        }

        public void Destory()
        {
            GetDataGameObject().Destory();
            GetComponent<RecycleableMonobehaviour>().Terminate();
        }
    }
}
