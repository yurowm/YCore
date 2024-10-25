// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Yurowm/Blur UI (Downgraded)" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Tint Color (RGB)", 2D) = "white" {}
    }
 
    Category {
 
        Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Opaque"
		}
 
        SubShader {
         
            // Distortion
            GrabPass {                        
                Tags {
					"LightMode" = "Always"
				}
            }
            Pass {
                Tags {
					"LightMode" = "Always"
				}
				
				Blend SrcAlpha OneMinusSrcAlpha
                
				CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag 
					#pragma fragmentoption ARB_precision_hint_fastest 
					#include "UnityCG.cginc"
             
					struct appdata_t {
						float4 vertex : POSITION;
						float2 texcoord: TEXCOORD0;
						float4 color    : COLOR;
					};
             
					struct v2f {
						float4 vertex : POSITION;
						float2 uvmain : TEXCOORD2;
						float4 color : COLOR;
					};
             
					float4 _MainTex_ST;
					sampler2D _MainTex;
					fixed4 _Color;
             
					v2f vert (appdata_t v) {
						v2f o;
						o.vertex = UnityObjectToClipPos(v.vertex);
						o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );
						o.color = v.color * _Color;
						return o;
					}
             
					half4 frag( v2f i ) : COLOR {
						return tex2D( _MainTex, i.uvmain ) * i.color;
					}
                ENDCG
            }
        }
    }
}