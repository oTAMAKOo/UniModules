
Shader "Custom/Mask/Unlit/Additive" 
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}

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
        Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        
        Blend SrcAlpha One
        
        Cull Off 
        Lighting Off 
        ZWrite Off 
        ZTest[unity_GUIZTestMode]
        ColorMask[_ColorMask]

        Fog { Color(0,0,0,0) }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        BindChannels 
        {
            Bind "Color", color
            Bind "Vertex", vertex
            Bind "TexCoord", texcoord
        }

        Pass
        {
            SetTexture[_MainTex]
            {
                combine texture * primary
            }
        }
    }
}