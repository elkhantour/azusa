Shader "Custom/DualCircles"
{
    Properties
    {
        _Color          ("Color",                   Color)              = (0, 0.9, 1, 1)
        _InnerRadius    ("Inner Radius",            Range(0.01, 0.49))  = 0.28
        _OuterRadius    ("Outer Radius",            Range(0.01, 0.49))  = 0.49
        _ScreenThickness("Screen Thickness (px)",   Range(0.5, 10))     = 1.5
        _DashCount      ("Dash Count",              Range(2, 64))       = 16
        _DashRatio      ("Dash Ratio",              Range(0.05, 0.95))  = 0.55
        _RotationSpeed  ("Rotation Speed",          Float)              = 0.6
        _DashedOpacity  ("Dashed Circle Opacity",   Range(0, 1))        = 0.7
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
	ZTest Always
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4  _Color;
            float   _InnerRadius;
            float   _OuterRadius;
            float   _ScreenThickness;
            float   _DashCount;
            float   _DashRatio;
            float   _RotationSpeed;
            float   _DashedOpacity;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f    { float4 pos : SV_POSITION;  float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv   = i.uv - 0.5;
                float  dist = length(uv);
                float  angle = atan2(uv.y, uv.x);

                // --- Screen-space thickness ---
                float camDist = length(ObjSpaceViewDir(float4(0, 0, 0, 1)));

                float scaleX = length(float3(
                    unity_ObjectToWorld[0][0],
                    unity_ObjectToWorld[1][0],
                    unity_ObjectToWorld[2][0]
                ));
                float scaleY = length(float3(
                    unity_ObjectToWorld[0][1],
                    unity_ObjectToWorld[1][1],
                    unity_ObjectToWorld[2][1]
                ));
                float worldScale = (scaleX + scaleY) * 0.5;

                float thickness = (camDist / worldScale) * _ScreenThickness / _ScreenParams.y;
                float aa = thickness * 0.3;

                float alpha = 0.0;

                // --- Solid inner circle ---
                float d1    = abs(dist - _InnerRadius);
                float ring1 = 1.0 - smoothstep(thickness - aa, thickness + aa, d1);
                alpha += ring1;

                // --- Dashed outer circle ---
                float rotated    = angle + _Time.y * _RotationSpeed;
                float normalized = (rotated % (2.0 * UNITY_PI) + 2.0 * UNITY_PI)
                                   % (2.0 * UNITY_PI) / (2.0 * UNITY_PI);
                float dashPhase  = fmod(normalized * _DashCount, 1.0);
                float onDash     = step(dashPhase, _DashRatio);

                float d2    = abs(dist - _OuterRadius);
                float ring2 = 1.0 - smoothstep(thickness - aa, thickness + aa, d2);
                ring2      *= onDash;

                alpha += ring2 * _DashedOpacity;

                return fixed4(_Color.rgb, saturate(alpha) * _Color.a);
            }
            ENDCG
        }
    }
}