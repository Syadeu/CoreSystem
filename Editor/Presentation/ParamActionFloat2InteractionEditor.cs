using Syadeu.Internal;
using Syadeu.Presentation.Input;
using UnityEditor;
using UnityEngine.InputSystem.Editor;

namespace SyadeuEditor.Presentation
{
    public class ParamActionFloat2InteractionEditor : InputParameterEditor<ParamActionFloat2Interaction>
    {
        ObjectDrawer Drawer;

        protected override void OnEnable()
        {
            //ObjectDrawer.ToDrawer(target, ReflectionHelper.g)
            Drawer = new ObjectDrawer(
                target,
                TypeHelper.TypeOf<ParamActionFloat2Interaction>.Type,
                string.Empty);

            base.OnEnable();
        }
        public override void OnGUI()
        {
            Drawer.OnGUI();
        }
    }
}
