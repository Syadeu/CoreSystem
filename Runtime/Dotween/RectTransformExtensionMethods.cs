using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Syadeu.Database;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Syadeu.Presentation.Dotween
{
    public static class RectTransformExtensionMethods
    {
        private sealed class DOTweenHelper
        {
            public RectTransform m_RectTransform;

            public Vector2 GetSizeDelta()
            {
                return m_RectTransform.sizeDelta;
            }
            public void SetRectWidth(Vector2 sizeDelta)
            {
                m_RectTransform.sizeDelta = sizeDelta;
            }

            public void Reserve()
            {
                PoolContainer<DOTweenHelper>.Enqueue(this);
            }
        }
        static RectTransformExtensionMethods()
        {
            PoolContainer<DOTweenHelper>.Initialize(Factory, 16);
        }
        private static DOTweenHelper Factory()
        {
            return new DOTweenHelper();
        }

        //public static void DOSizeDelta(this RectTransform other, Vector2 target, float time)
        //{
        //    DOTweenHelper temp = PoolContainer<DOTweenHelper>.Dequeue();
        //    temp.m_RectTransform = other;

        //    TweenerCore<Vector2, Vector2, VectorOptions> tween
        //        = DOTween.To(temp.GetSizeDelta, temp.SetRectWidth, target, time);

        //    tween.onKill += temp.Reserve;
        //}
    }
}
