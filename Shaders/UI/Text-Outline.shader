
Shader "Custom/UI/Text-Outline"
{
   Properties
   {
        [PerRendererData]
        _MainTex ("Font Texture", 2D) = "white" {}
       
        [HDR]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineSpread ("Outline Spread", Range(0.1, 5)) = 1

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
            };

            struct v2f 
            {
                float4 vertex : SV_POSITION;
                half4  color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            half4  _OutlineColor;
            half   _OutlineSpread;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
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

            struct appdata_t 
            {
                float4 vertex : POSITION;
                half4  color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f 
            {
                float4 vertex : SV_POSITION;
                half4  color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

           sampler2D _MainTex;
           float4 _MainTex_ST;
           float4 _MainTex_TexelSize;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv,_MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = i.color;
                
                half a0 = tex2D(_MainTex, i.uv).a;

                col.a *= a0;

                return col;
            }

            ENDCG
		}
   }
}