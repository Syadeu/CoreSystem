using Syadeu.Extentions.EditorUtils;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Syadeu
{
    public sealed class RenderManager : StaticManager<RenderManager>
    {
        public delegate bool RenderCondition();

        internal class ManagedObject
        {
            public Transform Transform = null;
            public List<Renderer> Renderers = null;
            public Vector3 Position = Vector3.zero;

            public RenderCondition RenderCondition = null;

            public bool IsStatic = false;
            public bool IsOnline = true;
        }

        private Camera TargetCamera { get; set; } = null;
        private Vector3 ScreenOffset { get; set; }

        private List<ManagedObject> ManagedList { get; } = new List<ManagedObject>();

        private List<int> WaitForCondition { get; } = new List<int>();
        private List<int> WaitForOffline { get; } = new List<int>();
        private List<int> WaitForOnline { get; } = new List<int>();

        /// <summary>
        /// 렌더링 규칙을 적용할 카메라를 설정합니다.
        /// </summary>
        /// <param name="cam"></param>
        /// <param name="offset"></param>
        public static void SetCamera(Camera cam, Vector3 offset = default)
        {
            Instance.TargetCamera = cam;
            Instance.ScreenOffset = offset;
        }
        /// <summary>
        /// 자동 렌더링 규칙을 가진 렌더러를 추가합니다.
        /// </summary>
        /// <param name="component"></param>
        /// <param name="isStatic"></param>
        public static void AddAutoRender(Component component, bool isStatic = false)
        {
            var temp = new ManagedObject
            {
                Transform = component.transform,
                Renderers = component.GetComponentsInChildren<Renderer>().ToList(),
                IsStatic = isStatic
            };

            if (isStatic)
            {
                temp.Position = temp.Transform.position;
            }

            Instance.ManagedList.Add(temp);
        }
        /// <summary>
        /// 고유 렌더링 규칙을 가진 렌더러를 추가합니다.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="renderers"></param>
        /// <param name="condition"></param>
        public static void AddCustomAutoRender(Vector3 center, List<Renderer> renderers, RenderCondition condition)
        {
            if (condition == null)
            {
                $"EXCEPTION :: Condition cannot be null".ToLog();
                return;
            }

            var temp = new ManagedObject
            {
                Position = center,
                Renderers = renderers,
                RenderCondition = condition,

                IsStatic = true
            };

            Instance.ManagedList.Add(temp);
        }

        /// <summary>
        /// 사용하지마세요
        /// </summary>
        public override void OnInitialize()
        {
            StartCoroutine(Updater());
        }

        private Matrix4x4 CamMatrix4x4;
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

                    if (!ManagedList[i].IsStatic && ManagedList[i].RenderCondition == null)
                    {
                        ManagedList[i].Position = ManagedList[i].Transform.position;
                    }

                    if (i % 1000 == 0) yield return null;
                }

                CoreSystem.AddBackgroundJob(jobWorkerIndex, CalculateRender, out var job);
                yield return new WaitForBackgroundJob(job);

                for (int i = 0; i < WaitForCondition.Count; i++)
                {
                    if (ManagedList[WaitForCondition[i]].RenderCondition.Invoke())
                    {
                        WaitForOnline.Add(i);
                    }
                    else
                    {
                        WaitForOffline.Add(i);
                    }

                    if (i % 1000 == 0) yield return null;
                }
                WaitForCondition.Clear();

                #region On Off Func
                for (int i = 0; i < WaitForOnline.Count; i++)
                {
                    for (int a = 0; a < ManagedList[WaitForOnline[i]].Renderers.Count; a++)
                    {
                        if (ManagedList[WaitForOnline[i]].Renderers[a] == null)
                        {
                            ManagedList[WaitForOnline[i]].Renderers.RemoveAt(a);
                            a--;
                            continue;
                        }

                        ManagedList[WaitForOnline[i]].Renderers[a].enabled = true;
                    }

                    ManagedList[WaitForOnline[i]].IsOnline = true;
                }
                WaitForOnline.Clear();

                for (int i = 0; i < WaitForOffline.Count; i++)
                {
                    for (int a = 0; a < ManagedList[WaitForOffline[i]].Renderers.Count; a++)
                    {
                        if (ManagedList[WaitForOffline[i]].Renderers[a] == null)
                        {
                            ManagedList[WaitForOffline[i]].Renderers.RemoveAt(a);
                            a--;
                            continue;
                        }

                        ManagedList[WaitForOffline[i]].Renderers[a].enabled = false;
                    }

                    ManagedList[WaitForOffline[i]].IsOnline = false;
                }
                WaitForOffline.Clear();
                #endregion

                yield return null;
            }
        }

        private void CalculateRender()
        {
            for (int i = 0; i < ManagedList.Count; i++)
            {
                if (IsInCameraScreen(ManagedList[i].Position))
                {
                    if (ManagedList[i].RenderCondition != null)
                    {
                        try
                        {
                            if (ManagedList[i].RenderCondition.Invoke())
                            {
                                AddToOnline(i);
                            }
                            else
                            {
                                AddToOffline(i);
                            }
                        }
                        catch (UnityException)
                        {
                            WaitForCondition.Add(i);
                        }
                    }
                    else
                    {
                        AddToOnline(i);
                    }
                }
                else
                {
                    if (ManagedList[i].RenderCondition != null)
                    {
                        try
                        {
                            if (ManagedList[i].RenderCondition.Invoke())
                            {
                                AddToOnline(i);
                            }
                            else
                            {
                                AddToOffline(i);
                            }
                        }
                        catch (UnityException)
                        {
                            WaitForCondition.Add(i);
                        }
                    }
                    else
                    {
                        AddToOffline(i);
                    }
                }
            }
        }
        private void AddToOnline(int i)
        {
            if (!ManagedList[i].IsOnline)
            {
                WaitForOnline.Add(i);
            }
        }
        private void AddToOffline(int i)
        {
            if (ManagedList[i].IsOnline)
            {
                WaitForOffline.Add(i);
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