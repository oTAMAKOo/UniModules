// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/Unlit/SoftParticle/AddMultiply" 
{
    Properties
    {
        _MainTex("Particle Texture", 2D) = "white" {}
        _TintColor("Tint Color", Color) = (0.5,0.5,0.5,0.5)
        _Contrast("Contrast Factor", Range(0.5,1.0)) = 0.1
        _InvFade("Soft Particles Factor", Range(0.01,3.0)) = 1.0
    }

    CGINCLUDE

    #pragma exclude_renderers gles
    #pragma target 3.0
    #pragma vertex vert
    #pragma multi_compile_particles
    #pragma fragmentoption ARB_precision_hint_fastest

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
    
        #ifdef SOFTPARTICLES_ON

        float4 projPos : TEXCOORD1;
        
        #endif
    };

    float4 _MainTex_ST;

    v2f vert(appdata_t v)
    {
        v2f o;
        o.vertex = UnityObjectToClipPos(v.vertex);

        #ifdef SOFTPARTICLES_ON

        o.projPos = ComputeScreenPos(o.vertex);
        COMPUTE_EYEDEPTH(o.projPos.z);

        #endif
    
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);

        UNITY_TRANSFER_FOG(o, o.vertex);

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
        ColorMask RGB

        Fog { Color(0,0,0,0) }
        
        BindChannels 
        {
            Bind "Color", color
            Bind "Vertex", vertex
            Bind "TexCoord", texcoord
        }

        SubShader 
        {
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
                Blend Zero SrcColor

                CGPROGRAM

                #pragma fragment frag

                fixed4 frag(v2f i) : COLOR
                {
                    half4 col = i.color * tex2D(_MainTex, i.texcoord);

                    col.r = gain(col.r , _Contrast);
                    col.g = gain(col.g , _Contrast);
                    col.b = gain(col.b , _Contrast);

                    #ifdef SOFTPARTICLES_ON

                    float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
                    float partZ = i.projPos.z;
                    float fade = saturate(_InvFade * (sceneZ - partZ));

                    col.a *= fade;

                    #endif

                    return lerp(half4(1,1,1,1), col, col.a);
                }

                ENDCG
            }

            Pass 
            {
                Blend SrcAlpha One
                AlphaTest Greater .01
                ColorMask RGB

                CGPROGRAM

                #pragma fragment frag

                fixed4 frag(v2f i) : COLOR
                {
                    fixed4 col = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);

                    col.r = gain(col.r , _Contrast);
                    col.g = gain(col.g , _Contrast);
                    col.b = gain(col.b , _Contrast);

                    #ifdef SOFTPARTICLES_ON

                    float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
                    float partZ = i.projPos.z;
                    float fade = saturate(_InvFade * (sceneZ - partZ));

                    col.a *= fade;

                    #endif

                    UNITY_APPLY_FOG(i.fogCoord, col);

                    return col;

                }

                ENDCG
            }
        }
    }
}
