using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu
{
    public sealed class RenderManager : StaticManager<RenderManager>
    {
        internal class ManagedObject
        {
            public Transform Transform;
            public Renderer[] Renderers;
            public Vector3 Position;

            public bool IsOnline = true;
        }

        public Camera TargetCamera { get; set; }
        public Vector3 ScreenOffset { get; set; } = Vector3.zero;

        private List<ManagedObject> ManagedList { get; } = new List<ManagedObject>();

        private List<int> WaitForOffline { get; } = new List<int>();
        private List<int> WaitForOnline { get; } = new List<int>();

        public static void AddAutoRender(Component component)
        {
            Instance.ManagedList.Add(new ManagedObject
            {
                Transform = component.transform,
                Renderers = component.GetComponentsInChildren<Renderer>()
            });
        }

        public override void OnInitialize()
        {
            StartCoroutine(Updater());
        }

        Matrix4x4 CamMatrix4x4;

        private IEnumerator Updater()
        {
            WaitForSeconds delay = new WaitForSeconds(2);
            int jobWorkerIndex = CoreSystem.CreateNewBackgroundJobWorker(true);

            while (true)
            {
                if (TargetCamera == null)
                {
                    yield return null;
                    continue;
                }

                CamMatrix4x4 = TargetCamera.projectionMatrix * TargetCamera.transform.worldToLocalMatrix;

                for (int i = 0; i < ManagedList.Count; i++)
                {
                    if (ManagedList[i].Transform == null)
                    {
                        ManagedList.RemoveAt(i);
                        i--;
                        continue;
                    }

                    ManagedList[i].Position = ManagedList[i].Transform.position;

                    if (i % 1000 == 0) yield return null;
                }

                CoreSystem.AddBackgroundJob(jobWorkerIndex, CalculateRender, out var job);
                yield return new WaitForBackgroundJob(job);

                for (int i = 0; i < WaitForOnline.Count; i++)
                {
                    if (ManagedList[WaitForOnline[i]].Transform == null) continue;

                    foreach (var item in ManagedList[WaitForOnline[i]].Renderers)
                    {
                        item.enabled = true;
                    }

                    ManagedList[WaitForOnline[i]].IsOnline = true;
                }
                WaitForOnline.Clear();

                for (int i = 0; i < WaitForOffline.Count; i++)
                {
                    if (ManagedList[WaitForOffline[i]].Transform == null) continue;

                    foreach (var item in ManagedList[WaitForOffline[i]].Renderers)
                    {
                        item.enabled = false;
                    }

                    ManagedList[WaitForOffline[i]].IsOnline = false;
                }
                WaitForOffline.Clear();

                yield return null;
            }
        }

        private void CalculateRender()
        {
            for (int i = 0; i < ManagedList.Count; i++)
            {
                if (IsInCameraScreen(ManagedList[i].Position))
                {
                    if (!ManagedList[i].IsOnline)
                    {
                        WaitForOnline.Add(i);
                    }
                }
                else
                {
                    if (ManagedList[i].IsOnline)
                    {
                        WaitForOffline.Add(i);
                    }
                }
            }
        }

        // 이거 젤터 전용
        //Vector3 screenOffset = new Vector3(1, 1, 5);
        private bool IsInCameraScreen(Vector3 target)
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