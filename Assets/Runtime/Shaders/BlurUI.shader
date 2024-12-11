Shader "Yurowm/Blur UI" {
    Properties {
	    _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Tint Color (RGB)", 2D) = "white" {}
        _Size ("Size", Float) = 1
		[MaterialToggle] _AlphaSize ("Alpha Size", Float) = 0
    	
    	_StencilComp ("Stencil Comparison", Float) = 8
		_Stencil ("Stencil ID", Float) = 0
		_StencilOp ("Stencil Operation", Float) = 0
		_StencilWriteMask ("Stencil Write Mask", Float) = 255
		_StencilReadMask ("Stencil Read Mask", Float) = 255
		_ColorMask ("Color Mask", Float) = 15
    }
 
    Category {
 
        Tags {
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"DisableBatching"="True"
		}
 		Cull Off
		Lighting Off
		ZWrite Off
		Fog { Mode Off }
		
        SubShader {
     
            // Horizontal blur 1 pass
            GrabPass {                    
                Tags {
					"LightMode" = "Always"
				}
            }
            Pass {
                Tags {
					"LightMode" = "Always"
				}
             
                Stencil {
                    Ref [_Stencil]
                    Comp [_StencilComp]
                    Pass [_StencilOp]
                    ReadMask [_StencilReadMask]
                    WriteMask [_StencilWriteMask]
                }
            	
                CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma fragmentoption ARB_precision_hint_fastest
					#include "UnityCG.cginc"
             
					struct appdata_t {
						float4 vertex : POSITION;
						float2 texcoord: TEXCOORD0;
						float4 color : COLOR;
					};
             
					struct v2f {
						float4 vertex : POSITION;
						float4 uvgrab : TEXCOORD0;
						float4 color : COLOR;
						float2 uvmain : TEXCOORD2;
					};
             
					sampler2D _GrabTexture;
					float4 _GrabTexture_TexelSize;
					float _Size;
					float _AlphaSize;
					float4 _Color;
					sampler2D _MainTex;
					float4 _MainTex_ST;

					v2f vert (appdata_t v) {
						v2f o;
						o.vertex = UnityObjectToClipPos(v.vertex);
						#if UNITY_UV_STARTS_AT_TOP
						float scale = -1.0;
						#else
						float scale = 1.0;
						#endif
						o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
						o.uvgrab.zw = o.vertex.zw;
						o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );
						o.color = v.color;
						return o;
					}
                          
					half4 frag( v2f i ) : COLOR {
						#define GRABPIXEL(weight,kernelx) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x + 0.01 * kernelx *_Size, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w))) * weight

						half4 sum = half4(0,0,0,0);
						half pow = tex2D( _MainTex, i.uvmain ).a;
					    if (_AlphaSize == 1) pow *= i.color.a;

						if (pow == 0) 
							sum = GRABPIXEL(1, 0);
						else {
							//sum += GRABPIXEL(0.05, -3.0 * pow);
							//sum += GRABPIXEL(0.1, -2.0 * pow);
							//sum += GRABPIXEL(0.2, -1.0 * pow);
							//sum += GRABPIXEL(0.3, 0.0);
							//sum += GRABPIXEL(0.2, +1.0 * pow);
							//sum += GRABPIXEL(0.1, +2.0 * pow);
							//sum += GRABPIXEL(0.05, +4.0 * pow);

							sum += GRABPIXEL(0.11, -2.0 * pow);
							sum += GRABPIXEL(0.22, -1.0 * pow);
							sum += GRABPIXEL(0.34, 0.0);
							sum += GRABPIXEL(0.22, +1.0 * pow);
							sum += GRABPIXEL(0.11, +2.0 * pow);
						}

						return sum;
					}
                ENDCG
            }

            // Vertical blur 1 pass
            GrabPass {                        
                Tags {
					"LightMode" = "Always"
				}
            }
            Pass {
                Tags { 
					"LightMode" = "Always"
				}
             
                Stencil {
                    Ref [_Stencil]
                    Comp [_StencilComp]
                    Pass [_StencilOp]
                    ReadMask [_StencilReadMask]
                    WriteMask [_StencilWriteMask]
                }
            	
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
             
                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord: TEXCOORD0;
					float4 color : COLOR;
                };
             
                struct v2f {
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
					float4 color : COLOR;
                    float2 uvmain : TEXCOORD2;
                };
             
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Size;
                float _AlphaSize;
				float4 _Color;
                sampler2D _MainTex;
                float4 _MainTex_ST;
             
                v2f vert (appdata_t v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    #if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
                    #else
                    float scale = 1.0;
                    #endif
                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;
                    o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );
					o.color = v.color;
                    return o;
                }

                half4 frag( v2f i ) : COLOR {
                    #define GRABPIXEL(weight,kernely) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x, i.uvgrab.y + 0.01 * kernely *_Size, i.uvgrab.z, i.uvgrab.w))) * weight

                    half4 sum = half4(0,0,0,0);
					float pow = tex2D( _MainTex, i.uvmain ).a;
                    if (_AlphaSize == 1) pow *= i.color.a;

					if (pow == 0) 
						sum = GRABPIXEL(1, 0);
					else {
						//sum += GRABPIXEL(0.05, -3.0 * pow);
						//sum += GRABPIXEL(0.1, -2.0 * pow);
						//sum += GRABPIXEL(0.2, -1.0 * pow);
						//sum += GRABPIXEL(0.3, 0.0);
						//sum += GRABPIXEL(0.2, +1.0 * pow);
						//sum += GRABPIXEL(0.1, +2.0 * pow);
						//sum += GRABPIXEL(0.05, +4.0 * pow);

						sum += GRABPIXEL(0.11, -2.0 * pow);
						sum += GRABPIXEL(0.22, -1.0 * pow);
						sum += GRABPIXEL(0.34, 0.0);
						sum += GRABPIXEL(0.22, +1.0 * pow);
						sum += GRABPIXEL(0.11, +2.0 * pow);

					}

                    return sum;
                }
                ENDCG
            }

			// Horizontal blur 2 pass
            GrabPass {                    
                Tags {
					"LightMode" = "Always"
				}
            }
            Pass {
                Tags {
					"LightMode" = "Always"
				}
            	
                Stencil {
                    Ref [_Stencil]
                    Comp [_StencilComp]
                    Pass [_StencilOp]
                    ReadMask [_StencilReadMask]
                    WriteMask [_StencilWriteMask]
                }
             
                CGPROGRAM
					#pragma vertex vert
					#pragma fragment frag
					#pragma fragmentoption ARB_precision_hint_fastest
					#include "UnityCG.cginc"
             
					struct appdata_t {
						float4 vertex : POSITION;
						float2 texcoord: TEXCOORD0;
						float4 color : COLOR;
					};
             
					struct v2f {
						float4 vertex : POSITION;
						float4 uvgrab : TEXCOORD0;
						float4 color : COLOR;
						float2 uvmain : TEXCOORD2;
					};
             
					sampler2D _GrabTexture;
					float4 _GrabTexture_TexelSize;
					float _Size;
					float _AlphaSize;
					float4 _Color;
					sampler2D _MainTex;
					float4 _MainTex_ST;

					v2f vert (appdata_t v) {
						v2f o;
						o.vertex = UnityObjectToClipPos(v.vertex);
						#if UNITY_UV_STARTS_AT_TOP
						float scale = -1.0;
						#else
						float scale = 1.0;
						#endif
						o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
						o.uvgrab.zw = o.vertex.zw;
						o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );
						o.color = v.color;
						return o;
					}
                          
					half4 frag( v2f i ) : COLOR {
						#define GRABPIXEL(weight,kernelx) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x + 0.01 * kernelx *_Size, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w))) * weight

						half4 sum = half4(0,0,0,0);
						half pow = tex2D( _MainTex, i.uvmain ).a;
					    if (_AlphaSize == 1) pow *= i.color.a;

						if (pow == 0) 
							sum = GRABPIXEL(1, 0);
						else {
							sum += GRABPIXEL(0.30, -0.5 * pow);
							sum += GRABPIXEL(0.40, 0.0);
							sum += GRABPIXEL(0.30, +0.5 * pow);
						}

						return sum;
					}
                ENDCG
            }

            // Vertical blur 2 pass
            GrabPass {                        
                Tags {
					"LightMode" = "Always"
				}
            }
            Pass {
                Tags { 
					"LightMode" = "Always"
				}
            	
                Stencil {
                    Ref [_Stencil]
                    Comp [_StencilComp]
                    Pass [_StencilOp]
                    ReadMask [_StencilReadMask]
                    WriteMask [_StencilWriteMask]
                }
            	
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
             
                struct appdata_t {
                    float4 vertex : POSITION;
                    float2 texcoord: TEXCOORD0;
					float4 color : COLOR;
                };
             
                struct v2f {
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
					float4 color : COLOR;
                    float2 uvmain : TEXCOORD2;
                };
             
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Size;
                float _AlphaSize;
				float4 _Color;
                sampler2D _MainTex;
                float4 _MainTex_ST;
             
                v2f vert (appdata_t v) {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    #if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
                    #else
                    float scale = 1.0;
                    #endif
                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;
                    o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );
					o.color = v.color;
                    return o;
                }

                half4 frag( v2f i ) : COLOR {
                    #define GRABPIXEL(weight,kernely) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x, i.uvgrab.y + 0.01 * kernely *_Size, i.uvgrab.z, i.uvgrab.w))) * weight

                    half4 sum = half4(0,0,0,0);
					float pow = tex2D( _MainTex, i.uvmain ).a;
                    if (_AlphaSize == 1) pow *= i.color.a;

					if (pow == 0) 
						sum = GRABPIXEL(1, 0);
					else {
						sum += GRABPIXEL(0.30, -0.5 * pow);
						sum += GRABPIXEL(0.40, 0.0);
						sum += GRABPIXEL(0.30, +0.5 * pow);
					}

                    return sum;
                }
                ENDCG
            }
         
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
            	
                Stencil {
                    Ref [_Stencil]
                    Comp [_StencilComp]
                    Pass [_StencilOp]
                    ReadMask [_StencilReadMask]
                    WriteMask [_StencilWriteMask]
                }
                
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
						float4 uvgrab : TEXCOORD0;
						float2 uvmain : TEXCOORD2;
						float4 color : COLOR;
					};
             
					float4 _MainTex_ST;
					sampler2D _GrabTexture;
					float4 _GrabTexture_TexelSize;
					sampler2D _MainTex;
					fixed4 _Color;
             
					v2f vert (appdata_t v) {
						v2f o;
						o.vertex = UnityObjectToClipPos(v.vertex);
						#if UNITY_UV_STARTS_AT_TOP
						float scale = -1.0;
						#else
						float scale = 1.0;
						#endif
						o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
						o.uvgrab.zw = o.vertex.zw;
						o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex );
						o.color = v.color * _Color;
						return o;
					}
             
					half4 frag( v2f i ) : COLOR {
						half4 tint = tex2D( _MainTex, i.uvmain ) * i.color;

						return tint;
					}
                ENDCG
            }
        }
    }
}