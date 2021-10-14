﻿#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using System;

namespace Syadeu.Presentation.Actions
{
    public struct FixedLogicTriggerAction8
    {
        private FixedLogicTriggerAction
            a, b, c, d,
            e, f, g, h;

        private int m_Length;

        public int Length => m_Length;
        public FixedLogicTriggerAction this[int index]
        {
            get
            {
                if (index >= m_Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                switch (index)
                {
                    case 0: return a;
                    case 1: return b;
                    case 2: return c;
                    case 3: return d;
                    case 4: return e;
                    case 5: return f;
                    case 6: return g;
                    case 7: return h;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            set
            {
                if (index >= m_Length)
                {
                    throw new ArgumentOutOfRangeException();
                }

                switch (index)
                {
                    case 0: { a = value; return; }
                    case 1: { b = value; return; }
                    case 2: { c = value; return; }
                    case 3: { d = value; return; }
                    case 4: { e = value; return; }
                    case 5: { f = value; return; }
                    case 6: { g = value; return; }
                    case 7: { h = value; return; }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public FixedLogicTriggerAction8(LogicTriggerAction[] logicTriggers)
        {
            if (logicTriggers.Length > 7)
            {
                throw new ArgumentOutOfRangeException();
            }

            this = default(FixedLogicTriggerAction8);

            m_Length = logicTriggers.Length;
            for (int i = 0; i < logicTriggers.Length; i++)
            {
                this[i] = logicTriggers[i].GetFixedLogicTriggerAction();
            }
        }
    }
}
