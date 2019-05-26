
Shader "Custom/UI/Text-Shadow (SoftMask)"
{
    Properties
	{
		[PerRendererData]
        _MainTex ("Sprite Texture", 2D) = "white" {}		
        
        [HDR]
        _ShadowColor ("Shadow", Color) = (0,0,0,1)
		
        _Offset ("ShadowOffset", Vector) = (0,-0.1,0,0)
        
        [MaterialToggle] 
        PixelSnap ("Pixel snap", Float) = 0

        // required for UI.Mask
        _StencilComp("Stencil Comparison", Float) = 8
        _Stencil("Stencil ID", Float) = 0
        _StencilOp("Stencil Operation", Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask("Stencil Read Mask", Float) = 255

        _ColorMask("Color Mask", Float) = 15

        // required for SoftMask
        [PerRendererData] 
        _SoftMask("Mask", 2D) = "white" {}
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

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest[unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask[_ColorMask]

		// draw shadow
		Pass
		{
		    CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _PIXELSNAP_ON

            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma multi_compile __ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED

			#include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/UnityAssets/SoftMask/Shaders/SoftMask.cginc"
			
			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
                
                SOFTMASK_COORDS(2)
			};
			
            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _ShadowColor;
			float4 _Offset;

			v2f vert(appdata_t v)
			{
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex + _Offset);
                o.color = _ShadowColor;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                SOFTMASK_CALCULATE_COORDS(o, v.vertex)

                o.color.a *= v.color.a; 

				#ifdef PIXELSNAP_ON
				
                o.vertex = UnityPixelSnap (o.vertex);
				
                #endif

				return o;
			}

			sampler2D _AlphaTex;
			float _AlphaSplitEnabled;

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

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 color = SampleSpriteTexture (i.texcoord) * i.color;
				color.rgb *= color.a;
               
                color.a *= SOFTMASK_GET_MASK(i);

                return color;
			}

		    ENDCG
		}

		// draw real text
		Pass
		{
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            #pragma multi_compile __ UNITY_UI_ALPHACLIP
            #pragma multi_compile __ SOFTMASK_SIMPLE SOFTMASK_SLICED SOFTMASK_TILED

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "Assets/UnityAssets/SoftMask/Shaders/SoftMask.cginc"

            struct appdata_t 
            {
                float4 vertex   : POSITION;
                half4  color    : COLOR;
                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f 
            {
                float4 vertex   : SV_POSITION;
                half4  color    : COLOR;
                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_OUTPUT_STEREO
                
                SOFTMASK_COORDS(2)
            };

           sampler2D _MainTex;
           float4 _MainTex_ST;
           float4 _MainTex_TexelSize;
           fixed4 _TextureSampleAdd;

            v2f vert (appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

                SOFTMASK_CALCULATE_COORDS(o, v.vertex)

                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
               
                color.a *= SOFTMASK_GET_MASK(i);

                return color;
            }

            ENDCG
		}
	}
}