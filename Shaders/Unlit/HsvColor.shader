
Shader "Custom/Unlit/HsvColor"
{
    Properties
    {
        [HideInInspector] 
        _MainTex ("Sprite Texture", 2D) = "white" {}

        _Hue ("Hue", Float) = 0
        _Sat ("Saturation", Float) = 1
        _Val ("Value", Float) = 1

        _Color ("Tint", Color) = (1,1,1,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255

        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
        ColorMask[_ColorMask]

        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

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
                float2 texcoord  : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            half _Hue, _Sat, _Val;

            fixed3 shift_col(fixed3 rgb, half3 shift)
            {
                fixed3 result = fixed3(rgb);

                float rad_shift_x = radians(shift.x);

                float vsu = shift.z * shift.y * cos(rad_shift_x);
                float vsw = shift.z * shift.y * sin(rad_shift_x);

                 result.x = (0.299 * shift.z + 0.701 * vsu + 0.168 * vsw) * rgb.x + 
                            (0.587 * shift.z - 0.587 * vsu + 0.330 * vsw) * rgb.y + 
                            (0.114 * shift.z - 0.114 * vsu - 0.497 * vsw) * rgb.z;

                 result.y = (0.299 * shift.z - 0.299 * vsu - 0.328 * vsw) * rgb.x + 
                            (0.587 * shift.z + 0.413 * vsu + 0.035 * vsw) * rgb.y + 
                            (0.114 * shift.z - 0.114 * vsu + 0.292 * vsw) * rgb.z;

                 result.z = (0.299 * shift.z - 0.3 * vsu + 1.25 * vsw) * rgb.x + 
                            (0.587 * shift.z - 0.588 * vsu - 1.05 * vsw) * rgb.y +
                            (0.114 * shift.z + 0.886 * vsu - 0.203 * vsw) * rgb.z;

                return result;
            }

            v2f vert(appdata_t v)
            {
                v2f output;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.worldPosition = v.vertex;
                output.vertex = UnityObjectToClipPos(v.vertex);

                output.texcoord = v.texcoord;

                output.color = v.color * _Color;

                return output;
            }

            sampler2D _MainTex;


            fixed4 frag(v2f IN) : SV_Target
            {
                half4 color = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;
                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                half3 shift = half3(_Hue, _Sat, _Val);

                return fixed4( shift_col(color, shift), color.a);
            }

            ENDCG
        }
    }
}