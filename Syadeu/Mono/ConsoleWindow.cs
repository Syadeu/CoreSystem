//using Syadeu.Extentions.EditorUtils;
//using Syadeu.Mono.Console;
//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//using UnityEngine;
//#if INPUTSYSTEM
//using UnityEngine.InputSystem;
//#endif

//namespace Syadeu.Mono
//{
//    public sealed class ConsoleWindow : StaticManager<ConsoleWindow>
//    {
//        public bool Opened { get; private set; } = false;

//        private List<CommandDefinition> PossibleDefs = new List<CommandDefinition>();
//        private List<CommandField> PossibleCmds = new List<CommandField>();
//        private CommandDefinition CurrentDefinition { get; set; }
//        private CommandField CurrentCommand { get; set; }

//        #region Initialze

//        GUIStyle m_ConsoleLogStyle;
//        GUIStyle m_ConsoleTextStyle;
//        GUIStyle m_ConsolePossStyle;
//        string m_ConsoleLog = "";
//        string m_ConsoleText = "";
//        Rect m_ConsoleRect = new Rect(0, 0, Screen.width, Screen.height * 0.5f);
//        Rect m_PossibleRect;
//        Rect m_ConsoleTextRect;
//        Vector2 m_ConsoleLogScroll = new Vector2(0, 0);
//        Vector2 m_PossibleCmdScroll = new Vector2(0, 0);

//        char[] m_TextSeperator = new char[] { ' ' };

//        [RuntimeInitializeOnLoadMethod]
//        private static void OnGameStart()
//        {
//            Instance.Initialize();
//        }
//        public override void OnInitialize()
//        {
//            Texture2D windowTexture = new Texture2D(1, 1);
//            windowTexture.SetPixel(1, 1, new Color(1, 1, 1, 0));
//            windowTexture.Apply();

//            m_ConsoleLogStyle = new GUIStyle("Box")
//            {
//                richText = true,
//                alignment = TextAnchor.UpperLeft,
//                fontSize = 15
//            };
//            m_ConsoleLogStyle.normal.background = windowTexture;
//            m_ConsoleLogStyle.normal.textColor = Color.white;
//            m_ConsoleTextStyle = new GUIStyle("Box")
//            {
//                alignment = TextAnchor.MiddleLeft,
//                padding = new RectOffset(3, 0, 0, 0),
//                fontSize = 15
//            };
//            m_ConsoleTextStyle.normal.textColor = new Color(.1f, .8f, .1f);
//            m_ConsolePossStyle = new GUIStyle("Label")
//            {
//                richText = true
//            };
//            m_ConsolePossStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

//            m_ConsoleTextRect = new Rect(m_ConsoleRect.x, m_ConsoleRect.y + m_ConsoleRect.height, Screen.width, 23);
//            m_PossibleRect = new Rect(Screen.width * 0.65f, m_ConsoleRect.height * 0.6f, Screen.width * 0.35f, m_ConsoleRect.height * 0.4f);

//            if (SyadeuSettings.Instance.m_UseConsole)
//            {
//                if (SyadeuSettings.Instance.m_UseOnlyDevelopmentBuild)
//                {
//#if UNITY_EDITOR || DEVELOPMENT_BUILD
//                    CoreSystem.OnUnityUpdate += InputCheck;
//#endif
//                }
//                else CoreSystem.OnUnityUpdate += InputCheck;
//            }

//            //KeySetting();

//            ConnectAction((arg) => $"test get : {arg}".ToLog(), "get");
//            ConnectAction((arg) => $"test get position : {arg}".ToLog(), "get", "position");
//            ConnectAction((arg) => $"test get position test1 : {arg}".ToLog(), "get", "position", "test1");
//            ConnectAction((arg) => $"test get position test1 test12 : {arg}".ToLog(), "get", "position", "test1", "test12");
//        }

//        private void InputCheck()
//        {
//#if INPUTSYSTEM
//            if (Keyboard.current.backquoteKey.wasPressedThisFrame)
//#else
//            if (Input.GetKeyDown(KeyCode.BackQuote))
//#endif
//            {
//                Opened = !Opened;
//            }

//            if (!Opened)
//            {
//                m_ConsoleText = "";
//                return;
//            }

//#if INPUTSYSTEM
//            if (Keyboard.current.enterKey.wasPressedThisFrame)
//#else
//            if (Input.GetKeyDown(KeyCode.Return))
//#endif
//            {
//                ExcuteCommand(m_ConsoleText);
//                m_ConsoleText = "";
//            }
//        }

//        #endregion

//        #region Window

//        private void OnGUI()
//        {
//            if (!Opened) return;

//            GUI.SetNextControlName("CmdWindow");
//            m_ConsoleRect = GUI.Window(0, m_ConsoleRect, Console, "", "Box");

//            GUI.SetNextControlName("CmdTextField");
//            m_ConsoleText = GUI.TextField(m_ConsoleTextRect, m_ConsoleText, m_ConsoleTextStyle);
//            if (GUI.changed)
//            {
//                SearchPossibleDefs(m_ConsoleText);
//            }

//            if (PossibleDefs.Count > 0 || PossibleCmds.Count > 0)
//            {
//                m_PossibleRect = GUI.Window(1, m_PossibleRect, PossibleCmdWindow, "", "Box");
//            }

//            GUI.FocusControl("CmdTextField");
//        }
//        private void Console(int id)
//        {
//            m_ConsoleLogScroll = GUILayout.BeginScrollView(m_ConsoleLogScroll);
//            GUILayout.TextArea(m_ConsoleLog, m_ConsoleLogStyle);
//            GUILayout.EndScrollView();
//        }
//        private void PossibleCmdWindow(int id)
//        {
//            m_PossibleCmdScroll = GUILayout.BeginScrollView(m_PossibleCmdScroll);
//            string sum = null;
//            for (int i = 0; i < PossibleDefs.Count; i++)
//            {
//                string output;
//                if (string.IsNullOrEmpty(sum)) output = PossibleDefs[i].m_Initializer;
//                else
//                {
//                    output = $"\n{PossibleDefs[i].m_Initializer}";
//                }

//                if (CurrentDefinition != null && CurrentDefinition == PossibleDefs[i])
//                {
//                    output = $"<color=teal>{output}</color>";
//                }
//                sum += output;
//            }
//            for (int i = 0; i < PossibleCmds.Count; i++)
//            {
//                string output;
//                if (string.IsNullOrEmpty(sum)) output = PossibleCmds[i].m_Field;
//                else
//                {
//                    output = $"\n{PossibleCmds[i].m_Field}";
//                }

//                if (CurrentCommand != null && CurrentCommand == PossibleCmds[i])
//                {
//                    output = $"<color=teal>{output}</color>";
//                }
//                sum += output;
//            }

//            GUILayout.Label(sum, m_ConsolePossStyle);
//            GUILayout.EndScrollView();
//        }

//        private void SearchPossibleDefs(string cmd)
//        {
//            if (string.IsNullOrEmpty(cmd))
//            {
//                PossibleDefs.Clear();
//                CurrentDefinition = null;
//                return;
//            }

//            CurrentDefinition = LookDefinition(cmd, ref PossibleDefs);
//            if (CurrentDefinition != null)
//            {
//                CurrentCommand = LookInside(cmd, CurrentDefinition, ref PossibleCmds);
//            }
//        }

//        #endregion

//        private void LogCommand(string log)
//        {
//            string output;
//            if (string.IsNullOrEmpty(m_ConsoleLog))
//            {
//                output = $"> <color=silver>{log}</color>";
//            }
//            else output = $"\n> <color=silver>{log}</color>";

//            m_ConsoleLog += output;

//            int logLength = m_ConsoleLog.Split('\n').Length;
//            m_ConsoleLogScroll.y = logLength * 15f;
//        }
//        private void ExcuteCommand(string cmd)
//        {
//            LogCommand(cmd);

//            if (!string.IsNullOrEmpty(cmd))
//            {
//                string[] vs = cmd.Split(m_TextSeperator, StringSplitOptions.RemoveEmptyEntries);
//                string arg = vs[vs.Length - 1];

//                if (CurrentCommand == null)
//                {
//                    if (CurrentDefinition != null)
//                    {
//                        if (arg.Equals(CurrentDefinition.m_Initializer)) arg = null;
//                        CurrentDefinition.Action?.Invoke(arg);
//                    }
//                }
//                else
//                {
//                    if (arg.Equals(CurrentCommand.m_Field)) arg = null;
//                    CurrentCommand.Action?.Invoke(arg);
//                }
//            }
            
//            PossibleDefs.Clear();
//            CurrentDefinition = null;
//            CurrentCommand = null;

//            //CommandDefinition def = LookDefinition(cmd);
//        }

//        private CommandDefinition LookDefinition(string cmd, ref List<CommandDefinition> possibleList)
//        {
//            string[] split = cmd.Split(m_TextSeperator, 2, StringSplitOptions.None);
//            string initializer = split[0].Trim();

//            possibleList.Clear();
//            if (CurrentDefinition != null &&
//                initializer.Equals(CurrentDefinition.m_Initializer)) return CurrentDefinition;

//            CommandDefinition bestDef = null;
//            for (int i = 0; i < SyadeuSettings.Instance.m_CommandDefinitions.Count; i++)
//            {
//                if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.Equals(initializer))
//                {
//                    bestDef = SyadeuSettings.Instance.m_CommandDefinitions[i];
//                }
//                else if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.StartsWith(initializer))
//                {
//                    possibleList.Add(SyadeuSettings.Instance.m_CommandDefinitions[i]);
//                }
//            }

//            return bestDef;
//        }
//        private CommandField LookInside(string cmd, CommandDefinition def, ref List<CommandField> possibleList)
//        {
//            string[] split = cmd.Split(m_TextSeperator, StringSplitOptions.RemoveEmptyEntries);
//            possibleList.Clear();

//            if (split.Length < 2) return null;

//            CommandField bestCmd = null;
//            if (split.Length == 2)
//            {
//                for (int i = 0; i < def.m_Args.Count; i++)
//                {
//                    if (def.m_Args[i].m_Field.Equals(split[1]))
//                    {
//                        bestCmd = def.m_Args[i];
//                    }
//                    else if (def.m_Args[i].m_Field.StartsWith(split[1]))
//                    {
//                        possibleList.Add(def.m_Args[i]);
//                    }
//                }
//                return bestCmd;
//            }

//            CommandField nextCmd = def.Find(split[1]);
//            for (int i = 2; i < split.Length - 1; i++)
//            {
//                nextCmd = nextCmd.Find(split[i]);
//            }

//            for (int i = 0; i < nextCmd.m_Args.Count; i++)
//            {
//                if (nextCmd.m_Args[i].m_Field.Equals(split[split.Length - 1]))
//                {
//                    bestCmd = nextCmd.m_Args[i];
//                }
//                else if (nextCmd.m_Args[i].m_Field.StartsWith(split[split.Length - 1]))
//                {
//                    possibleList.Add(nextCmd.m_Args[i]);
//                }
//            }

//            return bestCmd == null ? nextCmd : bestCmd;
//        }

//        private CommandDefinition FindDefinition(string name)
//        {
//            for (int i = 0; i < SyadeuSettings.Instance.m_CommandDefinitions.Count; i++)
//            {
//                if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.Equals(name))
//                {
//                    return SyadeuSettings.Instance.m_CommandDefinitions[i];
//                }
//            }
//            return null;
//        }
//        private void ConnectAction(Action<string> action, params string[] lines)
//        {
//            CommandDefinition def = FindDefinition(lines[0]);
//            if (def == null) throw new CoreSystemException(CoreSystemExceptionFlag.Console,
//                $"{lines[0]}의 명령어 시작 구문이 없거나 추가되지 않았습니다");

//            if (lines.Length < 2)
//            {
//                def.Action = action;
//                return;
//            }

//            CommandField nextCmd = def.Find(lines[1]);
//            for (int i = 2; i < lines.Length; i++)
//            {
//                nextCmd = nextCmd.Find(lines[i]);
//            }

//            nextCmd.Action = action;
//        }
//    }
//}
