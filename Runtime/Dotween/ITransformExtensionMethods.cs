using DG.Tweening;
using DG.Tweening.Core;
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

            private int m_Index;
            public ITransform m_Transform;

            public ITransformDOTweenHelper()
            {
                m_Index = s_Index;
                s_Index += 1;
            }

            public float3 GetPosition() => m_Transform.position;
            public void SetPosition(float3 position) => m_Transform.position = position;

            public void Reserve()
            {
                int hash = m_Transform.GetHashCode();
                s_Tweens[hash].Remove(this);

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
        private static readonly Dictionary<int, List<ITransformDOTweenHelper>> s_Tweens = new Dictionary<int, List<ITransformDOTweenHelper>>();

        public static void DOMove(this ITransform tr, float3 position, float time)
        {
            ITransformDOTweenHelper temp = PoolContainer<ITransformDOTweenHelper>.Dequeue();
            temp.m_Transform = tr;

            TweenerCore<float3, float3, Float3Options> tween
                  = DOTween.To<float3, float3, Float3Options>(
                      DOTweenPlugins.s_Float3Plugin, temp.GetPosition, temp.SetPosition, position, time);

            int hash = tr.GetHashCode();
            if (!s_Tweens.TryGetValue(hash, out var list))
            {
                list = new List<ITransformDOTweenHelper>();
                s_Tweens.Add(hash, list);
            }
            list.Add(temp);

            tween.onKill += temp.Reserve;
        }
    }
}
