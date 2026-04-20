Shader "Custom/WindTrail"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1,1,1,1)
        _Speed ("Wiggle Speed", Float) = 2.0
        _Amplitude ("Wiggle Strength", Float) = 0.5
        _FlowSpeed ("Texture Flow Speed", Float) = 1.0
        _LifeCycleSpeed ("Life Cycle Speed", Float) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off 

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing 
        
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float2 uv           : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };
        
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float lifeFade      : TEXCOORD1; // Pass global fade to fragment
                UNITY_VERTEX_INPUT_INSTANCE_ID 
            };
        
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _Speed;
                float _Amplitude;
                float _FlowSpeed;
                float _LifeCycleSpeed;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
        
                float time = _Time.y;
                
                // 1. Global Lifecycle (Fade in/out over time)
                // We use a sine wave shifted to 0-1 range to handle the "appearing/disappearing"
                o.lifeFade = sin(time * _LifeCycleSpeed) * 0.5 + 0.5;

                // 2. Wiggle Logic
                // Using OS.x ensures the wave travels down the length of your X-axis ribbon
                float wave = sin(time * _Speed + v.positionOS.x);
                
                // Apply wiggle to Z (or Y depending on your preference)
                // We multiply by uv.x so the "head" (0) is pinned and "tail" (1) wiggles
                v.positionOS.y += wave * _Amplitude * v.uv.x;

                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                
                // 3. Texture Flow Logic
                // We offset the U coordinate based on time to make the texture "travel"
                o.uv = v.uv;
                
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                // Sample texture with the flowing UVs
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _Color;
                
                // 4. Alpha Composition
                // (1.0 - i.uv.x) = Fades the tail (standard trail look)
                // i.lifeFade = Fades the entire object in and out over time
                //float edgeFade = (1.0 - frac(i.uv.x)); // Use frac if you want the texture pattern to repeat cleanly
                
                // col.a *= (1.0 - saturate(i.uv.x)) * i.lifeFade; 
 	       	 col.a *= i.lifeFade;    

                return col;
            }
            ENDHLSL
        }
    }
}