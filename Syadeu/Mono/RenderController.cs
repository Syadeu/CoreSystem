﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Collections;
using UnityEngine.Events;

namespace Syadeu.Mono
{
    public class RenderController : MonoBehaviour
    {
        public Camera Camera;
        public bool IsStandalone = false;

        [Space]
        public List<MonoBehaviour> AdditionalRenders = new List<MonoBehaviour>();

        [Space]
        [SerializeField] private UnityEvent OnVisible;
        [SerializeField] private UnityEvent OnInvisible;

        internal Renderer[] Renderers { get; private set; }
        internal bool IsInvisible { get; set; } = true;
        public bool IsForcedOff { get; internal set; } = false;

        internal Vector3 Position { get; private set; }
        internal bool Destroyed { get; private set; } = false;
        internal bool Listed { get; private set; } = false;

        private void Awake()
        {
            Renderers = transform.GetComponentsInChildren<Renderer>();

            if (Camera == null && !IsStandalone)
            {
                if (RenderManager.Instance.MainCamera == null)
                {
                    RenderManager.Instance.MainCamera = Camera.main;
                }

                Camera = RenderManager.Instance.MainCamera;
            }

            CoreSystem.Instance.StartBackgroundUpdate(BackgroundUpdate());
        }

        private void Update()
        {
            Position = transform.position;
        }
        private void OnDestroy()
        {
            Destroyed = true;
        }

        private IEnumerator BackgroundUpdate()
        {
            while (!Destroyed)
            {
                if (RenderManager.Instance.IsInCameraScreen(Position))
                {
                    IsInvisible = false;

                    if (!Listed)
                    {
                        if (!IsStandalone) RenderManager.Instance.AddRenderControl(this);

                        CoreSystem.AddForegroundJob(InvokeOnVisible);
                        Listed = true;
                    }
                }
                else
                {
                    IsInvisible = true;
                    
                    if (Listed)
                    {
                        CoreSystem.AddForegroundJob(InvokeOnInvisible);
                        Listed = false;
                    }
                }

                yield return null;
            }
        }

        private void InvokeOnVisible() => OnVisible?.Invoke();
        private void InvokeOnInvisible() => OnInvisible?.Invoke();

        public void RenderOff()
        {
            IsForcedOff = true;
            foreach (var item in Renderers)
            {
                item.enabled = false;
            }
            for (int i = 0; i < AdditionalRenders.Count; i++)
            {
                AdditionalRenders[i].enabled = false;
            }
        }
        public void RenderOn()
        {
            IsForcedOff = false;
            foreach (var item in Renderers)
            {
                item.enabled = true;
            }
            for (int i = 0; i < AdditionalRenders.Count; i++)
            {
                AdditionalRenders[i].enabled = true;
            }
        }
    }
}
