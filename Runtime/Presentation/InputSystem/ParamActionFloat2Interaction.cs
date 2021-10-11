using Syadeu.Presentation.Actions;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Syadeu.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

namespace Syadeu.Presentation.Input
{
    public sealed class ParamActionFloat2Interaction : IInputInteraction, Collections.IStaticInitializer
    {
        static ParamActionFloat2Interaction()
        {
            global::UnityEngine.InputSystem.InputSystem.RegisterInteraction<ParamActionFloat2Interaction>();
        }

        public long Action = 0;
        private bool m_Pressed;

        public void Process(ref InputInteractionContext context)
        {
            float2 value;
            Reference<ParamAction<float2>> reference;

            if (!m_Pressed)
            {
                //var isActuated = context.ControlIsActuated(0.75f);
                //if (isActuated)
                {
                    value = context.ReadValue<Vector2>();
                    reference = new Reference<ParamAction<float2>>(new Hash((ulong)Action));
                    reference.Execute(value);

                    "1".ToLog();
                    context.Started();
                }

                m_Pressed = true;
                return;
            }
            // Check if the control is currently actuated past our 3/4 threshold.
            var isStillActuated = context.ControlIsActuated(0.75f);

            if (isStillActuated)
            {
                value = context.ReadValue<Vector2>();
                reference = new Reference<ParamAction<float2>>(new Hash((ulong)Action));
                reference.Execute(value);

                "2".ToLog();
                context.Performed();

                return;
            }

            // See for how long the control has been held.
            var actuationTime = context.time - context.startTime;

            value = context.ReadValue<Vector2>();
            reference = new Reference<ParamAction<float2>>(new Hash((ulong)Action));
            reference.Execute(0);

            // Control is no longer actuated above 3/4 threshold. If it was held
            // for at least a second, perform the action. Otherwise cancel it.

            if (actuationTime >= 1)
            {
                "3".ToLog();
                context.Performed();
            }
            else
            {
                "4".ToLog();
                context.Canceled();
            }
        }
        public void Reset()
        {
            m_Pressed = false;
        }
    }
}
