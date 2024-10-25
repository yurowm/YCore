Shader "Yurowm/Addative" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _TintColor;
            int _WrapMode;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color * _TintColor;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 Sample(v2f i) {
                return tex2D(_MainTex, Wrap(i.uv, _WrapMode));
            }
            
            fixed4 frag (v2f i) : SV_Target {
                fixed4 result = Sample(i) * i.color;
            
                return result  * result.a;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
