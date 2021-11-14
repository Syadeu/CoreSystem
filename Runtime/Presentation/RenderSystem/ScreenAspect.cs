using UnityEngine;

namespace Syadeu.Presentation.Render
{
    public readonly struct ScreenAspect
    {
        public readonly int
            WidthRatio, HeightRatio,
            Width, Heigth;

        public ScreenAspect(Resolution resolution)
        {
            Width = resolution.width;
            Heigth = resolution.height;

            WidthRatio = resolution.width / 80;
            HeightRatio = resolution.height / 80;
        }

        public bool Is16p9()
        {
            return WidthRatio == 16 && HeightRatio == 9;
        }
        public bool Is16p10()
        {
            return WidthRatio == 16 && HeightRatio == 10;
        }
    }
}
