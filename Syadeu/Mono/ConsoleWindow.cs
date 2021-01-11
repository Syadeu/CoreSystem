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

        private Coroutine WindowCoroutine { get; set; }

        [RuntimeInitializeOnLoadMethod]
        private static void OnGameStart()
        {
            Instance.Initialize();
        }
        public override void OnInitialize()
        {
            KeySetting();
        }
        private void KeySetting()
        {
#if INPUTSYSTEM
            if (Keyboard.current.backquoteKey.isPressed)
#else
            if (Input.GetKeyDown(KeyCode.BackQuote))
#endif
            {
                Opened = !Opened;
            }

            if (WindowCoroutine == null) WindowCoroutine = StartCoroutine(WindowUpdate());
            else
            {
                StopCoroutine(WindowCoroutine);
                WindowCoroutine = null;
            }
        }

        private IEnumerator WindowUpdate()
        {
            while (true)
            {

                yield return null;
            }
        }

        string consoleLog = "";
        Rect consoleRect = new Rect(0, 0, Screen.width * 0.999f, Screen.height * 0.5f);
        Rect possibleRect;
        Rect ConsoleTextPos;
        Vector2 scroll = new Vector2(0, 0);

        private void OnGUI()
        {
            if (Opened)
            {
                consoleRect = GUI.Window(0, consoleRect, Console, "", "Box");
            }
        }
        private void Console(int id)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            GUILayout.TextArea(consoleLog, "textarea");
            GUILayout.EndScrollView();
        }
    }
}
