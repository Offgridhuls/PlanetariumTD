Shader "Custom/URPAtmosphere"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlueNoise ("Blue Noise", 2D) = "black" {}
        _BakedOpticalDepth ("Optical Depth", 2D) = "white" {}
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "AtmospherePass"
            
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            static const float PIE = 3.14159265359;
            static const float TAU = PIE * 2;
            static const float maxFloat = 3.402823466e+38;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 viewVector : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            TEXTURE2D(_BlueNoise);
            TEXTURE2D(_BakedOpticalDepth);
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_BlueNoise);
            SAMPLER(sampler_BakedOpticalDepth);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float3 dirToSun;
                float3 planetCentre;
                float atmosphereRadius;
                float oceanRadius;
                float planetRadius;
                int numInScatteringPoints;
                int numOpticalDepthPoints;
                float intensity;
                float4 scatteringCoefficients;
                float ditherStrength;
                float ditherScale;
                float densityFalloff;
            CBUFFER_END

            float remap(float v, float minOld, float maxOld, float minNew, float maxNew)
            {
                return saturate(minNew + (v - minOld) * (maxNew - minNew) / (maxOld - minOld));
            }

            float4 remap(float4 v, float minOld, float maxOld, float minNew, float maxNew)
            {
                return saturate(minNew + (v - minOld) * (maxNew - minNew) / (maxOld - minOld));
            }

            float2 squareUV(float2 uv)
            {
                float width = _ScreenParams.x;
                float height = _ScreenParams.y;
                float scale = 1000;
                float x = uv.x * width;
                float y = uv.y * height;
                return float2(x / scale, y / scale);
            }

            float2 raySphere(float3 sphereCentre, float sphereRadius, float3 rayOrigin, float3 rayDir)
            {
                float3 offset = rayOrigin - sphereCentre;
                float a = 1;
                float b = 2 * dot(offset, rayDir);
                float c = dot(offset, offset) - sphereRadius * sphereRadius;
                float d = b * b - 4 * a * c;

                if (d > 0)
                {
                    float s = sqrt(d);
                    float dstToSphereNear = max(0, (-b - s) / (2 * a));
                    float dstToSphereFar = (-b + s) / (2 * a);

                    if (dstToSphereFar >= 0)
                    {
                        return float2(dstToSphereNear, dstToSphereFar - dstToSphereNear);
                    }
                }
                return float2(maxFloat, 0);
            }

            float densityAtPoint(float3 densitySamplePoint)
            {
                float heightAboveSurface = length(densitySamplePoint - planetCentre) - planetRadius;
                float height01 = heightAboveSurface / (atmosphereRadius - planetRadius);
                float localDensity = exp(-height01 * densityFalloff) * (1 - height01);
                return localDensity;
            }

            float opticalDepthBaked(float3 rayOrigin, float3 rayDir)
            {
                float height = length(rayOrigin - planetCentre) - planetRadius;
                float height01 = saturate(height / (atmosphereRadius - planetRadius));
                float uvX = 1 - (dot(normalize(rayOrigin - planetCentre), rayDir) * .5 + .5);
                return SAMPLE_TEXTURE2D_LOD(_BakedOpticalDepth, sampler_BakedOpticalDepth, float2(uvX, height01), 0).r;
            }

            float opticalDepthBaked2(float3 rayOrigin, float3 rayDir, float rayLength)
            {
                float3 endPoint = rayOrigin + rayDir * rayLength;
                float d = dot(rayDir, normalize(rayOrigin - planetCentre));
                float opticalDepth = 0;

                const float blendStrength = 1.5;
                float w = saturate(d * blendStrength + .5);

                float d1 = opticalDepthBaked(rayOrigin, rayDir) - opticalDepthBaked(endPoint, rayDir);
                float d2 = opticalDepthBaked(endPoint, -rayDir) - opticalDepthBaked(rayOrigin, -rayDir);

                opticalDepth = lerp(d2, d1, w);
                return opticalDepth;
            }

            float3 calculateLight(float3 rayOrigin, float3 rayDir, float rayLength, float3 originalCol, float2 uv)
            {
                float blueNoise = SAMPLE_TEXTURE2D_LOD(_BlueNoise, sampler_BlueNoise, squareUV(uv) * ditherScale, 0).r;
                blueNoise = (blueNoise - 0.5) * ditherStrength;

                float3 inScatterPoint = rayOrigin;
                float stepSize = rayLength / (numInScatteringPoints - 1);
                float3 inScatteredLight = float3(0, 0, 0);
                float viewRayOpticalDepth = 0;
                float3 transmittance = float3(1, 1, 1);

                [loop]
                for (int i = 0; i < numInScatteringPoints; i++)
                {
                    float sunRayLength = raySphere(planetCentre, atmosphereRadius, inScatterPoint, dirToSun).y;
                    float sunRayOpticalDepth = opticalDepthBaked(inScatterPoint + dirToSun * ditherStrength, dirToSun);
                    float localDensity = densityAtPoint(inScatterPoint);
                    viewRayOpticalDepth = opticalDepthBaked2(rayOrigin, rayDir, stepSize * i);
                    transmittance = exp(-(sunRayOpticalDepth + viewRayOpticalDepth) * scatteringCoefficients.xyz);

                    inScatteredLight += localDensity * transmittance;
                    inScatterPoint += rayDir * stepSize;
                }
                
                inScatteredLight *= scatteringCoefficients.xyz * intensity * stepSize / planetRadius;
                inScatteredLight += float3(blueNoise, blueNoise, blueNoise) * 0.01;

                float3 reflectedLight = originalCol.xyz * transmittance;
                float3 finalCol = reflectedLight + inScatteredLight;

                return finalCol;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                
                // Calculate view vector (modified for URP)
                float4 clip = float4(input.uv * 2 - 1, 0, -1);
                float3 viewVector = mul(unity_CameraInvProjection, clip).xyz;
                output.viewVector = mul(unity_CameraToWorld, float4(viewVector, 0)).xyz;
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 originalCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float sceneDepthNonLinear = SampleSceneDepth(input.uv);
                float sceneDepth = LinearEyeDepth(sceneDepthNonLinear, _ZBufferParams) * length(input.viewVector);

                float fluidDst = originalCol.a < 0 ? -originalCol.a : 999999;
                sceneDepth = min(sceneDepth, fluidDst);

                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDir = normalize(input.viewVector);

                float dstToOcean = raySphere(planetCentre, oceanRadius, rayOrigin, rayDir).x;
                float dstToSurface = min(sceneDepth, dstToOcean);

                float2 hitInfo = raySphere(planetCentre, atmosphereRadius, rayOrigin, rayDir);
                float dstToAtmosphere = hitInfo.x;
                float dstThroughAtmosphere = min(hitInfo.y, dstToSurface - dstToAtmosphere);

                if (dstThroughAtmosphere > 0)
                {
                    const float epsilon = 0.0001;
                    float3 pointInAtmosphere = rayOrigin + rayDir * (dstToAtmosphere + epsilon);
                    float3 light = calculateLight(pointInAtmosphere, rayDir, dstThroughAtmosphere - epsilon * 2, originalCol.xyz, input.uv);
                    return float4(light, 1);
                }
                return originalCol;
            }
            ENDHLSL
        }
    }
}