Shader "DeadDawn/VignetteFogPlane"
{
    Properties
    {
        _VignetteColor ("Vignette Tint", Color) = (0.02, 0.03, 0.05, 0.85)
        _InnerRadius ("Clear View Radius", Range(0.0, 0.5)) = 0.28
        _Feather ("Edge Softness", Range(0.01, 0.5)) = 0.18
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent+100" "RenderPipeline"="UniversalPipeline" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
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
                float4 _VignetteColor;
                float _InnerRadius;
                float _Feather;
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
                // Distance from center (0.5, 0.5)
                float2 centerVec = input.uv - float2(0.5, 0.5);
                float dist = length(centerVec);

                // Smoothly fade from 0 (clear vision around player) to dark edge
                float alphaFactor = smoothstep(_InnerRadius, _InnerRadius + _Feather, dist);

                half4 finalColor = _VignetteColor;
                finalColor.a *= alphaFactor;
                return finalColor;
            }
            ENDHLSL
        }
    }
}
