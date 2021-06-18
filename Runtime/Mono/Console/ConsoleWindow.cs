using System;
using System.Collections.Generic;
using Syadeu.Mono.Console;

using UnityEngine;
#if INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace Syadeu.Mono
{
    [StaticManagerDescription("" +
        "Console System")]
    public sealed class ConsoleWindow : StaticManager<ConsoleWindow>
    {
        public static void Log(string log, ConsoleFlag flag = ConsoleFlag.Normal) => Instance.LogCommand(log, flag);
        public static void LogAssert(bool isTrue, string log, bool throwException = true)
            => Instance.InternalLogAssert(isTrue, log, Environment.StackTrace, throwException);

        public static void AddCommand(Action<string> action, params string[] arguments)
            => Instance.InternalAddCommand(action, arguments);
        public static void AddCommand(Action<string> action, CommandRequires requires, params string[] arguments)
            => Instance.InternalAddCommand(action, requires, arguments);
        public static void AddCommand(Action<string> action, Type scriptType, string methodName, object expectValue, params string[] arguments)
            => Instance.InternalAddCommand(action, scriptType, methodName, expectValue, arguments);
        public static void AddCommand(Action<string> action, Type scriptType, string methodName, CommandRequiresDelegate predictate, params string[] arguments)
            => Instance.InternalAddCommand(action, scriptType, methodName, predictate, arguments);
        public static void CreateCommand(Action<string> action, params string[] arguments)
            => Instance.InternalCreateCommand(action, arguments);
        
        public static event Action OnErrorReceieved;

        public bool Opened { get; private set; } = false;

        #region Initialze

        private List<CommandDefinition> PossibleDefs = new List<CommandDefinition>();
        private List<CommandField> PossibleCmds = new List<CommandField>();

        private CommandDefinition CurrentDefinition { get; set; }
        private CommandField CurrentCommand { get; set; }
        private string LastCommand { get; set; } = null;

        GUIStyle m_ConsoleLogStyle = null;
        GUIStyle m_ConsoleTextStyle = null;
        GUIStyle m_ConsolePossStyle = null;
        string m_ConsoleLog = "";
        string m_ConsoleText = "";
        Rect m_ConsoleRect = new Rect(0, 0, Screen.width, Screen.height * 0.5f);
        Rect m_PossibleRect;
        Rect m_ConsoleTextRect;
        Vector2 m_ConsoleLogScroll = new Vector2(0, 0);
        Vector2 m_PossibleCmdScroll = new Vector2(0, 0);

        readonly char[] m_TextSeperator = new char[] { ' ' };

        [RuntimeInitializeOnLoadMethod]
        private static void OnGameStart()
        {
            Instance.Initialize();
        }
        public override void OnInitialize()
        {
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

                Application.logMessageReceived += Application_logMessageReceived;

                if (FindDefinition("clear") == null)
                {
                    InternalCreateCommand((arg) =>
                    {
                        m_ConsoleLog = "";
                        m_ConsoleLogScroll = Vector2.zero;
                    }, "clear");
                }
            }
        }
        private void Application_logMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (SyadeuSettings.Instance.m_ConsoleLogWhenLogRecieved &&
                SyadeuSettings.Instance.m_ConsoleLogTypes.HasFlag(ConvertFlag(type)))
            {
                if (SyadeuSettings.Instance.m_ConsoleLogOnlyIsDevelopment)
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    if (ConvertFlag(type) == ConsoleFlag.Error)
                    {
                        InternalLogAssert(true, condition, stackTrace, SyadeuSettings.Instance.m_ConsoleThrowWhenErrorRecieved);
                    }
                    else InternalLog(condition, StringColor.white);
#endif
                }
                else
                {
                    if (ConvertFlag(type) == ConsoleFlag.Error)
                    {
                        InternalLogAssert(true, condition, stackTrace, SyadeuSettings.Instance.m_ConsoleThrowWhenErrorRecieved);
                    }
                    else InternalLog(condition, StringColor.white);
                }
            }
        }

        #endregion

        #region Window

        private void InputCheck()
        {
#if INPUTSYSTEM
            if (Keyboard.current.backquoteKey.wasPressedThisFrame)
            {
                Opened = !Opened;
            }
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Opened = false;
            }
#endif
            if (!Opened)
            {
                m_ConsoleText = "";
                return;
            }

#if INPUTSYSTEM
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ExcuteCommand(m_ConsoleText);
                m_ConsoleText = "";
            }
#endif
        }
        private void OnGUI()
        {
            if (m_ConsoleLogStyle == null)
            {
                Texture2D windowTexture = new Texture2D(1, 1);
                windowTexture.SetPixel(1, 1, new Color(1, 1, 1, .25f));
                windowTexture.Apply();
                m_ConsoleLogStyle = new GUIStyle("Box")
                {
                    richText = true,
                    alignment = TextAnchor.UpperLeft,
                    fontSize = SyadeuSettings.Instance.m_ConsoleFontSize,

                    fixedWidth = Screen.width * .983f
                };
                m_ConsoleLogStyle.normal.background = windowTexture;
                m_ConsoleLogStyle.normal.textColor = Color.white;
            }
            if (m_ConsoleTextStyle == null)
            {
                m_ConsoleTextStyle = new GUIStyle("Box")
                {
                    alignment = TextAnchor.MiddleLeft,
                    padding = new RectOffset(3, 0, 0, 0),
                    fontSize = SyadeuSettings.Instance.m_ConsoleFontSize
                };
                m_ConsoleTextStyle.normal.textColor = new Color(.1f, .8f, .1f);
            }
            if (m_ConsolePossStyle == null)
            {
                m_ConsolePossStyle = new GUIStyle("Label")
                {
                    richText = true,
                    fontSize = SyadeuSettings.Instance.m_ConsoleFontSize
                };
                m_ConsolePossStyle.normal.textColor = Color.white;
            }

            if (!SyadeuSettings.Instance.m_UseConsole) return;
            if (SyadeuSettings.Instance.m_UseOnlyDevelopmentBuild)
            {
#if !UNITY_EDITOR || !DEVELOPMENT_BUILD
                return;
#endif
            }


#if !INPUTSYSTEM
            if (Event.current.keyCode == KeyCode.BackQuote && Event.current.type == EventType.KeyDown)
            {
                m_ConsoleText = "";
                Opened = !Opened;
                return;
            }
            if (Event.current.keyCode == KeyCode.BackQuote && Event.current.type == EventType.KeyUp)
            {
                m_ConsoleText = "";
                return;
            }
            if (Event.current.keyCode == KeyCode.Escape)
            {
                Opened = false;
            }
#endif

            if (!Opened) return;
#if !INPUTSYSTEM
            if (Event.current.keyCode == KeyCode.Return && Event.current.type == EventType.KeyDown)
            {
                ExcuteCommand(m_ConsoleText);
                m_ConsoleText = "";
                return;
            }
#endif

            GUI.SetNextControlName("CmdWindow");
            m_ConsoleRect = GUI.Window(0, m_ConsoleRect, Console, "", "Box");

            GUI.SetNextControlName("CmdTextField");
            m_ConsoleText = GUI.TextField(m_ConsoleTextRect, m_ConsoleText, m_ConsoleTextStyle);
            if (GUI.changed)
            {
                SearchPossibleDefs(m_ConsoleText);
            }

            if (PossibleDefs.Count > 0 || PossibleCmds.Count > 0)
            {
                m_PossibleRect = GUI.Window(1, m_PossibleRect, PossibleCmdWindow, "", "Box");
            }

            GUI.FocusControl("CmdTextField");

            if (Event.current.keyCode == KeyCode.Tab)
            {
                if (Event.current.type == EventType.KeyDown) QuickTab();
                else if (Event.current.type == EventType.KeyUp) SetTextCursorToLast("CmdTextField");
            }
        }
        private void Console(int id)
        {
            m_ConsoleLogScroll = GUILayout.BeginScrollView(m_ConsoleLogScroll, false, true);
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
            for (int i = 0; i < PossibleCmds.Count; i++)
            {
                string output;
                if (string.IsNullOrEmpty(sum)) output = PossibleCmds[i].m_Field;
                else
                {
                    output = $"\n{PossibleCmds[i].m_Field}";
                }

                if (CurrentCommand != null && CurrentCommand == PossibleCmds[i])
                {
                    output = $"<color=teal>{output}</color>";
                }
                sum += output;
            }

            GUILayout.Label(sum, m_ConsolePossStyle);
            GUILayout.EndScrollView();
        }

        private void QuickTab()
        {
            string[] split = m_ConsoleText.Split(m_TextSeperator, StringSplitOptions.RemoveEmptyEntries);
            if (CurrentDefinition != null)
            {
                if (PossibleCmds.Count > 0)
                {
                    CommandField bestField = FindClosestField(split, ref PossibleCmds);
                    PossibleCmds.Clear();
                    CurrentCommand = bestField;
                    m_ConsoleText = 
                        m_ConsoleText.Substring(0, m_ConsoleText.Length - split[split.Length - 1].Length);
                    m_ConsoleText += bestField.m_Field;
                }
            }
            else if (PossibleDefs.Count > 0)
            {
                CommandDefinition bestDef = FindClosestDefinition(split, ref PossibleDefs);
                PossibleDefs.Clear();
                CurrentDefinition = bestDef;
                m_ConsoleText = bestDef.m_Initializer;
            }
            else
            {
                if (!string.IsNullOrEmpty(LastCommand))
                {
                    m_ConsoleText = LastCommand;
                    SearchPossibleDefs(m_ConsoleText);
                }
            }
        }

        private CommandDefinition FindClosestDefinition(string[] cmds, ref List<CommandDefinition> defs)
        {
            if (defs.Count == 0) return null;

            CommandDefinition bestDef = null;
            int bestLength = 9999;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i] == null)
                {
                    defs.RemoveAt(i);
                    i--;
                    continue;
                }

                int length = defs[i].m_Initializer.Trim(cmds[0].ToCharArray()).Length;
                if (length < bestLength)
                {
                    bestDef = defs[i];
                    bestLength = length;
                }
            }

            return bestDef;
        }
        private CommandField FindClosestField(string[] cmds, ref List<CommandField> fields)
        {
            if (fields.Count == 0) return null;

            string lastField = cmds[cmds.Length - 1];
            CommandField bestField = null;
            int bestLength = 9999;
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i] == null)
                {
                    fields.RemoveAt(i);
                    i--;
                    continue;
                }

                int length = fields[i].m_Field.Trim(lastField.ToCharArray()).Length;
                if (length < bestLength)
                {
                    bestField = fields[i];
                    bestLength = length;
                }
            }

            return bestField;
        }

        #endregion

        #region Utils

        private TextEditor SetTextCursorToLast(string controlName)
        {
            GUI.FocusControl(controlName);
            TextEditor t = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);

            t.cursorIndex = t.text.Length;
            t.selectIndex = t.text.Length;

            return t;
        }

        #endregion

        #region Internals

        private ConsoleFlag ConvertFlag(LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                return ConsoleFlag.Error;
            }
            else if (type == LogType.Warning) return ConsoleFlag.Warning;

            return ConsoleFlag.Normal;
        }

        private enum StringColor
        {
            sliver, // user

            maroon, // red
            orange, //
            teal, // green
            white
        }
        private string ConvertString(string text, StringColor color)
            => $"<color={color}>{text}</color>";
        private void InternalLog(string text, StringColor color)
        {
            string output;
            if (string.IsNullOrEmpty(m_ConsoleLog))
            {
                output = $"> {ConvertString(text, color)}";
            }
            else output = $"\n> {ConvertString(text, color)}";

            m_ConsoleLog += output;
            int logLength = m_ConsoleLog.Split('\n').Length;
            m_ConsoleLogScroll.y = logLength * 15f;
        }
        private void InternalLogAssert(bool isTrue, string log, string trace, bool throwException = true)
        {
            if (isTrue)
            {
                InternalLog($"{log}\n{trace}", StringColor.maroon);
                OnErrorReceieved?.Invoke();
                if (throwException)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Console, log, trace);
                }
            }
        }
        private void LogCommand(string log, ConsoleFlag flag = ConsoleFlag.Normal)
        {
            if (flag == ConsoleFlag.Error)
            {
                InternalLog(log, StringColor.maroon);
                OnErrorReceieved?.Invoke();
                if (SyadeuSettings.Instance.m_ConsoleThrowWhenErrorRecieved)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Console, log);
                }
            }
            else if (flag == ConsoleFlag.Warning)
            {
                InternalLog(log, StringColor.orange);
            }
            else
            {
                InternalLog(log, StringColor.white);
            }
        }
        private void ExcuteCommand(string cmd)
        {
            InternalLog(cmd, StringColor.sliver);

            if (!string.IsNullOrEmpty(cmd))
            {
                string[] vs = cmd.Split(m_TextSeperator, StringSplitOptions.RemoveEmptyEntries);
                string arg = vs[vs.Length - 1];

                if (CurrentCommand == null)
                {
                    if (CurrentDefinition != null)
                    {
                        if (arg.Equals(CurrentDefinition.m_Initializer)) arg = null;

                        if (CurrentDefinition.Requires != null)
                        {
                            if (CurrentDefinition.Requires.Invoke())
                            {
                                CurrentDefinition.Action?.Invoke(arg);
                            }
                        }
                        else CurrentDefinition.Action?.Invoke(arg);

                        if (CurrentDefinition.Action != null) LastCommand = cmd;
                    }
                }
                else
                {
                    if (arg.Equals(CurrentCommand.m_Field)) arg = null;

                    if (CurrentCommand.Requires != null)
                    {
                        if (CurrentCommand.Requires.Invoke())
                        {
                            CurrentCommand.Action?.Invoke(arg);
                        }
                    }
                    else CurrentCommand.Action?.Invoke(arg);

                    if (CurrentCommand.Action != null) LastCommand = cmd;
                }
            }

            PossibleDefs.Clear();

            CurrentDefinition = null;
            CurrentCommand = null;
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
            if (CurrentDefinition != null)
            {
                CurrentCommand = LookInside(cmd, CurrentDefinition, ref PossibleCmds);
            }
        }
        private CommandDefinition LookDefinition(string cmd, ref List<CommandDefinition> possibleList)
        {
            string[] split = cmd.Split(m_TextSeperator, 2, StringSplitOptions.None);
            string initializer = split[0].Trim();

            possibleList.Clear();
            if (CurrentDefinition != null &&
                initializer.Equals(CurrentDefinition.m_Initializer)) return CurrentDefinition;

            CommandDefinition bestDef = null;
            for (int i = 0; i < SyadeuSettings.Instance.m_CommandDefinitions.Count; i++)
            {
                if (SyadeuSettings.Instance.m_CommandDefinitions[i] == null)
                {
                    SyadeuSettings.Instance.m_CommandDefinitions.RemoveAt(i);
                    i--;
                    continue;
                }

                if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.Equals(initializer))
                {
                    if (SyadeuSettings.Instance.m_CommandDefinitions[i].Requires != null)
                    {
                        if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Settings.HasFlag(CommandSetting.ShowIfRequiresTrue))
                        {
                            if (SyadeuSettings.Instance.m_CommandDefinitions[i].Requires.Invoke()) bestDef = SyadeuSettings.Instance.m_CommandDefinitions[i];
                        }
                        else bestDef = SyadeuSettings.Instance.m_CommandDefinitions[i];
                    }
                    else bestDef = SyadeuSettings.Instance.m_CommandDefinitions[i];
                }
                else if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.StartsWith(initializer))
                {
                    if (SyadeuSettings.Instance.m_CommandDefinitions[i].Requires != null)
                    {
                        if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Settings.HasFlag(CommandSetting.ShowIfRequiresTrue))
                        {
                            if (SyadeuSettings.Instance.m_CommandDefinitions[i].Requires.Invoke()) possibleList.Add(SyadeuSettings.Instance.m_CommandDefinitions[i]);
                        }
                        else possibleList.Add(SyadeuSettings.Instance.m_CommandDefinitions[i]);
                    }
                    else possibleList.Add(SyadeuSettings.Instance.m_CommandDefinitions[i]);
                }
            }

            return bestDef;
        }
        private CommandField LookInside(string cmd, CommandDefinition def, ref List<CommandField> possibleList)
        {
            string[] split = cmd.Split(m_TextSeperator, StringSplitOptions.RemoveEmptyEntries);
            possibleList.Clear();

            if (split.Length < 2) return null;

            CommandField bestCmd = null;
            if (split.Length == 2)
            {
                for (int i = 0; i < def.m_Args.Count; i++)
                {
                    if (def.m_Args[i] == null)
                    {
                        def.m_Args.RemoveAt(i);
                        i--;
                        continue;
                    }

                    if (def.m_Args[i].m_Field.Equals(split[1]))
                    {
                        if (def.m_Args[i].Requires != null)
                        {
                            if (def.m_Args[i].m_Settings.HasFlag(CommandSetting.ShowIfRequiresTrue))
                            {
                                if (def.m_Args[i].Requires.Invoke()) bestCmd = def.m_Args[i];
                            }
                            else bestCmd = def.m_Args[i];
                        }
                        else bestCmd = def.m_Args[i];
                    }
                    else if (def.m_Args[i].m_Field.StartsWith(split[1]))
                    {
                        if (def.m_Args[i].Requires != null)
                        {
                            if (def.m_Args[i].m_Settings.HasFlag(CommandSetting.ShowIfRequiresTrue))
                            {
                                if (def.m_Args[i].Requires.Invoke()) possibleList.Add(def.m_Args[i]);
                            }
                            else possibleList.Add(def.m_Args[i]);
                        }
                        else possibleList.Add(def.m_Args[i]);
                    }
                }
                return bestCmd;
            }

            CommandField nextCmd = def.Find(split[1]);
            for (int i = 2; i < split.Length - 1; i++)
            {
                nextCmd = nextCmd.Find(split[i]);
            }

            for (int i = 0; i < nextCmd.m_Args.Count; i++)
            {
                if (nextCmd.m_Args[i] == null)
                {
                    nextCmd.m_Args.RemoveAt(i);
                    i--;
                    continue;
                }

                if (nextCmd.m_Args[i].m_Field.Equals(split[split.Length - 1]))
                {
                    if (nextCmd.m_Args[i].Requires != null)
                    {
                        if (nextCmd.m_Args[i].m_Settings.HasFlag(CommandSetting.ShowIfRequiresTrue))
                        {
                            if (nextCmd.m_Args[i].Requires.Invoke()) bestCmd = nextCmd.m_Args[i];
                        }
                        else bestCmd = nextCmd.m_Args[i];
                    }
                    else bestCmd = nextCmd.m_Args[i];
                }
                else if (nextCmd.m_Args[i].m_Field.StartsWith(split[split.Length - 1]))
                {
                    if (nextCmd.m_Args[i].Requires != null)
                    {
                        if (nextCmd.m_Args[i].m_Settings.HasFlag(CommandSetting.ShowIfRequiresTrue))
                        {
                            if (nextCmd.m_Args[i].Requires.Invoke()) possibleList.Add(nextCmd.m_Args[i]);
                        }
                        else possibleList.Add(nextCmd.m_Args[i]);
                    }
                    else possibleList.Add(nextCmd.m_Args[i]);
                }
            }

            return bestCmd == null ? nextCmd : bestCmd;
        }

        private CommandDefinition FindDefinition(string name)
        {
            for (int i = 0; i < SyadeuSettings.Instance.m_CommandDefinitions.Count; i++)
            {
                if (SyadeuSettings.Instance.m_CommandDefinitions[i] == null)
                {
                    SyadeuSettings.Instance.m_CommandDefinitions.RemoveAt(i);
                    i--;
                    continue;
                }

                if (SyadeuSettings.Instance.m_CommandDefinitions[i].m_Initializer.Equals(name))
                {
                    return SyadeuSettings.Instance.m_CommandDefinitions[i];
                }
            }
            return null;
        }
        private void InternalAddCommand(Action<string> action, params string[] lines)
        {
            CommandDefinition def = FindDefinition(lines[0]);
            if (def == null) throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                $"{lines[0]}의 명령어 시작 구문이 없거나 추가되지 않았습니다");

            if (lines.Length < 2)
            {
                def.Connected = true;
                def.Action = action;
                return;
            }

            CommandField nextCmd = def.Find(lines[1]);
            for (int i = 2; i < lines.Length; i++)
            {
                nextCmd = nextCmd.Find(lines[i]);
            }

            nextCmd.Connected = true;
            nextCmd.Action = action;
        }
        private void InternalAddCommand(Action<string> action, CommandRequires requires, params string[] lines)
        {
            CommandDefinition def = FindDefinition(lines[0]);
            if (def == null) throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                $"{lines[0]}의 명령어 시작 구문이 없거나 추가되지 않았습니다");

            if (lines.Length < 2)
            {
                if (def.m_Settings == CommandSetting.None)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                        $"명령어 {def.m_Initializer}는 아무 세팅도 없으므로 요구 조건이 필요없는데 조건을 추가하려합니다");
                }

                def.Connected = true;
                def.Action = action;
                def.Requires = requires;
                return;
            }

            CommandField nextCmd = def.Find(lines[1]);
            for (int i = 2; i < lines.Length; i++)
            {
                nextCmd = nextCmd.Find(lines[i]);
            }

            if (nextCmd.m_Settings == CommandSetting.None)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                        $"명령어 {def.m_Initializer}는 아무 세팅도 없으므로 요구 조건이 필요없는데 조건을 추가하려합니다");
            }

            nextCmd.Connected = true;
            nextCmd.Action = action;
            nextCmd.Requires = requires;
        }
        private void InternalAddCommand(Action<string> action, Type scriptType, string methodName, object expectValue, params string[] lines)
        {
            CommandDefinition def = FindDefinition(lines[0]);
            if (def == null) throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                $"{lines[0]}의 명령어 시작 구문이 없거나 추가되지 않았습니다");

            CommandRequires requires = new CommandRequires(scriptType.GetMethod(methodName), expectValue);

            if (lines.Length < 2)
            {
                if (def.m_Settings == CommandSetting.None)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                        $"명령어 {def.m_Initializer}는 아무 세팅도 없으므로 요구 조건이 필요없는데 조건을 추가하려합니다");
                }

                def.Connected = true;
                def.Action = action;
                def.Requires = requires;
                return;
            }

            CommandField nextCmd = def.Find(lines[1]);
            for (int i = 2; i < lines.Length; i++)
            {
                nextCmd = nextCmd.Find(lines[i]);
            }

            if (nextCmd.m_Settings == CommandSetting.None)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                        $"명령어 {def.m_Initializer}는 아무 세팅도 없으므로 요구 조건이 필요없는데 조건을 추가하려합니다");
            }

            nextCmd.Connected = true;
            nextCmd.Action = action;
            nextCmd.Requires = requires;
        }
        private void InternalAddCommand(Action<string> action, Type scriptType, string methodName, CommandRequiresDelegate predictate, params string[] lines)
        {
            CommandDefinition def = FindDefinition(lines[0]);
            if (def == null) throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                $"{lines[0]}의 명령어 시작 구문이 없거나 추가되지 않았습니다");

            CommandRequires requires = new CommandRequires(scriptType.GetMethod(methodName), predictate);

            if (lines.Length < 2)
            {
                if (def.m_Settings == CommandSetting.None)
                {
                    throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                        $"명령어 {def.m_Initializer}는 아무 세팅도 없으므로 요구 조건이 필요없는데 조건을 추가하려합니다");
                }

                def.Connected = true;
                def.Action = action;
                def.Requires = requires;
                return;
            }

            CommandField nextCmd = def.Find(lines[1]);
            for (int i = 2; i < lines.Length; i++)
            {
                nextCmd = nextCmd.Find(lines[i]);
            }

            if (nextCmd.m_Settings == CommandSetting.None)
            {
                throw new CoreSystemException(CoreSystemExceptionFlag.Console,
                        $"명령어 {def.m_Initializer}는 아무 세팅도 없으므로 요구 조건이 필요없는데 조건을 추가하려합니다");
            }

            nextCmd.Connected = true;
            nextCmd.Action = action;
            nextCmd.Requires = requires;
        }
        private void InternalCreateCommand(Action<string> action, params string[] lines)
        {
            CommandDefinition def = ScriptableObject.CreateInstance<CommandDefinition>();
            def.m_Initializer = lines[0];

            SyadeuSettings.Instance.m_CommandDefinitions.Add(def);

            if (lines.Length < 2)
            {
                def.Action = action;
                return;
            }

            CommandField nextCmd = def.Find(lines[1]);
            if (nextCmd == null)
            {
                nextCmd = ScriptableObject.CreateInstance<CommandField>();
                nextCmd.m_Field = lines[1];

                def.m_Args.Add(nextCmd);
            }
            for (int i = 2; i < lines.Length; i++)
            {
                CommandField findNext = nextCmd.Find(lines[i]);
                if (findNext == null)
                {
                    findNext = ScriptableObject.CreateInstance<CommandField>();
                    findNext.m_Field = lines[i];

                    nextCmd.m_Args.Add(findNext);
                }

                nextCmd = findNext;
            }

            nextCmd.Connected = true;
            nextCmd.Action = action;
        }

        #endregion
    }
}
