Shader "Unlit/CoreSystemDecal"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            //#pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 object : TEXCOORD0;
                float4 screen : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;

            v2f vert (float4 vertex : POSITION)
            {
                v2f o;
                o.object = vertex;
                o.vertex = UnityObjectToClipPos(vertex);
                o.screen = ComputeScreenPos(o.vertex);
                return o;
            }
            float3 ReconstructWorldPos(float4 object, float4 screen)
            {
                float depth = LinearEyeDepth(tex2Dproj(_CameraDepthTexture, screen));
                float3 ray = WorldSpaceViewDir(object);
                ray /= dot(ray, normalize(UNITY_MATRIX_V[2].xyz));
                return _WorldSpaceCameraPos - ray * depth;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 world = ReconstructWorldPos(i.object, i.screen);
                float4 object = mul(unity_WorldToObject, float4(world, 1));
                float2 uv = object.xz + 0.5;

                clip((1 - uv) * uv);

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}
