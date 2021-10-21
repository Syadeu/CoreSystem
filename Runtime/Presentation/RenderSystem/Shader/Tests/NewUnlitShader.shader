Shader "Unlit/NewUnlitShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MainTex2 ("Texture2", 2D) = "red" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGINCLUDE
        struct appdata
        {
            // POSITION == vertex position ? object space (local trs)
            float4 vertex : POSITION; // vertex input
            float2 uv : TEXCOORD0; // texture coordinate input
        };
        ENDCG

        Pass
        {
            Name "ExamplePassName"
            //Tags { "LightMode" = "ExampleLightModeTagValue" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                // SV_POSITION == clip space
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // object's vertex position handler?
            v2f vert (appdata v)
            {
                v2f o;
                // appdata.vertex == object space (local trs)
                //v.vertex.y +=1;
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);

                //o.vertex.y += 1;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // SV_Target == render target ?
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
        // per pass per one object ?
        Pass
        {
            Name "ExamplePassName2"
            //Tags { "LightMode" = "ExampleLightModeTagValue" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "../Includes/FractalNoise.cginc"
            #include "../Includes/Math.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                // SV_POSITION == clip space
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex2;
            float4 _MainTex2_ST;
            static const NumberGenerator _NumberGenerator;
            // static (nor const) value // initialize just once => like c# static variable.
            //static const float4 _UpperDirection = float4(0, 5, 0, 0);

            // object's vertex position handler?
            v2f vert (appdata v)
            {
                // in local, doesn't need to static
                const float4 _UpperDirection = float4(0, 5, 0, 0);

                v2f o;
                // appdata.vertex == object space (local trs)
                //v.vertex.y +=1;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.vertex.x += 1;
                o.vertex.y += 1;
                //o.vertex.y +=  _NumberGenerator.GetRandomFloat(-1, 1);
                o.vertex.z += _NumberGenerator.GetRandomFloat(-1, 1);
                //o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
                //o.vertex = simpleNoise(o.vertex, .1, 0);
                

                //o.vertex += _UpperDirection;
                float3 temp = float3(o.vertex.xyz);
                //float3 result = simpleNoise(temp, 1, 1);
                o.vertex.xyz = temp;

                o.uv = TRANSFORM_TEX(v.uv, _MainTex2);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            // SV_Target == render target ?
            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex2, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }

            
            ENDCG
        }
    }
}
