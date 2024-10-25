Shader "Yurowm/AddativeNoised" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Noise ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        [WrapModeProperty] _WrapMode ("WrapMode", Int) = -1
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
        Blend One One

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Yurowm.cginc"

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
				float2 projPos : TEXCOORD2;
            };

            sampler2D _MainTex;
            sampler2D _Noise;
            float4 _MainTex_ST;
            float4 _Noise_ST;
            fixed4 _TintColor;
            int _WrapMode;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _TintColor;
                o.projPos = TRANSFORM_TEX(ComputeScreenPos (o.vertex), _Noise);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 Sample(v2f i) {
                return tex2D(_MainTex, Wrap(i.uv, _WrapMode));
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 result = Sample(i) * i.color;

                fixed noise = tex2D(_Noise, i.projPos + float2(cos(_Time.x / 100), sin(_Time.x / 70)) * 300).a;
                noise += tex2D(_Noise, i.projPos + float2(cos(-_Time.x / 230), sin(_Time.x / 130)) * 200).a;
                noise += tex2D(_Noise, i.projPos + float2(sin(_Time.x / 50), cos(-_Time.x / 200)) * 120).a;
                result.a += (noise - 1) * 2 * result.a;
            
                return result * result.a;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
