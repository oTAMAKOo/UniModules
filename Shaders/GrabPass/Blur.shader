
Shader "Custom/GrabPass/Blur" 
{
    Properties 
	{
        _Distortion ("Distortion", Range(0, 25)) = 1

		[HideInInspector] 
        _MainTex ("Tint Color (RGB)", 2D) = "white" {}
    }
     
    Category 
	{
		Tags 
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Opaque" 
		}
          
        SubShader 
		{
         
            // Horizontal blur
            GrabPass 
			{                    
                Tags { "LightMode" = "Always" }
            }

            Pass 
			{
                Tags { "LightMode" = "Always" }
                 
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                 
                struct appdata_t 
				{
                    float4 vertex : POSITION;
                    float2 texcoord: TEXCOORD0;
                };
                 
                struct v2f 
				{
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
                };
                 
                v2f vert (appdata_t v) 
				{
                    #if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
                    #else
                    float scale = 1.0;
                    #endif

					v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;

                    return o;
                }
                 
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Distortion;
                 
                half4 frag( v2f i ) : COLOR 
				{
				    half4 sum = half4(0,0,0,0);

                    #define GRABPIXEL(weight,kernelx) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x + _GrabTexture_TexelSize.x * kernelx * _Distortion, i.uvgrab.y, i.uvgrab.z, i.uvgrab.w))) * weight

                    sum += GRABPIXEL(0.05, -4.0);
                    sum += GRABPIXEL(0.09, -3.0);
                    sum += GRABPIXEL(0.12, -2.0);
                    sum += GRABPIXEL(0.15, -1.0);
                    sum += GRABPIXEL(0.18,  0.0);
                    sum += GRABPIXEL(0.15, +1.0);
                    sum += GRABPIXEL(0.12, +2.0);
                    sum += GRABPIXEL(0.09, +3.0);
                    sum += GRABPIXEL(0.05, +4.0);
                     
                    return sum;
                }
                ENDCG
            }

            // Vertical blur
            GrabPass 
			{                        
                Tags { "LightMode" = "Always" }
            }

            Pass 
			{
                Tags { "LightMode" = "Always" }
                 
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                 
                struct appdata_t 
				{
                    float4 vertex : POSITION;
                    float2 texcoord: TEXCOORD0;
                };
                 
                struct v2f 
				{
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
                };
                 
                v2f vert (appdata_t v) 
				{
                    #if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
                    #else
                    float scale = 1.0;
                    #endif

					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;

                    return o;
                }
                 
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Distortion;
                 
                half4 frag( v2f i ) : COLOR 
				{                     
                    half4 sum = half4(0,0,0,0);

                    #define GRABPIXEL(weight,kernely) tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(float4(i.uvgrab.x, i.uvgrab.y + _GrabTexture_TexelSize.y * kernely * _Distortion, i.uvgrab.z, i.uvgrab.w))) * weight
                     
                    sum += GRABPIXEL(0.05, -4.0);
                    sum += GRABPIXEL(0.09, -3.0);
                    sum += GRABPIXEL(0.12, -2.0);
                    sum += GRABPIXEL(0.15, -1.0);
                    sum += GRABPIXEL(0.18,  0.0);
                    sum += GRABPIXEL(0.15, +1.0);
                    sum += GRABPIXEL(0.12, +2.0);
                    sum += GRABPIXEL(0.09, +3.0);
                    sum += GRABPIXEL(0.05, +4.0);
                     
                    return sum;
                }
                ENDCG
            }
             
            // Distortion
            GrabPass 
			{                        
                Tags { "LightMode" = "Always" }
            }

            Pass 
			{
                Tags { "LightMode" = "Always" }
                 
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                 
                struct appdata_t 
				{
                    float4 vertex	: POSITION;
                    float2 texcoord	: TEXCOORD0;
					float4 color	: COLOR;
                };
                 
                struct v2f 
				{
                    float4 vertex : POSITION;
                    float4 uvgrab : TEXCOORD0;
                    float2 uvmain : TEXCOORD2;
					float4 color  : COLOR;
                };
                 
                float4 _MainTex_ST;
                 
                v2f vert (appdata_t v) 
				{
                    #if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
                    #else
                    float scale = 1.0;
                    #endif

					v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y*scale) + o.vertex.w) * 0.5;
                    o.uvgrab.zw = o.vertex.zw;
                    o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex);
					o.color = v.color;

                    return o;
                }
                 
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                sampler2D _MainTex;
                 
                half4 frag( v2f i ) : COLOR 
				{
                    i.uvgrab.xy = _GrabTexture_TexelSize.xy * i.uvgrab.z + i.uvgrab.xy;
                     
                    half4 col = tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(i.uvgrab));
					half4 tint = tex2D( _MainTex, i.uvmain) * i.color;

					float ratio = 1 - 0.5 * tint.a;

					col.r = (col.r + tint.r * tint.a) * ratio;
					col.g = (col.g + tint.g * tint.a) * ratio;
					col.b = (col.b + tint.b * tint.a) * ratio;

                    return col;
                }
                ENDCG
            }
        }
    }
}