
Shader "Custom/GrabPass/Blur" 
{
    Properties 
	{
        _Distortion ("Distortion", Range(0, 100)) = 1

		[HideInInspector] 
        _MainTex ("Tint Color (RGB)", 2D) = "white" {}
    }
     
    Category 
	{
		Tags 
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Opaque" 
		}
          
        SubShader 
		{
         
            // Horizontal blur
            GrabPass 
			{                    
                Tags { "LightMode" = "Always" }
            }

            Pass 
			{
                Tags { "LightMode" = "Always" }
                 
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                 
                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    fixed4 color : COLOR;
                };

                struct v2f
                {
                    float4 grabPos : TEXCOORD0;
                    float4 pos : SV_POSITION;
                    float4 vertColor : COLOR;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.grabPos = ComputeGrabScreenPos(o.pos);
                    o.vertColor = v.color;
                    return o;
                }
                 
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Distortion;

                half4 frag(v2f i) : SV_Target
                {
                    float distortion = _Distortion;
                    distortion = max(1, distortion);

                    fixed4 col = (0, 0, 0, 0);
                    float weight_total = 0;

                    [loop]
                    for (float x = -distortion; x <= distortion; x += 1)
                    {
                        float distance_normalized = abs(x / distortion);
                        float weight = exp(-0.5 * pow(distance_normalized, 2) * 5.0);
                        weight_total += weight;
                        col += tex2Dproj(_GrabTexture, i.grabPos + float4(x * _GrabTexture_TexelSize.x, 0, 0, 0)) * weight;
                    }

                    col /= weight_total;

                    return col;
                }
                ENDCG
            }

            // Vertical blur
            GrabPass 
			{                        
                Tags { "LightMode" = "Always" }
            }

            Pass 
			{
                Tags { "LightMode" = "Always" }
                 
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                 
                struct appdata
                {
                    float4 vertex : POSITION;
                    float2 uv : TEXCOORD0;
                    fixed4 color : COLOR;
                };

                struct v2f
                {
                    float4 grabPos : TEXCOORD0;
                    float4 pos : SV_POSITION;
                    float4 vertColor : COLOR;
                };

                v2f vert(appdata v)
                {
                    v2f o;
                    o.pos = UnityObjectToClipPos(v.vertex);
                    o.grabPos = ComputeGrabScreenPos(o.pos);
                    o.vertColor = v.color;
                    return o;
                }
                 
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                float _Distortion;

                half4 frag(v2f i) : SV_Target
                {
                    float distortion = _Distortion;
                    distortion = max(1, distortion);

                    fixed4 col = (0, 0, 0, 0);
                    float weight_total = 0;

                    [loop]
                    for (float y = -distortion; y <= distortion; y += 1)
                    {
                        float distance_normalized = abs(y / distortion);
                        float weight = exp(-0.5 * pow(distance_normalized, 2) * 5.0);
                        weight_total += weight;
                        col += tex2Dproj(_GrabTexture, i.grabPos + float4(0, y * _GrabTexture_TexelSize.y, 0, 0)) * weight;
                    }

                    col /= weight_total;

                    return col;
                }

                ENDCG
            }
        
            // Tint
            GrabPass 
			{                        
                Tags { "LightMode" = "Always" }
            }

            Pass 
			{
                Tags { "LightMode" = "Always" }
                 
                CGPROGRAM

                #pragma vertex vert
                #pragma fragment frag
                #pragma fragmentoption ARB_precision_hint_fastest
                #include "UnityCG.cginc"
                 
                struct appdata_t 
				{
                    float4 vertex	: POSITION;
                    float2 texcoord	: TEXCOORD0;
					float4 color	: COLOR;
                };
                 
                struct v2f 
				{
                    float4 vertex : POSITION;
                    float4 grabPos : TEXCOORD0;
                    float2 uvmain : TEXCOORD2;
					float4 color  : COLOR;
                };
                 
                float4 _MainTex_ST;
                 
                v2f vert (appdata_t v) 
				{
                    #if UNITY_UV_STARTS_AT_TOP
                    float scale = -1.0;
                    #else
                    float scale = 1.0;
                    #endif

					v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.grabPos.xy = ComputeGrabScreenPos(o.vertex);
                    o.uvmain = TRANSFORM_TEX( v.texcoord, _MainTex);
					o.color = v.color;

                    return o;
                }
                 
                sampler2D _GrabTexture;
                float4 _GrabTexture_TexelSize;
                sampler2D _MainTex;
                 
                half4 frag( v2f i ) : COLOR 
				{
                    i.grabPos.xy = _GrabTexture_TexelSize.xy * i.grabPos.z + i.grabPos.xy;
                     
                    half4 col = tex2Dproj( _GrabTexture, UNITY_PROJ_COORD(i.grabPos));
					half4 tint = tex2D( _MainTex, i.uvmain) * i.color;

					float ratio = 1 - 0.5 * tint.a;

					col.r = (col.r + tint.r * tint.a) * ratio;
					col.g = (col.g + tint.g * tint.a) * ratio;
					col.b = (col.b + tint.b * tint.a) * ratio;

                    return col;
                }

                ENDCG
            }
        }
    }
}