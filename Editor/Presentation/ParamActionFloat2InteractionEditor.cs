using Syadeu.Collections;
using Syadeu.Internal;
using Syadeu.Presentation;
using Syadeu.Presentation.Actions;
using Syadeu.Presentation.Input;
using SyadeuEditor.Utilities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace SyadeuEditor.Presentation
{
    //public class ParamActionFloat2InteractionEditor : InputParameterEditor<ParamActionFloat2Interaction>
    //{
    //    private sealed class Reflector
    //    {
    //        public Reference<ParamAction<float2>> Action;
    //    }

    //    Reflector m_Reflector;
    //    ObjectDrawer Drawer;

    //    protected override void OnEnable()
    //    {
    //        m_Reflector = new Reflector
    //        {
    //            Action = new Reference<ParamAction<float2>>(new Hash((ulong)target.Action))
    //        };

    //        Drawer = new ObjectDrawer(
    //            m_Reflector,
    //            TypeHelper.TypeOf<Reflector>.Type,
    //            string.Empty);

    //        if (!EntityDataList.IsLoaded)
    //        {
    //            EntityDataList.Instance.LoadData();
    //        }

    //        base.OnEnable();
    //    }
    //    public override void OnGUI()
    //    {
    //        if (GUILayout.Button("Save"))
    //        {
    //            ulong temp = m_Reflector.Action.Hash;
    //            target.Action = (long)temp;
    //        }

    //        using (var change = new EditorGUI.ChangeCheckScope())
    //        {
    //            Drawer.OnGUI();
    //            if (change.changed)
    //            {
    //                ulong temp = m_Reflector.Action.Hash;
    //                target.Action = (long)temp;
    //            }
    //        }
    //    }
    //}
}
