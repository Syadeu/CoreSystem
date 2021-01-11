using Syadeu.Extentions.EditorUtils;
using Syadeu.Mono.Console;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;
#if INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace Syadeu.Mono
{
    public sealed class ConsoleWindow : StaticManager<ConsoleWindow>
    {
        public bool Opened { get; private set; } = false;

        private List<CommandDefinition> PossibleDefs = new List<CommandDefinition>();
        private CommandDefinition CurrentDefinition { get; set; }

        #region Initialze

        GUIStyle m_ConsoleLogStyle;
        GUIStyle m_ConsoleTextStyle;
        GUIStyle m_ConsolePossStyle;
        string m_ConsoleLog = "";
        string m_ConsoleText = "";
        Rect m_ConsoleRect = new Rect(0, 0, Screen.width, Screen.height * 0.5f);
        Rect m_PossibleRect;
        Rect m_ConsoleTextRect;
        Vector2 m_ConsoleLogScroll = new Vector2(0, 0);
        Vector2 m_PossibleCmdScroll = new Vector2(0, 0);

        char[] m_TextSeperator = new char[] { ' ' };

        [RuntimeInitializeOnLoadMethod]
        private static void OnGameStart()
        {
            Instance.Initialize();
        }
        public override void OnInitialize()
        {
            Texture2D windowTexture = new Texture2D(1, 1);
            windowTexture.SetPixel(1, 1, new Color(1, 1, 1, 0));
            windowTexture.Apply();

            m_ConsoleLogStyle = new GUIStyle("Box")
            {
                richText = true,
                alignment = TextAnchor.UpperLeft,
                fontSize = 15
            };
            m_ConsoleLogStyle.normal.background = windowTexture;
            m_ConsoleLogStyle.normal.textColor = Color.white;
            m_ConsoleTextStyle = new GUIStyle("Box")
            {
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(3, 0, 0, 0),
                fontSize = 15
            };
            m_ConsolePossStyle = new GUIStyle("Label")
            {
                richText = true
            };
            m_ConsolePossStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

            m_ConsoleTextRect = new Rect(m_ConsoleRect.x, m_ConsoleRect.y + m_ConsoleRect.height, Screen.width, 23);
            m_PossibleRect = new Rect(Screen.width * 0.65f, m_ConsoleRect.height * 0.6f, Screen.width * 0.35f, m_ConsoleRect.height * 0.4f);

            if (SyadeuSettings.Instance.m_UseConsole)
            {
                if (SyadeuSettings.Instance.m_UseOnlyDevelopmentBuild)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    CoreSystem.OnUnityUpdate += InputCheck;
#endif
                }
                else CoreSystem.OnUnityUpdate += InputCheck;
            }
            
            //KeySetting();
        }

        private void InputCheck()
        {
#if INPUTSYSTEM
            if (Keyboard.current.backquoteKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.BackQuote))
#endif
            {
                Opened = !Opened;
            }

            if (!Opened)
            {
                m_ConsoleText = "";
                return;
            }

#if INPUTSYSTEM
            if (Keyboard.current.enterKey.wasPressedThisFrame)
#else
            if (Input.GetKeyDown(KeyCode.Return))
#endif
            {
                ExcuteCommand(m_ConsoleText);
                m_ConsoleText = "";
            }
        }

        #endregion

        #region Window

        private void OnGUI()
        {
            if (!Opened) return;

            GUI.SetNextControlName("CmdWindow");
            m_ConsoleRect = GUI.Window(0, m_ConsoleRect, Console, "", "Box");

            GUI.SetNextControlName("CmdTextField");
            m_ConsoleText = GUI.TextField(m_ConsoleTextRect, m_ConsoleText, m_ConsoleTextStyle);
            if (GUI.changed)
            {
                SearchPossibleDefs(m_ConsoleText);
            }

            if (PossibleDefs.Count > 0)
            {
                m_PossibleRect = GUI.Window(1, m_PossibleRect, PossibleCmdWindow, "", "Box");
            }

            GUI.FocusControl("CmdTextField");
        }
        private void Console(int id)
        {
            m_ConsoleLogScroll = GUILayout.BeginScrollView(m_ConsoleLogScroll);
            GUILayout.TextArea(m_ConsoleLog, m_ConsoleLogStyle);
            GUILayout.EndScrollView();
        }
        private void PossibleCmdWindow(int id)
        {
            m_PossibleCmdScroll = GUILayout.BeginScrollView(m_PossibleCmdScroll);
            string sum = null;
            for (int i = 0; i < PossibleDefs.Count; i++)
            {
                string output;
                if (string.IsNullOrEmpty(sum)) output = PossibleDefs[i].m_Initializer;
                else
                {
                    output = $"\n{PossibleDefs[i].m_Initializer}";
                }

                if (CurrentDefinition != null && CurrentDefinition == PossibleDefs[i])
                {
                    output = $"<color=teal>{output}</color>";
                }
                sum += output;
            }

            GUILayout.Label(sum, m_ConsolePossStyle);
            GUILayout.EndScrollView();
        }

        private void SearchPossibleDefs(string cmd)
        {
            if (string.IsNullOrEmpty(cmd))
            {
                PossibleDefs.Clear();
                CurrentDefinition = null;
                return;
            }

            CurrentDefinition = LookDefinition(cmd, ref PossibleDefs);
        }

        #endregion

        private void LogCommand(string log)
        {
            string output;
            if (string.IsNullOrEmpty(m_ConsoleLog))
            {
                output = $"> <color=silver>{log}</color>";
            }
            else output = $"\n> <color=silver>{log}</color>";

            m_ConsoleLog += output;

            int logLength = m_ConsoleLog.Split('\n').Length;
            m_ConsoleLogScroll.y = logLength * 15f;
        }
        private void ExcuteCommand(string cmd)
        {
            PossibleDefs.Clear();
            CurrentDefinition = null;

            LogCommand(cmd);

            
            //CommandDefinition def = LookDefinition(cmd);
        }

        private CommandDefinition LookDefinition(string cmd, ref List<CommandDefinition> possibleList)
        {
            string[] split = cmd.Split(m_TextSeperator, 1, StringSplitOptions.None);
            string initializer = split[0];

            possibleList.Clear();
            if (CurrentDefinition != null &&
                initializer.Equals(CurrentDefinition.m_Initializer)) return CurrentDefinition;

            CommandDefinition bestDef = null;
            for (int i = 0; i < SyadeuSettings.Instance.m_CommandDefinitions.Count; i++)
            {
                if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.Equals(initializer))
                {
                    bestDef = SyadeuSettings.Instance.m_CommandDefinitions[i];
                }

                if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.StartsWith(initializer))
                {
                    possibleList.Add(SyadeuSettings.Instance.m_CommandDefinitions[i]);
                }
            }

            return bestDef;
        }
    }
}
