using UnityEngine;
using UnityEditor;
using SyadeuEditor.Utilities;
using Syadeu.Presentation.Data;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Syadeu.Collections;

namespace SyadeuEditor.Presentation
{
    [CustomPropertyDrawer(typeof(PositionConnectivityData))]
    internal sealed class PositionConnectivityDataPropertyDrawer : DataObjectBasePropertyDrawer
    {
        static class ConnectivityHelper
        {
            public static SerializedProperty GetConnectivityProperty(SerializedProperty property)
            {
                const string c_Str = "m_Connectivity";
                return property.FindPropertyRelative(c_Str);
            }
            public static SerializedProperty GetNodeArrayProperty(SerializedProperty connectivity)
            {
                const string c_Str = "m_Nodes", c_Arr = "p_Array";
                return connectivity.FindPropertyRelative(c_Str).FindPropertyRelative(c_Arr);
            }
            public static SerializedProperty GetUserData(SerializedProperty connectivity)
            {
                const string c_Str = "m_UserData";
                return connectivity.FindPropertyRelative(c_Str);
            }
        }

        private SerializedProperty GetCirculerProperty(SerializedProperty property)
        {
            const string c_Str = "m_Circuler";
            return property.FindPropertyRelative(c_Str);
        }

        private sealed class TestView : GraphView
        {
            public TestView()
            {
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger());
                this.AddManipulator(new RectangleSelector());

                AddElement(CreateEntryNode());
            }

            private Node CreateEntryNode()
            {
                TestNode node = new TestNode()
                {
                    title = "main",
                };
                node.SetPosition(new Rect(100, 200, 100, 150));

                return node;
            }
        }
        private sealed class TestNode : Node
        {

        }

        

        #region Overrides

        protected override void OnInitialize(SerializedProperty property)
        {
            base.OnInitialize(property);

            //m_GraphView = new TestView();
        }

        protected override float PropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            height += ThisHeight(property, label);

            return DefaultHeight(property, label) + height;
        }
        protected override void OnPropertyGUI(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            DrawDefault(ref rect, property, label);

            DrawDataObject(ref rect, property, label);
            DrawThis(ref rect, property, label);

            //DrawFrom(ref rect, GetHashProperty(property));
        }

        #endregion

        private float ThisHeight(SerializedProperty property, GUIContent label)
        {
            float height = 0;

            height += 100;

            return height;
        }
        private void DrawThis(ref AutoRect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty
                connectivityProp = ConnectivityHelper.GetConnectivityProperty(property);

            Line(ref rect);

            PropertyField(ref rect, GetCirculerProperty(property));
            PropertyField(ref rect, connectivityProp, connectivityProp.isExpanded);

            //m_GraphView.StretchToParentWidth();
            if (Button(ref rect, "test"))
            {
                GraphViewWindow.Open();
            }

            //GUI.Window(0, rect.Pop(100), GetGraph, "");
        }

        static void GetGraph(int unusedWindowID)
        {
            //GUI.DragWindow();
        }

        private sealed class GraphViewWindow : EditorWindow
        {
            private TestView m_GraphView;

            public static void Open()
            {
                var window = EditorWindow.GetWindow<GraphViewWindow>(
                    //TypeHelper.TypeOf<EntityWindow>.Type
                    );
                GetWindow<EntityWindow>().DockWindow(window, Docker.DockPosition.Bottom);
            }

            private void OnEnable()
            {
                m_GraphView = new TestView();
                m_GraphView.StretchToParentSize();

                rootVisualElement.Add(m_GraphView);
            }
            private void OnDisable()
            {
                rootVisualElement.Remove(m_GraphView);
            }
        }
    }
}
