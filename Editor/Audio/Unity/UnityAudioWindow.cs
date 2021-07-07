using Syadeu;
using SyadeuEditor.Test;
using System.Collections;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

#if CORESYSTEM_UNITYAUDIO

namespace SyadeuEditor.Audio.Unity
{
    public sealed class UnityAudioWindow : EditorWindow
    {
        private void OnEnable()
        {
            
        }
        private void OnGUI()
        {
            //string path = null;

            //string[] rootAudioPaths = Directory.GetFiles(path);

            if (GUILayout.Button("btt"))
            {
                EditorApplication.ExecuteMenuItem("Assets/Create/Audio Mixer");
                Keyboard.SendWithShift(Keyboard.ScanCodeShort.KEY_M);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_A);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_S);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_T);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_E);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_R);
                Keyboard.SendWithShift(Keyboard.ScanCodeShort.KEY_M);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_I);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_X);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_E);
                Keyboard.Send(Keyboard.ScanCodeShort.KEY_R);
                Keyboard.Send(Keyboard.ScanCodeShort.RETURN);
                Keyboard.Send(Keyboard.ScanCodeShort.RETURN);

                CoreSystem.AddEditorTask(task);
                
            }
            
        }

        private IEnumerator task(int progressID)
        {
            //CoreSystem.NotNull(Selection.activeObject, "selection is null");
            yield return new WaitUntil(() => Selection.activeObject != null);

            AudioMixer masterMixer = (AudioMixer)Selection.activeObject;
            CoreSystem.IsNotNull(masterMixer);
            CoreSystem.Log(Channel.Editor, $"{masterMixer.name}");
        }
    }
}
#endif