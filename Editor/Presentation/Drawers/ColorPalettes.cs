﻿using UnityEngine;

namespace SyadeuEditor.Presentation
{
    public static class ColorPalettes
    {
        public struct PastelDreams
        {
            public static readonly Color32 HotPink = new Color32(255, 174, 188, 255);
            public static readonly Color32 TiffanyBlue = new Color32(160, 231, 229, 255);
            public static readonly Color32 Mint = new Color32(180, 248, 200, 255);
            public static readonly Color32 Yellow = new Color32(251, 231, 198, 255);
        }
        public struct WaterFoam
        {
            public static readonly Color32 Teal = new Color32(41, 160, 177, 255);
            public static readonly Color32 TealGreen = new Color32(22, 125, 127, 255);
            public static readonly Color32 Spearmint = new Color32(152, 215, 194, 255);
            public static readonly Color32 Mint = new Color32(221, 255, 231, 255);
        }

        public struct TriadicColor
        {
            public static readonly Color32 One = new Color32(255, 231, 221, 255);
            public static readonly Color32 Two = new Color32(221, 255, 231, 255);
            public static readonly Color32 Three = new Color32(231, 221, 255, 255);
            public static readonly Color32 Four = new Color32(255, 221, 245, 255);
            public static readonly Color32 Five = new Color32(221, 255, 248, 255);
        }

        public static void SetBackgroundColor(GUIStyle style, Color normal, Color hover, Color press)
        {
            var BttNormalTex = new Texture2D(1, 1);
            BttNormalTex.SetPixel(0, 0, normal);
            BttNormalTex.Apply();
            var BttHoverTex = new Texture2D(1, 1);
            BttHoverTex.SetPixel(0, 0, hover);
            BttHoverTex.Apply();
            var BttPressTex = new Texture2D(1, 1);
            BttPressTex.SetPixel(0, 0, press);
            BttPressTex.Apply();

            style.normal.background = BttNormalTex;
            style.hover.background = BttHoverTex;
            style.active.background = BttPressTex;
        }
    }
}