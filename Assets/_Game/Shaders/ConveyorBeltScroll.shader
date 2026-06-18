Shader "FlowBlast/ConveyorBeltScroll"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (0.72, 0.75, 0.8, 1)
        _SlatRepeat("Slat Repeat", Float) = 24
        _SlatContrast("Slat Contrast", Range(0, 0.5)) = 0.1
        _ScrollSpeed("Scroll Speed", Float) = 0.2
        _PathHalfLength("Path Half Length", Float) = 1.5
        _PathRadius("Path Radius", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _SlatRepeat;
                float _SlatContrast;
                float _ScrollSpeed;
                float _PathHalfLength;
                float _PathRadius;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
            };

            float GetBeltLoop01(float2 xz, float3 normalOS, float halfLen, float radius)
            {
                const float pi = 3.14159265359;
                const float halfCapArc = pi * 0.5 * radius;
                const float capArc = pi * radius;
                const float straightSpan = halfLen * 2.0;
                const float totalLength = straightSpan * 2.0 + capArc * 2.0;
                float coord = 0.0;

                if (abs(xz.x) <= halfLen)
                {
                    if (xz.y >= 0.0)
                    {
                        coord = halfCapArc + (xz.x + halfLen);
                    }
                    else
                    {
                        coord = straightSpan + capArc + (halfLen - xz.x);
                    }
                }
                else if (xz.x > halfLen)
                {
                    float2 capOffset = xz - float2(halfLen, 0.0);
                    float angle = atan2(capOffset.y, capOffset.x);
                    coord = straightSpan + (pi * 0.5 - angle) * radius;
                }
                else
                {
                    float2 capOffset = xz - float2(-halfLen, 0.0);
                    float angle = atan2(capOffset.y, capOffset.x);
                    coord = (pi - angle) * radius;
                }

                float loopCoord = coord / totalLength;

                if (normalOS.y < 0.0)
                {
                    loopCoord = 1.0 - loopCoord;
                }

                return loopCoord;
            }

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionOS = input.positionOS.xyz;
                output.normalWS = normalInputs.normalWS;
                output.normalOS = input.normalOS;
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float loopCoord = GetBeltLoop01(input.positionOS.xz, input.normalOS, _PathHalfLength, _PathRadius);
                float slatPhase = loopCoord * _SlatRepeat - _Time.y * _ScrollSpeed;
                float slat = step(0.5, frac(slatPhase));

                half3 dark = _BaseColor.rgb * (1.0h - _SlatContrast);
                half3 light = _BaseColor.rgb * (1.0h + _SlatContrast);
                half3 albedo = lerp(dark, light, slat);

                Light mainLight = GetMainLight();
                half ndotl = saturate(dot(normalize(input.normalWS), mainLight.direction));
                half3 lighting = mainLight.color * ndotl + half3(0.25h, 0.25h, 0.28h);
                half3 color = albedo.rgb * lighting;

                color = MixFog(color, input.fogFactor);
                return half4(color, _BaseColor.a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
