    
Shader "Custom/AlphaChannel/PremultipliedAlpha" 
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_AlpColor ("AlpColor(RGBA)", Color) = (1,1,1,0.5)
		_AddColor ("AddColor(RGB)", Color) = (0.5,0.5,0.5,0.5)

		[MaterialToggle] 
        _PixelSnap ("Pixel snap", Float) = 0
        
        [Toggle(_STENCIL_MASK)]
        _Mask("Use Stencil Mask", Float) = 0

        // required for UI.Mask
		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255
		_ColorMask("Color Mask", Float) = 15
	}

	SubShader
	{
		Tags
		{ 
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent" 
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend One OneMinusSrcAlpha

        // required for UI.Mask
		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Pass
		{
		    CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON

			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				half2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _AlpColor;
			fixed4 _AddColor;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _AlpColor;
				OUT.color.rgb *= OUT.color.a;								//事前乗算
				OUT.color.rgb += IN.color.rgb * _AddColor.rgb * IN.color.a;	//加算成分追加

				#ifdef PIXELSNAP_ON

				OUT.vertex = UnityPixelSnap (OUT.vertex);

				#endif

				return OUT;
			}

			sampler2D _MainTex;

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 tex = tex2D(_MainTex, IN.texcoord);
				tex.rgb *= tex.a;
				return tex * IN.color;
			}

		    ENDCG
		}
	}
}
