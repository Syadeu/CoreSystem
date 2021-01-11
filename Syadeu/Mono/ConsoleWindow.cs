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

        #region Initialze

        GUIStyle m_ConsoleLogStyle;
        GUIStyle m_ConsoleTextStyle;
        string m_ConsoleLog = "";
        string m_ConsoleText = "";
        Rect m_ConsoleRect = new Rect(0, 0, Screen.width, Screen.height * 0.5f);
        Rect m_PossibleRect;
        Rect m_ConsoleTextRect;
        Vector2 m_ConsoleLogScroll = new Vector2(0, 0);

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

            m_ConsoleTextRect = new Rect(m_ConsoleRect.x, m_ConsoleRect.y + m_ConsoleRect.height, Screen.width, 23);

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

            GUI.FocusControl("CmdTextField");
        }
        private void Console(int id)
        {
            m_ConsoleLogScroll = GUILayout.BeginScrollView(m_ConsoleLogScroll);
            GUILayout.TextArea(m_ConsoleLog, m_ConsoleLogStyle);
            GUILayout.EndScrollView();
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
            LogCommand(cmd);

            CommandDefinition def = LookDefinition(cmd);
        }

        private CommandDefinition LookDefinition(string cmd)
        {
            string[] split = cmd.Split(m_TextSeperator, 1, StringSplitOptions.None);
            string initializer = split[0];
            for (int i = 0; i < SyadeuSettings.Instance.m_CommandDefinitions.Count; i++)
            {
                if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.Equals(initializer))
                {
                    SyadeuSettings.Instance.m_CommandDefinitions[i].Run(split);
                    return SyadeuSettings.Instance.m_CommandDefinitions[i];
                }
            }

            // error
            return null;
        }
    }
}
