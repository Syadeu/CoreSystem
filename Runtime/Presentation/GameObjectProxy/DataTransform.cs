using Syadeu.Database;
using Syadeu.Mono;
using Syadeu.ThreadSafe;
using System;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Syadeu.Presentation
{
    public struct DataTransform : IInternalDataComponent, IReadOnlyTransform, IValidation, IEquatable<DataTransform>
    {
        const string c_WarningText = "This Data Transform has been destroyed or didn\'t created propery. Request ignored.";

        internal static int2 ProxyNull = new int2(-1, -1);
        internal static int2 ProxyQueued = new int2(-2, -2);

        internal Hash m_GameObject;
        internal Hash m_Idx;
        internal int2 m_ProxyIdx;
        internal int m_PrefabIdx;
        internal bool m_EnableCull;

        internal bool m_IsVisible;

        Hash IInternalDataComponent.GameObject => m_GameObject;
        Hash IInternalDataComponent.Idx => m_Idx;
        bool IInternalDataComponent.HasProxyObject => !m_ProxyIdx.Equals(ProxyNull) && !m_ProxyIdx.Equals(ProxyQueued);
        internal bool HasProxyObject => !m_ProxyIdx.Equals(ProxyNull) && !m_ProxyIdx.Equals(ProxyQueued);
        bool IInternalDataComponent.ProxyRequested => m_ProxyIdx.Equals(ProxyQueued);
        internal bool ProxyRequested => m_ProxyIdx.Equals(ProxyQueued);

        DataTransform IInternalDataComponent.transform => this;
        bool IEquatable<IInternalDataComponent>.Equals(IInternalDataComponent other) => m_Idx.Equals(other.Idx);
        bool IEquatable<DataTransform>.Equals(DataTransform other) => m_Idx.Equals(other.m_Idx);

        internal Vector3 m_Position;
        internal quaternion m_Rotation;
        internal Vector3 m_LocalScale;

        internal RecycleableMonobehaviour ProxyObject
        {
            get
            {
                if (!((IInternalDataComponent)this).HasProxyObject || ((IInternalDataComponent)this).ProxyRequested) return null;
                return PresentationSystem<GameObjectProxySystem>.System.m_Instances[m_ProxyIdx.x][m_ProxyIdx.y];
            }
        }
        unsafe private DataTransform* GetPointer() => PresentationSystem<GameObjectProxySystem>.System.GetDataTransformPointer(m_Idx);
        private ref DataTransform GetRef()
        {
            unsafe
            {
                return ref *GetPointer();
            }
        }
        private void RequestUpdate()
        {
            if (!PresentationSystem<RenderSystem>.System.IsInCameraScreen(m_Position)) return;
            PresentationSystem<GameObjectProxySystem>.System.RequestUpdateTransform(m_Idx);
        }

        public bool IsValid() =>
            !m_GameObject.Equals(Hash.Empty) && !m_Idx.Equals(Hash.Empty) &&
            !PresentationSystem<GameObjectProxySystem>.System.Disposed &&
            PresentationSystem<GameObjectProxySystem>.IsValid() &&
            PresentationSystem<GameObjectProxySystem>.System.m_MappedTransformIdxes.ContainsKey(m_Idx) &&
            PresentationSystem<GameObjectProxySystem>.System.m_MappedGameObjectIdxes.ContainsKey(m_GameObject) &&
            !PresentationSystem<GameObjectProxySystem>.System.GetDataGameObject(m_GameObject).m_Destroyed;
        public bool IsVisible()
        {
            if (!IsValid()) return false;
            return GetRef().m_IsVisible;
        }
        public void SynchronizeWithProxy()
        {
            if (!CoreSystem.IsThisMainthread()) throw new CoreSystemThreadSafeMethodException("SynchronizeWithProxy");
            if (!IsValid())
            {
                CoreSystem.Logger.LogError(Channel.Presentation,
                    "transform is not valid");
                return;
            }

            PresentationSystem<GameObjectProxySystem>.System.DownloadDataTransform(m_Idx);
        }
        public void SetCulling(bool enable)
        {
            GetRef().m_EnableCull = enable;
        }

#pragma warning disable IDE1006 // Naming Styles
#line hidden
        public DataGameObject gameObject
        {
            get
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return default;
                }
                return PresentationSystem<GameObjectProxySystem>.System.GetDataGameObject(m_GameObject);
            }
        }

        /// <summary>
        /// <see langword="true"/>일 경우, 화면 밖에 있을때 자동으로 프록시 오브젝트를 할당 해제합니다.
        /// </summary>
        public bool enableCull
        {
            get
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return false;
                }

                return GetRef().m_EnableCull;
            }
            set
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return;
                }

                ref DataTransform tr = ref GetRef();
                if (tr.m_EnableCull.Equals(value)) return;
                tr.m_EnableCull = value;
                RequestUpdate();
            }
        }

        public Vector3 position
        {
            get
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return Vector3.Zero;
                }
                return GetRef().m_Position;
            }
            set
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return;
                }

                ref DataTransform tr = ref GetRef();
                if (tr.m_Position.Equals(value)) return;
                tr.m_Position = value;
                RequestUpdate();
            }
        }
        public Vector3 eulerAngles
        {
            get
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return Vector3.Zero;
                }

                var temp = rotation.Euler();
                return temp.ToThreadSafe() * UnityEngine.Mathf.Rad2Deg;
            }
            set
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return;
                }

                Vector3 temp = new Vector3(value.x * UnityEngine.Mathf.Deg2Rad, value.y * UnityEngine.Mathf.Deg2Rad, value.z * UnityEngine.Mathf.Deg2Rad);
                rotation = quaternion.EulerZXY(temp);
            }
        }
        public quaternion rotation
        {
            get
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return quaternion.identity;
                }
                return GetRef().m_Rotation;
            }
            set
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return;
                }

                ref DataTransform tr = ref GetRef();
                if (tr.m_Rotation.Equals(value)) return;
                tr.m_Rotation = value;
                RequestUpdate();
            }
        }
        
        public Vector3 right => rotation * Vector3.Right;
        public Vector3 up => rotation * Vector3.Up;
        public Vector3 forward => rotation * Vector3.Forward;

        public Vector3 localScale
        {
            get
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return Vector3.Zero;
                }
                return GetRef().m_LocalScale;
            }
            set
            {
                if (!IsValid())
                {
                    CoreSystem.Logger.LogWarning(Channel.Presentation, c_WarningText);
                    return;
                }

                ref DataTransform tr = ref GetRef();
                if (tr.m_LocalScale.Equals(value)) return;
                tr.m_LocalScale = value;
                RequestUpdate();
            }
        }

        public float4x4 localToWorldMatrix => RenderSystem.LocalToWorldMatrix(position, rotation);
        public float4x4 worldToLocalMatrix => math.inverse(localToWorldMatrix);

        Vector3 IReadOnlyTransform.position => position;
        Vector3 IReadOnlyTransform.eulerAngles => eulerAngles;
        quaternion IReadOnlyTransform.rotation => rotation;

        Vector3 IReadOnlyTransform.right => right;
        Vector3 IReadOnlyTransform.up => up;
        Vector3 IReadOnlyTransform.forward => forward;

        Vector3 IReadOnlyTransform.localScale => localScale;
#line default
#pragma warning restore IDE1006 // Naming Styles
    }
}
