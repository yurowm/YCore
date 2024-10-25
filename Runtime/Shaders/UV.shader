Shader "YProject/UV" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _ChessboardSize("Chessboard Size", Float) = 5
        }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _ChessboardSize;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            bool Chessboard(float x, float size) {
                x *= size;
                x -= floor(x);
                return x > 0.5;
            }

            bool Chessboard(float2 uv, float size) {
                return Chessboard(uv.x, size) == Chessboard(uv.y, size);
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 result = fixed4(i.uv.x, i.uv.y, 0, 1);
                
                if (Chessboard(i.uv, _ChessboardSize))
                    result.rgb = 1 - result.rgb;
            
                return result;
            }
            ENDCG
        }
    }
}

