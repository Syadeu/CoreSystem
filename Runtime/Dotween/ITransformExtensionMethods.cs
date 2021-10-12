using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Core;
using Syadeu.Collections;
using Syadeu.Collections.Proxy;
using System.Collections.Generic;
using Unity.Mathematics;

namespace Syadeu.Presentation.Dotween
{
    public static class ITransformExtensionMethods
    {
        private sealed class ITransformDOTweenHelper
        {
            private static int s_Index = 0;

            private readonly int m_Index;
            private ITransform m_Transform;
            private Tweener m_Tweener;

            public ITransformDOTweenHelper()
            {
                m_Index = s_Index;
                s_Index += 1;
            }
            public TweenerCore<float3, float3, Float3Options> DOPosition(ITransform tr, float3 endValue, float duration)
            {
                int hash = tr.GetHashCode();

                m_Transform = tr;
                
                TweenerCore<float3, float3, Float3Options> temp = DOTween.To<float3, float3, Float3Options>(
                      DOTweenPlugins.s_Float3Plugin, GetPosition, SetPosition, endValue, duration);
                temp.SetId(hash);

                m_Tweener = temp;

                //if (!s_Tweens.TryGetValue(hash, out var list))
                //{
                //    list = new List<Tweener>();
                //    s_Tweens.Add(hash, list);
                //}
                //list.Add(m_Tweener);

                m_Tweener.onKill += Reserve;
                return temp;
            }

            public float3 GetPosition() => m_Transform.position;
            public void SetPosition(float3 position) => m_Transform.position = position;

            public void Reserve()
            {
                //int hash = m_Transform.GetHashCode();
                //s_Tweens[hash].Remove(m_Tweener);

                m_Transform = null;
                m_Tweener = null;

                PoolContainer<ITransformDOTweenHelper>.Enqueue(this);
            }

            public override int GetHashCode() => m_Index;
        }
        static ITransformExtensionMethods()
        {
            PoolContainer<ITransformDOTweenHelper>.Initialize(Factory, 16);
        }
        private static ITransformDOTweenHelper Factory()
        {
            return new ITransformDOTweenHelper();
        }
        //private static readonly Dictionary<int, List<Tweener>> s_Tweens = new Dictionary<int, List<Tweener>>();

        private static void Sequence(this ITransform tr)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.SetId(tr.GetHashCode());
        }

        public static TweenerCore<float3, float3, Float3Options> DOMove(this ITransform tr, float3 position, float time)
        {
            ITransformDOTweenHelper temp = PoolContainer<ITransformDOTweenHelper>.Dequeue();
            TweenerCore<float3, float3, Float3Options> tween = temp.DOPosition(tr, position, time);

            return tween;
        }
        public static void DOKill(this ITransform tr, bool complete = false)
        {
            DOTween.Kill(tr.GetHashCode(), complete);
        }
    }
}
