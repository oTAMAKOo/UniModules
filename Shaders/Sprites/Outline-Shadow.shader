
Shader "Custom/Sprites/Outline-Shadow"
{
   Properties
   {
        [PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Color("Tint", Color) = (1,1,1,1)

        // Outline
       
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineSpread ("Outline Spread", Range(0.1, 5)) = 1


        // Shadow

        _ShadowColor ("Shadow Color", Color) = (0,0,0,1)		
        _ShadowOffset ("Shadow Offset", Vector) = (0,-0.1,0,0)

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
           "Queue"           = "Transparent"
           "IgnoreProjector" = "True"
           "RenderType"      = "Transparent"
           "PreviewType"     = "Plane"
       }

        // required for UI.Mask
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        ColorMask [_ColorMask]

        Lighting Off Cull Off ZTest Always ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

		// draw shadow

		Pass
		{
		    CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _PIXELSNAP_ON

			#include "UnityCG.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 uv       : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex        : SV_POSITION;
				fixed4 color         : COLOR;
				float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
			};
			
            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _ShadowColor;
			float4 _ShadowOffset;
            sampler2D _AlphaTex;
            float _AlphaSplitEnabled;

			v2f vert(appdata_t v)
			{
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex + _ShadowOffset);
                o.color = _ShadowColor;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPosition = v.vertex;

                o.color.a *= v.color.a;

                #ifdef PIXELSNAP_ON
				
                o.vertex = UnityPixelSnap (o.vertex);
				
                #endif

                return o;
			}

			fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D (_MainTex, uv);

				color.rgb = _ShadowColor.rgb;

				#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
				
                if (_AlphaSplitEnabled)
                {
					color.a = tex2D (_AlphaTex, uv).r;
                }

				#endif

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				fixed4 c = SampleSpriteTexture (IN.uv) * IN.color;
				c.rgb *= c.a;
				return c;
			}

		    ENDCG
        }

        // draw outline

        Pass 
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t 
            {
                float4 vertex : POSITION;
                half4  color  : COLOR;
                float2 uv     : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f 
            {
                float4 vertex        : SV_POSITION;
                half4  color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            half4  _OutlineColor;
            half   _OutlineSpread;

            v2f vert (appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
                o.worldPosition = v.vertex;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = i.color;

                half4 o_color = _OutlineColor;
                
                half4 lerp_color = _OutlineColor;

                half a0 = tex2D(_MainTex, i.uv).a;

                o_color.a *= color.a;

                lerp_color.a = 0;

                color = lerp(o_color, lerp_color, a0);

                float4 delta = float4(1, 1, 0, -1) * _MainTex_TexelSize.xyxy * _OutlineSpread;

                half a1 = max(max(tex2D(_MainTex, i.uv + delta.xz).a, tex2D(_MainTex, i.uv - delta.xz).a),
                              max(tex2D(_MainTex, i.uv + delta.zy).a, tex2D(_MainTex, i.uv - delta.zy).a));

                delta *= 0.7071;
               
                half a2 = max(max(tex2D(_MainTex, i.uv + delta.xy).a, tex2D(_MainTex, i.uv - delta.xy).a),
                              max(tex2D(_MainTex, i.uv + delta.xw).a, tex2D(_MainTex, i.uv - delta.xw).a));

                half aa = max(a0, max(a1, a2));

                color.a *= aa;

                #ifdef UNITY_UI_CLIP_RECT
                
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                #endif
                
                #ifdef UNITY_UI_ALPHACLIP

                clip(color.a - 0.001);
                
                #endif
           
                return color;
            }

            ENDCG
        }

        // draw real sprite

        Pass
		{
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t 
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f 
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;

                UNITY_VERTEX_OUTPUT_STEREO
            };

           sampler2D _MainTex;
           fixed4 _Color;
           fixed4 _TextureSampleAdd;
           float4 _MainTex_ST;
           float4 _MainTex_TexelSize;

            v2f vert (appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPosition = v.vertex;

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.uv) + _TextureSampleAdd) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                
                color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                #endif
                
                #ifdef UNITY_UI_ALPHACLIP

                clip(color.a - 0.001);
                
                #endif

                return color;
            }

            ENDCG
		}
   }
}