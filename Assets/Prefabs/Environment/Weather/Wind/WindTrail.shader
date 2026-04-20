Shader "Custom/WindTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Speed ("Wind Speed", Float) = 2.0
        _Amplitude ("Wiggle Strength", Float) = 0.5
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off // Essential for flat ribbons so both sides show

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Speed;
                float _Amplitude;
            CBUFFER_END

	    Varyings vert(Attributes v)
	    {
		Varyings o;
	    
		// 1. Setup wave parameters
    	    	float time = _Time.y * _Speed;
    
		// Use the X position to create the traveling wave effect
    		// We add 'time' to make the wave move along the ribbon
    		float wave = sin(time + v.positionOS.x);
    
		// 2. Apply the Wiggle
    		// Multiplying by v.uv.x ensures the 'Head' (UV 0) is pinned 
    		// and the 'Tail' (UV 1) wiggles.
    		// If your Blender mesh has the head at the origin, use v.uv.x.
    		// If the head is at the far end, use (1.0 - v.uv.x).
    		//float mask = v.uv.y; 
    
		v.positionOS.z += wave * _Amplitude; //* mask;

    		// 3. Transform to Clip Space
    		o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    		o.uv = v.uv;
    		return o;
	}

            half4 frag(Varyings i) : SV_Target
            {
                // Sample texture and apply color
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                
                // Fade out the tail automatically
                col.a *= (1.0 - i.uv.x); 
                
                return col;
            }
            ENDHLSL
        }
    }
}