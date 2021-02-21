using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Syadeu.Mono
{
    public class SDBasicHealth : SDComponent
    {
        private float m_HP;
        private bool m_Dead = false;

        public float HP => m_HP;
        public bool Dead => m_Dead;

        public override void Initialize(SDType type)
        {
            m_HP = type.m_MaxHP;
        }

        public float TakeDamage(float dmg)
        {
            if (m_Dead) return 0;

            m_HP -= dmg;
            if (m_HP <= 0)
            {
                m_Dead = true;
            }

            return m_HP;
        }
        public float CalculateDamage(float dmg, float reducePercent)
            => (dmg * .01f) * reducePercent;
        public float TakeDamage(float dmg, float reducePercent)
        {
            if (m_Dead) return 0;

            m_HP -= (dmg * .01f) * reducePercent;
            if (m_HP <= 0)
            {
                m_Dead = true;
            }

            return m_HP;
        }
    }

    public abstract class SDComponent : DataComponent
    {
        public abstract void Initialize(SDType type);
    }
}
