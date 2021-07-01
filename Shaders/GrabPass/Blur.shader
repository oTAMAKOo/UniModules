
Shader "Custom/GrabPass/Blur" 
{
    Properties 
	{
        [HideInInspector]
        _MainTex("Tint Color (RGB)", 2D) = "white" {}

        _Distortion ("Distortion", Range(0, 100)) = 1
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

                    fixed4 col = fixed4(0, 0, 0, 0);
                    float weight_total = 0;

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

                    fixed4 col = fixed4(0, 0, 0, 0);
                    float weight_total = 0;

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
        }
    }
}