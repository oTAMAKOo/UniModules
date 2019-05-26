
Shader "Custom/Sprites/ColorFilterOverlay"
{
	Properties
	{
		[HideInInspector] _MainTex("Texture", 2D) = "" {}
		_Color("Blend Color", Color) = (0.5, 0.5, 0.5, 1.0)
		_SubColor("Blend Color", Color) = (0.5, 0.5, 0.5, 1.0)
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent" 
			"IgnoreProjector" = "True" 
			"RenderType" = "Transparent" 
		}

		ZWrite Off
		Lighting Off
		Cull Off
		Fog{ Mode Off }

		Blend SrcColor OneMinusSrcAlpha
		
		Pass
		{
			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma fragmentoption ARB_precision_hint_fastest

			#include "UnityCG.cginc"

			struct appdata_custom
			{
				float4 vertex : POSITION;
				fixed2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : POSITION;
				fixed2 uv : TEXCOORD0;
			};

			sampler2D _MainTex;
			fixed4 _MainTex_ST;
			fixed4 _Color;

			v2f vert(appdata_custom v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv,_MainTex);
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				fixed4 diffuse = tex2D(_MainTex, i.uv);

				diffuse = diffuse < 0.5 ? 2.0 * diffuse * _Color : 1.0 - 2.0 * (1.0 - diffuse) * (1.0 - _Color);
				diffuse.a = _Color.a;
	
				return diffuse;
			}

			ENDCG
		}

		Pass
		{
			Blend DstColor OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			fixed4 _SubColor;

			fixed getAddValue(fixed value) 
			{
				fixed v = value;

				if (v > 0.5) 
				{ 
					v = 0.5; 
				}

				return 2 * v;
			}

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex; float4 _MainTex_ST;

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);

				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 texCol = tex2D(_MainTex, i.uv);

				fixed4 col = fixed4(getAddValue(texCol.r),getAddValue(texCol.g),getAddValue(texCol.b),getAddValue(texCol.a));

				return col * _SubColor;
			}

			ENDCG
		}
	}
	
	Fallback off
}
