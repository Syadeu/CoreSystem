using System;

namespace Syadeu.Collections.Proxy
{
    public interface IProxyMonobehaviour
    {
        UnityEngine.GameObject gameObject { get; }
        UnityEngine.Transform transform { get; }

        bool InitializeOnCall { get; }

        bool Activated { get; }

        void Initialize();

        T GetOrAddComponent<T>() where T : UnityEngine.Component;
        T GetComponent<T>() where T : UnityEngine.Component;
        T[] GetComponents<T>() where T : UnityEngine.Component;
        UnityEngine.Component GetComponent(Type t);
        UnityEngine.Component[] GetComponents(Type t);

        T GetComponentUnity<T>() where T : UnityEngine.Component;

        T AddComponent<T>() where T : UnityEngine.Component;
    }
}
