Shader "DeadDawn/VisibilityBoundary"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.11, 0.14, 0.20, 0.98)
        _PlayerScreenPos ("Player Screen Pos (UV)", Vector) = (0.5, 0.5, 0, 0)
        _ClearRadius ("Clear Vision Radius", Range(0.05, 0.8)) = 0.26
        _Feather ("Feather Softness", Range(0.01, 0.5)) = 0.18
        _FogDensity ("Max Fog Density", Range(0.0, 1.0)) = 0.98
        _NoiseSpeed ("Mist Drift Speed", Float) = 0.04
        _NoiseScale ("Mist Drift Scale", Float) = 8.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+80" 
            "RenderPipeline"="UniversalPipeline" 
        }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "VisibilityBoundaryPass"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _FogColor;
                float4 _PlayerScreenPos;
                float _ClearRadius;
                float _Feather;
                float _FogDensity;
                float _NoiseSpeed;
                float _NoiseScale;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Correct for screen aspect ratio so vision cone is circular
                float aspect = _ScreenParams.x / _ScreenParams.y;
                float2 offset = input.uv - _PlayerScreenPos.xy;
                offset.x *= aspect;

                float dist = length(offset);

                // Subtle atmospheric mist variation
                float mist = sin((input.uv.x + _Time.y * _NoiseSpeed) * _NoiseScale) 
                           * cos((input.uv.y - _Time.y * _NoiseSpeed * 0.6) * _NoiseScale) * 0.015;
                dist += mist;

                // Fade from clear center to dense boundary fog
                float fogFactor = smoothstep(_ClearRadius, _ClearRadius + _Feather, dist);
                fogFactor = saturate(fogFactor * _FogDensity);

                half4 finalColor = _FogColor;
                finalColor.a *= fogFactor;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
