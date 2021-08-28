using Syadeu.Presentation.Actions;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

namespace Syadeu.Presentation.Input
{
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public sealed class ParamActionFloat2Interaction : IInputInteraction
    {
#if UNITY_EDITOR
        static ParamActionFloat2Interaction()
        {
            Initialize();
        }
#endif

        [RuntimeInitializeOnLoadMethod]
        static void Initialize()
        {
            global::UnityEngine.InputSystem.InputSystem.RegisterInteraction<ParamActionFloat2Interaction>();
        }

        internal Reference<ParamAction<float2>>[] Actions = Array.Empty<Reference<ParamAction<float2>>>();

        public void Process(ref InputInteractionContext context)
        {
            //if (context.timerHasExpired)
            //{
            //    context.Canceled();
            //    return;
            //}
            //context.action.
            switch (context.phase)
            {
                case InputActionPhase.Disabled:
                    break;
                case InputActionPhase.Waiting:
                    break;
                case InputActionPhase.Started:
                    break;
                case InputActionPhase.Performed:
                    float2 value = context.ReadValue<Vector2>();

                    Actions.Execute(value);
                    break;
                case InputActionPhase.Canceled:
                    break;
                default:
                    break;
            }
        }
        public void Reset()
        {
        }
    }
}
