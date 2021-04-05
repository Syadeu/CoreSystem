using System.Collections.Generic;

using UnityEngine;

namespace Syadeu.FMOD
{
#if CORESYSTEM_FMOD
    using FMODUnity;
    public class FMODListener : MonoBehaviour
    {
        private static List<FMODListener> Listeners { get; } = new List<FMODListener>();
        private static int numListeners = 0;

        public int Index;
        public GameObject attenuationObject;

        private Transform m_Transform;
        private Rigidbody m_Rigidbody;
        private Rigidbody2D m_Rigidbody2D;

        public Vector3 Position { get; private set; }

        private static int AddListener(FMODListener listener)
        {
            for (int i = 0; i < Listeners.Count; i++)
            {
                if (Listeners[i] != null && listener.gameObject == Listeners[i].gameObject)
                {
                    Debug.LogWarning(string.Format(("[FMOD] Listener has already been added at index {0}."), i));
                    return i;
                }
            }
            // If already at the max numListeners
            if (numListeners >= global::FMOD.CONSTANTS.MAX_LISTENERS)
            {
                Debug.LogWarning(string.Format(("[FMOD] Max number of listeners reached : {0}."), global::FMOD.CONSTANTS.MAX_LISTENERS));
                //return -1;
            }

            // If not already in the list
            // The next available spot in the list should be at `numListeners`
            if (Listeners.Count <= numListeners)
            {
                Listeners.Add(listener);
            }
            else
            {
                Listeners[numListeners] = listener;
            }
            // Increment `numListeners`
            numListeners++;
            // setNumListeners (8 is the most that FMOD supports)
            int numListenersClamped = Mathf.Min(numListeners, global::FMOD.CONSTANTS.MAX_LISTENERS);
            FMODSystem.Instance.FMODStudioSystem.setNumListeners(numListenersClamped);

            CoreSystem.OnUnityUpdate += listener.OnUnityUpdate;

            return numListeners - 1;
        }
        private static bool RemoveListener(FMODListener listener)
        {
            int index = listener.Index;
            // Remove listener
            if (index != -1)
            {
                Listeners[index] = null;

                // Are there more listeners above the index of the one we are removing?
                if (numListeners - 1 > index)
                {
                    // Move any higher index listeners down
                    for (int i = index; i < Listeners.Count; i++)
                    {
                        if (i == Listeners.Count - 1)
                        {
                            Listeners[i] = null;
                        }
                        else
                        {
                            Listeners[i] = Listeners[i + 1];
                            if (Listeners[i])
                            {
                                Listeners[i].Index = i;
                            }
                        }
                    }
                }
                // Decriment numListeners
                numListeners--;
                // Always need at least 1 listener, otherwise "[FMOD] assert : assertion: 'numListeners >= 1 && numListeners <= 8' failed"
                int numListenersClamped = Mathf.Min(Mathf.Max(numListeners, 1), global::FMOD.CONSTANTS.MAX_LISTENERS);
                FMODSystem.Instance.FMODStudioSystem.setNumListeners(numListenersClamped);
                // Listener attributes will be updated before the next update, due to the Script Execution Order.

                CoreSystem.OnUnityUpdate -= listener.OnUnityUpdate;

                return true;
            }
            else
            {
                return false;
            }
        }

        private void OnEnable()
        {
            RuntimeUtils.EnforceLibraryOrder();
            m_Transform = transform;
            m_Rigidbody = gameObject.GetComponent<Rigidbody>();
            m_Rigidbody2D = gameObject.GetComponent<Rigidbody2D>();

            Index = AddListener(this);
        }
        private void OnDisable()
        {
            RemoveListener(this);
        }

        private void OnUnityUpdate()
        {
            if (Index >= 0 && Index < global::FMOD.CONSTANTS.MAX_LISTENERS)
            {
                if (m_Rigidbody)
                {
                    SetListenerLocation3D(Index, transform, m_Rigidbody, attenuationObject);
                }
                else
                {
                    SetListenerLocation2D(Index, transform, m_Rigidbody2D, attenuationObject);
                }

                Position = m_Transform.position;
            }
        }

        private static void SetListenerLocation3D(int listenerIndex, Transform transform, Rigidbody rigidBody = null, GameObject attenuationObject = null)
        {
            if (attenuationObject)
            {
                FMODSystem.Instance.FMODStudioSystem.setListenerAttributes(0, RuntimeUtils.To3DAttributes(transform, rigidBody), RuntimeUtils.ToFMODVector(attenuationObject.transform.position));
            }
            else
            {
                FMODSystem.Instance.FMODStudioSystem.setListenerAttributes(0, RuntimeUtils.To3DAttributes(transform, rigidBody));
            }
        }
        private static void SetListenerLocation2D(int listenerIndex, Transform transform, Rigidbody2D rigidBody = null, GameObject attenuationObject = null)
        {
            if (attenuationObject)
            {
                FMODSystem.Instance.FMODStudioSystem.setListenerAttributes(0, RuntimeUtils.To3DAttributes(transform, rigidBody), RuntimeUtils.ToFMODVector(attenuationObject.transform.position));
            }
            else
            {
                FMODSystem.Instance.FMODStudioSystem.setListenerAttributes(0, RuntimeUtils.To3DAttributes(transform, rigidBody));
            }
        }
    }
#endif
}
