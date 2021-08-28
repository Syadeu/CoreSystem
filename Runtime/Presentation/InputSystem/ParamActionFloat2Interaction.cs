using Syadeu.Presentation.Actions;
using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Syadeu.Database;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine.InputSystem.Editor;
#endif

namespace Syadeu.Presentation.Input
{
    public sealed class ParamActionFloat2Interaction : IInputInteraction, Database.IStaticInitializer
    {
        static ParamActionFloat2Interaction()
        {
            global::UnityEngine.InputSystem.InputSystem.RegisterInteraction<ParamActionFloat2Interaction>();
        }

        //public Reference<ParamAction<float2>>[] Actions = Array.Empty<Reference<ParamAction<float2>>>();
        public long Action = 0;

        public void Process(ref InputInteractionContext context)
        {
            //if (context.timerHasExpired)
            //{
            //    context.Canceled();
            //    return;
            //}
            //context.action.
            //switch (context.phase)
            //{
            //    case InputActionPhase.Disabled:
            //        break;
            //    case InputActionPhase.Waiting:
            //        break;
            //    case InputActionPhase.Started:
            //        break;
            //    case InputActionPhase.Performed:
            //        float2 value = context.ReadValue<Vector2>();

            //        Actions.Execute(value);
            //        break;
            //    case InputActionPhase.Canceled:
            //        break;
            //    default:
            //        break;
            //}

            float2 value = context.ReadValue<Vector2>();

            Reference<ParamAction<float2>> reference = new Reference<ParamAction<float2>>(new Hash((ulong)Action));
            reference.Execute(value);
        }
        public void Reset()
        {
        }
    }
}
