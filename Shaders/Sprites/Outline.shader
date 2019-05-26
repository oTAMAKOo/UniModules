
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Sprites/Outline"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		_OutLineSpread("OutLine Spread", Range(0.00, 1.00)) = 0.01
		_OutLineColor("Outline Color", Color) = (1, 1, 1, 1)

		[MaterialToggle] _ShadowOn("Shadow On", Float) = 0
		_ShadowOffsetX("Shadow Offset X", Float) = 0.02
		_ShadowOffsetY("Shadow Offset Y", Float) = -0.02
		_ShadowColor("Shadow Color", Color) = (0, 0, 0, 0.8)
		_Alpha("Alpha", Range(0.0, 1.0)) = 1
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
		Fog{ Mode Off }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile DUMMY PIXELSNAP_ON
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex	: SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			sampler2D _MainTex;
			half _OutLineSpread;
			fixed4 _OutLineColor;
			half _ShadowOffsetX;
			half _ShadowOffsetY;
			fixed4 _ShadowColor;
			half _Alpha;

			fixed _ShadowOn;

			v2f vert(appdata IN)
			{
				float2 tex = IN.texcoord;

				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = tex;
				OUT.color = IN.color;

				#ifdef PIXELSNAP_ON

				OUT.vertex = UnityPixelSnap(OUT.vertex);
				
				#endif

				return OUT;
			}

			sampler2D _AlphaTex;
			float _AlphaSplitEnabled;

			fixed4 SampleSpriteTexture(float2 uv)
			{
				fixed4 color = tex2D(_MainTex, uv);

				#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED

				if (_AlphaSplitEnabled)
				{
					color.a = tex2D(_AlphaTex, uv).r;
				}

				#endif

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				const fixed THRESHOLD = 0.1;

				// 元のテクスチャ
				fixed4 base = SampleSpriteTexture(IN.texcoord) * IN.color;

				// アウトライン色
				fixed4 out_col = _OutLineColor;
				_OutLineColor.a = 1;
				half2 line_w = half2(_OutLineSpread, 0);
				fixed4 line_col = SampleSpriteTexture(IN.texcoord + line_w.xy)
					+ SampleSpriteTexture(IN.texcoord - line_w.xy)
					+ SampleSpriteTexture(IN.texcoord + line_w.yx)
					+ SampleSpriteTexture(IN.texcoord - line_w.yx);

				_OutLineColor *= (line_col.a);
				_OutLineColor.rgb = out_col.rgb;
				_OutLineColor = lerp(base, _OutLineColor, max(0, sign(_OutLineSpread)));

				// 合成
				fixed4 main_col = base;
				main_col = lerp(main_col, _OutLineColor, (1 - main_col.a));
				main_col.a = _Alpha * max(0, sign(main_col.a - THRESHOLD));

				// 影
				if (_ShadowOn)
				{
					fixed4 shadow = SampleSpriteTexture(IN.texcoord - half2(_ShadowOffsetX, _ShadowOffsetY));
					shadow = _ShadowColor * max(0, sign(shadow.a - THRESHOLD));
					shadow.a *= _ShadowColor.a;
					_ShadowColor = shadow;

					// 合成.
					_ShadowColor.a = min(_Alpha, shadow.a);
					main_col = lerp(_ShadowColor, main_col, max(0, sign(main_col.a - THRESHOLD)));
				}

				return main_col;
			}
				
			ENDCG
		}
	}
}