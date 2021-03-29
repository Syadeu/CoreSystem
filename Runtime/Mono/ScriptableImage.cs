using System;
using UnityEngine;
using UnityEngine.UI;

namespace Syadeu.Mono
{
    [Obsolete("작업 중")]
    public sealed class ScriptableImage : Image
    {
        private static Material m_DefaultMaterial;
        private static Material DefaultMaterial
        {
            get
            {
                if (m_DefaultMaterial == null)
                {

                }
                return m_DefaultMaterial;
            }
        }
        private struct ProceduralImageInfo
        {
            public float width;
            public float height;
            public float fallOffDistance;
            public Vector4 radius;
            public float borderWidth;
            public float pixelSize;

            public ProceduralImageInfo(float width, float height, float fallOffDistance, float pixelSize, Vector4 radius, float borderWidth)
            {
                this.width = Mathf.Abs(width);
                this.height = Mathf.Abs(height);
                this.fallOffDistance = Mathf.Max(0, fallOffDistance);
                this.radius = radius;
                this.borderWidth = Mathf.Max(borderWidth, 0);
                this.pixelSize = Mathf.Max(0, pixelSize);
            }
        }

        [SerializeField] private float borderWidth;
        [SerializeField] private float falloffDistance = 1;
        [SerializeField] private Vector4 radius;

        protected override void OnEnable()
        {
            base.OnEnable();
            FixTexCoordsInCanvas();
        }
        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            base.OnPopulateMesh(toFill);
            EncodeAllInfoIntoVertices(toFill, CalculateInfo());
        }
        protected override void OnTransformParentChanged()
        {
            base.OnTransformParentChanged();
            FixTexCoordsInCanvas();
        }

        private void FixTexCoordsInCanvas()
        {
            Canvas c = this.GetComponentInParent<Canvas>();
            if (c != null)
            {
                c.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1 | AdditionalCanvasShaderChannels.TexCoord2 | AdditionalCanvasShaderChannels.TexCoord3;
            }
        }
        private ProceduralImageInfo CalculateInfo()
        {
            var r = GetPixelAdjustedRect();
            float pixelSize = 1f / Mathf.Max(0, falloffDistance);

            Vector4 radius = FixRadius(this, this.radius);

            float minside = Mathf.Min(r.width, r.height);

            ProceduralImageInfo info = new ProceduralImageInfo(r.width + falloffDistance, r.height + falloffDistance, falloffDistance, pixelSize, radius / minside, borderWidth / minside * 2);

            return info;
        }
        private void EncodeAllInfoIntoVertices(VertexHelper vh, ProceduralImageInfo info)
        {
            UIVertex vert = new UIVertex();

            Vector2 uv1 = new Vector2(info.width, info.height);
            Vector2 uv2 = new Vector2(EncodeFloats_0_1_16_16(info.radius.x, info.radius.y), EncodeFloats_0_1_16_16(info.radius.z, info.radius.w));
            Vector2 uv3 = new Vector2(info.borderWidth == 0 ? 1 : Mathf.Clamp01(info.borderWidth), info.pixelSize);

            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref vert, i);

                vert.position += ((Vector3)vert.uv0 - new Vector3(0.5f, 0.5f)) * info.fallOffDistance;
                //vert.uv0 = vert.uv0;
                vert.uv1 = uv1;
                vert.uv2 = uv2;
                vert.uv3 = uv3;

                vh.SetUIVertex(vert, i);
            }
        }

        /// <summary>
        /// Encode two values between [0,1] into a single float. Each using 16 bits.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static float EncodeFloats_0_1_16_16(float a, float b)
        {
            Vector2 kDecodeDot = new Vector2(1.0f, 1f / 65535.0f);
            return Vector2.Dot(new Vector2(Mathf.Floor(a * 65534) / 65535f, Mathf.Floor(b * 65534) / 65535f), kDecodeDot);
        }
        /// <summary>
        /// Prevents radius to get bigger than rect size
        /// </summary>
        /// <returns>The fixed radius.</returns>
        /// <param name="vec">border-radius as Vector4 (starting upper-left, clockwise)</param>
        private static Vector4 FixRadius(ScriptableImage img, Vector4 vec)
        {
            Rect r = img.rectTransform.rect;
            vec = new Vector4(Mathf.Max(vec.x, 0), Mathf.Max(vec.y, 0), Mathf.Max(vec.z, 0), Mathf.Max(vec.w, 0));

            //Allocates mem
            //float scaleFactor = Mathf.Min(r.width / (vec.x + vec.y), r.width / (vec.z + vec.w), r.height / (vec.x + vec.w), r.height / (vec.z + vec.y), 1);
            //Allocation free:
            float scaleFactor = Mathf.Min(Mathf.Min(Mathf.Min(Mathf.Min(r.width / (vec.x + vec.y), r.width / (vec.z + vec.w)), r.height / (vec.x + vec.w)), r.height / (vec.z + vec.y)), 1f);
            return vec * scaleFactor;
        }
    }
}
