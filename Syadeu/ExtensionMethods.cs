using System;
using UnityEngine;

namespace Syadeu.Extentions.EditorUtils
{
    public static class EditorUtilExtensions
    {
        public static void ToLogError(this string log, bool overrideLog = false)
        {
#if UNITY_EDITOR
            Debug.LogError(log);
#endif
        }
        public static void ToLog(this string log, bool overrideLog = false)
        {
#if UNITY_EDITOR
            Debug.Log(log);
#endif
        }
    }
}

namespace Syadeu
{
    public static class ExtensionMethods
    {
        public static T Clamp<T>(this T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }
        public static void SaveTextureAsPNG(this Texture2D _texture, string _fullPath)
        {
            byte[] _bytes = _texture.EncodeToPNG();
            System.IO.File.WriteAllBytes(_fullPath, _bytes);
            Debug.Log(_bytes.Length / 1024 + "Kb was saved as: " + _fullPath);
        }
    }
}