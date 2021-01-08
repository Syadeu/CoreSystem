using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using Syadeu.Extentions.EditorUtils;

using UnityEngine;

namespace Syadeu.Mono
{
    public sealed class RenderManager : StaticManager<RenderManager>
    {
        public delegate bool RenderCondition();

        public override string DisplayName => "Render Manager";
        public override bool DontDestroy => false;
        public override bool HideInHierarchy => false;

        public List<ManagedObject> ManagedObjects = new List<ManagedObject>();
        private Vector3 ScreenOffset { get; set; }

        public Camera MainCamera;
        private Matrix4x4 CamMatrix4x4;

        [Serializable]
        public class ManagedObject
        {
            public RenderController Controller { get; }
            public RenderCondition WhileVisible => Controller.WhileVisible;

            public ManagedObject(RenderController controller)
            {
                Controller = controller;
            }
        }

        private void Awake()
        {
            if (MainCamera == null) MainCamera = Camera.main;

            StartUnityUpdate(UnityUpdate());
        }

        private IEnumerator UnityUpdate()
        {
            CamMatrix4x4 = MainCamera.projectionMatrix * MainCamera.transform.worldToLocalMatrix;

            while (m_Instance != null)
            {
                if (m_WaitForManaged.Count > 0)
                {
                    int count = m_WaitForManaged.Count;
                    for (int i = 0; i < count; i++)
                    {
                        if (m_WaitForManaged.TryDequeue(out var controller))
                        {
                            ManagedObjects.Add(new ManagedObject(controller));
                        }
                    }
                }

                for (int i = 0; i < ManagedObjects.Count; i++)
                {
                    if (ManagedObjects[i].Controller.IsInvisible)
                    {
                        ManagedObjects.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (ManagedObjects[i].WhileVisible != null)
                    {
                        if (ManagedObjects[i].WhileVisible.Invoke())
                        {
                            if (ManagedObjects[i].Controller.IsForcedOff)
                            {
                                ManagedObjects[i].Controller.RenderOn();
                            }
                        }
                        else
                        {
                            if (!ManagedObjects[i].Controller.IsForcedOff)
                            {
                                ManagedObjects[i].Controller.RenderOff();
                            }
                        }
                    }

                    if (i != 0 && i % 500 == 0) yield return null;
                }

                yield return null;
            }
        }

        private readonly ConcurrentQueue<RenderController> m_WaitForManaged = new ConcurrentQueue<RenderController>();
        internal void AddRenderControl(RenderController controller)
        {
            m_WaitForManaged.Enqueue(controller);
        }

        /// <summary>
        /// 렌더링 규칙을 적용할 카메라를 설정합니다.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="offset"></param>
        public static void SetCamera(Camera cam, Vector3 offset = default)
        {
            Instance.MainCamera = cam;
            Instance.ScreenOffset = offset;
        }
        //// 이거 젤터 전용
        ////Vector3 screenOffset = new Vector3(1, 1, 5);
        internal bool IsInCameraScreen(Vector3 target)
        {
            Vector4 p4 = target;
            p4.w = 1;
            Vector4 result4 = CamMatrix4x4 * p4;
            Vector3 screenPoint = result4;
            screenPoint /= -result4.w;
            screenPoint.x = screenPoint.x / 2 + 0.5f;
            screenPoint.y = screenPoint.y / 2 + 0.5f;
            screenPoint.z = -result4.w;

            return screenPoint.z > 0 - ScreenOffset.z &&
                screenPoint.x > 0 - ScreenOffset.x &&
                screenPoint.x < 1 + ScreenOffset.x &&
                screenPoint.y > 0 - ScreenOffset.y &&
                screenPoint.y < 1 + ScreenOffset.y;
        }
    }
}