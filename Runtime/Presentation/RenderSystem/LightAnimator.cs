﻿using System.Collections;
using UnityEngine;

namespace Syadeu.Presentation.Render
{
    [RequireComponent(typeof(Light))]
    public sealed class LightAnimator : MonoBehaviour, Render.IPlayable
    {
        [SerializeField] private AnimationCurve m_LightCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float m_TimeMultiplier = .25f;

        private Light m_Light;
        private float m_StartTime, m_GraphLastTime;

        private Coroutine m_Coroutine;

        private void Awake()
        {
            m_Light = GetComponent<Light>();
            m_Light.intensity = m_LightCurve.Evaluate(0);
            m_Light.enabled = false;

            m_GraphLastTime = m_LightCurve.keys[m_LightCurve.length - 1].time;
        }
        public void Play()
        {
            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
            }

            m_StartTime = Time.time;
            m_Coroutine = StartCoroutine(Updater());
        }
        private IEnumerator Updater()
        {
            m_Light.enabled = true;

            float time = Time.time - m_StartTime;
            while (time >= m_TimeMultiplier)
            {
                time = Time.time - m_StartTime;
                m_Light.intensity = m_LightCurve.Evaluate(time / m_TimeMultiplier) * m_GraphLastTime;

                yield return null;
            }

            m_Coroutine = null;
            m_Light.enabled = false;
        }
    }
}