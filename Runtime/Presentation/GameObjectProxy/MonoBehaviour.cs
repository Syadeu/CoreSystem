using UnityEngine;

namespace Syadeu.Presentation
{
    public abstract class MonoBehaviour<T> : MonoBehaviour
    {
        [SerializeReference] public T m_Value;
    }
}
