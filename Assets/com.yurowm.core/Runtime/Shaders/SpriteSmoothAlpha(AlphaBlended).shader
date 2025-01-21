Shader "YProject/Sprite-SmoothAlpha (Alpha-Blended)" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _Power ("Power", Range(1, 5)) = 2
    }
    SubShader {
        Tags { 
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
             }
        LOD 100

        Cull Off
        Lighting Off
        ZWrite Off
        Fog{ Mode Off }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _Power;
            fixed4 _TintColor;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _TintColor;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            float Sample(float2 uv) {
                return tex2D(_MainTex, uv).a;
            }
            
            float Sample(float2 uv, float2 offset) {
                return Sample(uv + offset * _Power * _MainTex_TexelSize.xy);
            }
            
            float GetSmoothAlpha(float2 uv) {
                float result = Sample(uv);
                result += Sample(uv, float2(1, 0));
                result += Sample(uv, float2(1, 1));
                result += Sample(uv, float2(0, 1));
                result += Sample(uv, float2(-1, 1));
                result += Sample(uv, float2(-1, 0));
                result += Sample(uv, float2(-1, -1));
                result += Sample(uv, float2(0, -1));
                result += Sample(uv, float2(1, -1));
                
                return result / 9;
            }
            
            

            fixed4 frag (v2f i) : SV_Target {
                float4 result = tex2D(_MainTex, i.uv);
        
                result.a = GetSmoothAlpha(i.uv);
            
                return result * 2.0f * i.color;
            }
            ENDCG
        }
    }
}

