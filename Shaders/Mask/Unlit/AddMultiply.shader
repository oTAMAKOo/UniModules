
Shader "Custom/Mask/Unlit/AddMultiply" 
{
	Properties
	{
		_TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
		_MainTex("Particle Texture", 2D) = "white" {}
		_InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
		_Contrast("Contrast Factor", Range(0.1,1.0)) = 0.1

		// required for UI.Mask
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
	}
		
	CGINCLUDE

	#pragma vertex vert
	#include "UnityCG.cginc"

	struct appdata_t
	{
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
	};

	struct v2f
	{
		float4 vertex : POSITION;
		fixed4 color : COLOR;
		float2 texcoord : TEXCOORD0;
	};

	float4 _MainTex_ST;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.color = v.color;
		o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

		return o;
	}

	sampler2D _CameraDepthTexture;
	float _InvFade;

	sampler2D _MainTex;
	fixed4 _TintColor;
	float _Contrast;


	float bias(float val, float b)
	{
		return (b > 0) ? pow(abs(val), log(b) / log(0.5)) : 0;
	}

	float gain(float val, float g)
	{
		return 0.5 * ((val < 0.5) ? bias(2.0*val, 1.0 - g) : (2.0 - bias(2.0 - 2.0*val, 1.0 - g)));
	}

	ENDCG

	Category
	{
		Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		
		Cull Off 
		Lighting Off 
		ZWrite Off 
		Fog { Mode Off }

		BindChannels
		{
			Bind "Color", color
			Bind "Vertex", vertex
			Bind "TexCoord", texcoord
		}

		// required for UI.Mask
		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		ColorMask[_ColorMask]

		SubShader
		{
			Pass 
			{
				Blend Zero SrcColor

				ColorMask RGBA
				
				CGPROGRAM

				#pragma fragment frag

				fixed4 frag(v2f i) : COLOR
				{
					half4 prev = i.color * tex2D(_MainTex, i.texcoord);

					prev.r = gain(prev.r , _Contrast);
					prev.g = gain(prev.g , _Contrast);
					prev.b = gain(prev.b , _Contrast);

					return lerp(half4(1,1,1,1), prev, prev.a);
				}

				ENDCG

				SetTexture[_MainTex] 
				{
					combine texture * primary
				}
					
				SetTexture[_MainTex] 
				{
					constantColor(1,1,1,1)
					combine previous lerp(previous) constant
				}
			}

			Pass
			{
				Blend SrcAlpha One
				
				AlphaTest Greater .01
				ColorMask RGBA

				CGPROGRAM

				#pragma fragment frag

				fixed4 frag(v2f i) : COLOR
				{
					fixed4 prev = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);

					prev.r = gain(prev.r , _Contrast);
					prev.g = gain(prev.g , _Contrast);
					prev.b = gain(prev.b , _Contrast);

					return prev;
				}

				ENDCG

				SetTexture[_MainTex] 
				{
					constantColor[_TintColor]
					combine constant * primary
				}
				
				SetTexture[_MainTex]
				{
					combine texture * previous DOUBLE
				}
			}
		}
	}
}
