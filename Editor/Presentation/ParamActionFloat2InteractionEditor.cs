using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Input;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace SyadeuEditor.Presentation
{
    public class ParamActionFloat2InteractionEditor : InputParameterEditor<ParamActionFloat2Interaction>
    {
        private sealed class Reflector
        {
            public Reference<ParamAction<float2>> Action;
        }

        Reflector m_Reflector;
        ObjectDrawer Drawer;

        protected override void OnEnable()
        {
            m_Reflector = new Reflector
            {
                Action = new Reference<ParamAction<float2>>(new Hash((ulong)target.Action))
            };

            Drawer = new ObjectDrawer(
                m_Reflector,
                TypeHelper.TypeOf<Reflector>.Type,
                string.Empty);

            if (!EntityWindow.IsDataLoaded)
            {
                EntityWindow.Instance.LoadData();
            }

            base.OnEnable();
        }
        public override void OnGUI()
        {
            if (GUILayout.Button("Save"))
            {
                ulong temp = m_Reflector.Action.m_Hash;
                target.Action = (long)temp;
            }

            EditorGUI.BeginChangeCheck();
            Drawer.OnGUI();
            if (EditorGUI.EndChangeCheck())
            {
                ulong temp = m_Reflector.Action.m_Hash;
                target.Action = (long)temp;
            }
        }
    }
}
