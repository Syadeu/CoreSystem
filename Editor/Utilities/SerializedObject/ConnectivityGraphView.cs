// Copyright 2022 Seung Ha Kim
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !CORESYSTEM_DISABLE_CHECKS
#define DEBUG_MODE
#endif

using Codice.Client.BaseCommands;
using Syadeu.Collections;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Direction = UnityEditor.Experimental.GraphView.Direction;

namespace SyadeuEditor.Utilities
{
    // https://www.youtube.com/watch?v=7KHGH0fPL84

    // https://www.youtube.com/watch?v=F4cTWOxMjMY
    public class ConnectivityGraphView : GraphView
    {
        private static readonly Vector2 DefaultNodeSize = new Vector2(150, 200);

        internal class Helper
        {
            //private IConnectivity m_Connectivity;
            private SerializedProperty m_Property;
            //private readonly bool m_IsConstructedWithProperty;

            //public Helper(IConnectivity connectivity)
            //{
            //    m_Connectivity = connectivity;
            //    m_IsConstructedWithProperty = false;
            //}
            public Helper(SerializedProperty serializedProperty)
            {
                m_Property = serializedProperty;
                //m_IsConstructedWithProperty = true;
            }

            //public bool IsSerializedProperty => m_IsConstructedWithProperty;
            public Type ConnectivityType
            {
                get
                {
                    //if (m_IsConstructedWithProperty)
                    {
                        return m_Property.GetSystemType();
                    }
                    //return m_Connectivity.GetType();
                }
            }
            public Type UserDataType
            {
                get
                {
                    //if (m_IsConstructedWithProperty)
                    {
                        return m_Property.FindPropertyRelative("m_UserData").GetFieldTypeType();
                    }
                    //return m_Connectivity.UserDataType;
                }
            }
        }
        public class Node : UnityEditor.Experimental.GraphView.Node
        {
            private readonly Helper m_Helper;

            internal Node(Helper helper, object userData)
            {
                m_Helper = helper;
                this.userData = userData;

                Button choiceBtt = new Button(AddChoicePort);
                choiceBtt.text = "New";
                Button delBtt = new Button(RemovePort);
                delBtt.text = "Delete";

                titleContainer.Add(choiceBtt);
                titleContainer.Add(delBtt);

                //if (m_Helper.IsSerializedProperty)
                {
                    contentContainer.Add(new IMGUIContainer(OnSerializedPropertyGUI));
                }
                //else
                //{
                //    contentContainer.Add(new IMGUIContainer(OnObjectGUI));
                //}
            }

            private void AddChoicePort()
            {
                var port = AddOutput();
                var deleteButton = new Button(() => RemovePort(port))
                {
                    text = "X"
                };
                port.contentContainer.Add(deleteButton);
            }

            private void OnSerializedPropertyGUI()
            {
                if (userData == null)
                {
                    EditorGUILayout.HelpBox("UserData is null", MessageType.Error);
                    return;
                }

                EditorGUILayout.PropertyField((SerializedProperty)userData);
            }
            //private void OnObjectGUI()
            //{
            //    throw new NotImplementedException();
            //}

            public Port AddPort(
                string name, Direction direction, Port.Capacity capacity, Type targeType, Orientation orientation = Orientation.Horizontal)
            {
                Port port = InstantiatePort(
                    orientation,
                    direction,
                    capacity,
                    targeType);

                port.portName = name;
                outputContainer.Add(port);

                RefreshExpandedState();
                RefreshPorts();

                return port;
            }
            public void RemovePort()
            {
                if (outputContainer.childCount == 0) return;

                outputContainer.RemoveAt(outputContainer.childCount - 1);

                RefreshExpandedState();
                RefreshPorts();
            }
            public void RemovePort(Port port)
            {
                if (outputContainer.childCount == 0) return;

                outputContainer.Remove(port);

                RefreshExpandedState();
                RefreshPorts();
            }

            public Port AddOutput()
            {
                var outputPortCount = outputContainer.Query("connector").ToList().Count;

                var port = AddPort(
                    $"{outputPortCount} Next ({TypeHelper.ToString(m_Helper.ConnectivityType)})",
                    Direction.Output, Port.Capacity.Single, m_Helper.ConnectivityType);

                return port;
            }
            public Port AddInput()
            {
                var port = AddPort(
                    $"Input ({TypeHelper.ToString(m_Helper.ConnectivityType)})",
                    Direction.Input,
                    Port.Capacity.Single,
                    m_Helper.ConnectivityType);

                return port;
            }
        }

        private readonly Helper m_Helper;
        private readonly Node m_EntryNode;

        protected override bool canDeleteSelection
        {
            get
            {
                if (selection.Contains(m_EntryNode))
                {
                    return false;
                }

                return base.canDeleteSelection;
            }
        }

        private ConnectivityGraphView() { }
        public ConnectivityGraphView(SerializedProperty connectivity)
        {
            m_Helper = new Helper(connectivity);

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();

            SerializedProperty nodesProp = connectivity.FindPropertyRelative("m_Nodes");
            if (nodesProp.arraySize == 0)
            {
                nodesProp.InsertArrayElementAtIndex(0);
            }
            SerializedProperty userDataProp = nodesProp.GetArrayElementAtIndex(0).FindPropertyRelative("m_UserData");

            m_EntryNode = PrivateCreateNode("Entry", Vector2.zero, userDataProp.Copy());

            
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Create Node", (t) =>
            {
                CreateNode("Node", evt.localMousePosition, null);
                //Debug.Log($"in {evt.mousePosition} :: {evt.localMousePosition} :: {mousePosition}");
                return;
            });
            evt.menu.AppendSeparator();

            base.BuildContextualMenu(evt);
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            List<Port> compatiblePorts = new List<Port>();

            ports.ForEach(port =>
            {
                if (startPort != port && startPort.node != port.node &&
                    startPort.direction != port.direction)
                {
                    compatiblePorts.Add(port);
                }
            });

            return compatiblePorts;
        }
        
        private Node PrivateCreateNode(string name, Vector2 pos, object userData)
        {
            Node node = new Node(m_Helper, userData)
            {
                title = name
            };
            AddElement(node);
            node.SetPosition(new Rect(pos, DefaultNodeSize));

            return node;
        }
        public Node CreateNode(string name, Vector2 pos, object userData)
        {
            Node node = PrivateCreateNode(name, pos, userData);

            var port = node.AddPort(
                $"Input ({TypeHelper.ToString(m_Helper.ConnectivityType)})", 
                Direction.Input, 
                Port.Capacity.Single, 
                m_Helper.ConnectivityType);

            node.StretchToParentSize();

            return node;
        }
    }

    public class ConnectivityGraphViewWindow : EditorWindow
    {
        #region Static 

        private static ConnectivityGraphViewWindow s_Window = null;
        //public static ConnectivityGraphViewWindow Open(IConnectivity connectivity)
        //{
        //    if (s_Window == null)
        //    {
        //        s_Window = GetWindow<ConnectivityGraphViewWindow>();

        //        if (s_Window.EnableAutoDock)
        //        {
        //            GetWindow(s_Window.AutoDockTarget).DockWindow(s_Window, s_Window.AutoDockPosition);
        //        }

        //        s_Window.Initialize(connectivity);
        //    }
        //    return s_Window;
        //}
        public static ConnectivityGraphViewWindow Open(SerializedProperty connectivity)
        {
            if (s_Window == null)
            {
                s_Window = GetWindow<ConnectivityGraphViewWindow>();

                if (s_Window.EnableAutoDock)
                {
                    GetWindow(s_Window.AutoDockTarget).DockWindow(s_Window, s_Window.AutoDockPosition);
                }

                s_Window.Initialize(connectivity);
            }
            return s_Window;
        }
        public static ConnectivityGraphViewWindow Open<TWindow>(SerializedProperty connectivity, Docker.DockPosition dockPosition)
            where TWindow : EditorWindow
        {
            if (s_Window == null)
            {
                s_Window = GetWindow<ConnectivityGraphViewWindow>();

                GetWindow<TWindow>().DockWindow(s_Window, dockPosition);

                s_Window.Initialize(connectivity);
            }
            return s_Window;
        }

        #endregion

        protected virtual bool EnableAutoDock => false;
        protected virtual Type AutoDockTarget { get; }
        protected virtual Docker.DockPosition AutoDockPosition { get; }

        private bool m_Initialized = false;
        private ConnectivityGraphView m_GraphView;

        #region Initialize

        //private void Initialize(IConnectivity connectivity)
        //{
        //    m_GraphView = new ConnectivityGraphView(connectivity);
        //    m_GraphView.StretchToParentSize();

        //    rootVisualElement.Add(m_GraphView);
        //    Toolbar();
        //    MiniMap();
        //    BlackBoard();

        //    m_Initialized = true;
        //}
        private void Initialize(SerializedProperty connectivity)
        {
            m_GraphView = new ConnectivityGraphView(connectivity);
            m_GraphView.StretchToParentSize();

            rootVisualElement.Add(m_GraphView);
            Toolbar();
            MiniMap();
            BlackBoard();

            m_Initialized = true;
        }

        #endregion

        #region Unity Messages

        private void OnDisable()
        {
            if (!m_Initialized) return;

            rootVisualElement.Remove(m_GraphView);
        }
        private void OnDestroy()
        {
            s_Window = null;
        }

        #endregion

        private void Toolbar()
        {
            Toolbar toolbar = new Toolbar();

            var nodeCreateBtt = new Button(OnNodeCreateButton);
            nodeCreateBtt.text = "Create Node";
            toolbar.Add(nodeCreateBtt);

            rootVisualElement.Add(toolbar);
        }
        private void MiniMap()
        {
            var miniMap = new MiniMap() { anchored = true };

            var cords = m_GraphView.contentViewContainer.WorldToLocal(new Vector2(this.maxSize.x - 10, 30));

            miniMap.SetPosition(new Rect(cords.x, cords.y, 200, 140));
            m_GraphView.Add(miniMap);
        }
        private void BlackBoard()
        {
            var blackboard = new Blackboard(m_GraphView);

            blackboard.Add(new BlackboardSection { title = "Exposed Properties" });
            //blackboard.addItemRequested = _blackboard =>
            //{
            //    m_GraphView.add
            //};
            blackboard.Add(new IMGUIContainer(OnBlackBoardGUI));

            var cords = m_GraphView.contentViewContainer.WorldToLocal(new Vector2(10, 30));
            blackboard.SetPosition(new Rect(cords.x, cords.y, 200, 300));
            m_GraphView.Add(blackboard);
        }

        private void OnNodeCreateButton()
        {
            var node = m_GraphView.CreateNode("Node", Event.current.mousePosition, null);
        }
        private void OnBlackBoardGUI()
        {
            EditorGUILayout.LabelField("testest label");
        }
    }
}
