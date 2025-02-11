Shader "Custom/PlanetWeather"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WeatherTex ("Weather Pattern", 2D) = "white" {}
        _WeatherColor ("Weather Color", Color) = (1,1,1,0.5)
        _WeatherIntensity ("Weather Intensity", Range(0,1)) = 0.5
        _RotationSpeed ("Rotation Speed", Float) = 1.0
        _WeatherHeight ("Weather Height", Float) = 1.1
        _WeatherBandWidth ("Weather Band Width", Range(0,1)) = 0.1
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent"
            "Queue" = "Transparent+100"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        
        Pass
        {
            Name "WeatherPass"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };
            
            TEXTURE2D(_MainTex);
            TEXTURE2D(_WeatherTex);
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_WeatherTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _WeatherTex_ST;
                float4 _WeatherColor;
                float _WeatherIntensity;
                float _RotationSpeed;
                float _WeatherHeight;
                float _WeatherBandWidth;
                float3 _PlanetCenter;
                float _PlanetRadius;
            CBUFFER_END
            
            float2 RotateUV(float2 uv, float rotation)
            {
                float s = sin(rotation);
                float c = cos(rotation);
                float2x2 rotationMatrix = float2x2(c, -s, s, c);
                return mul(rotationMatrix, uv - 0.5) + 0.5;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Transform position and normal
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                
                // Pass UVs
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                // Calculate fog factor
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Calculate distance from planet center
                float distFromCenter = length(input.positionWS - _PlanetCenter);
                float normalizedHeight = (distFromCenter - _PlanetRadius) / _PlanetRadius;
                
                // Weather band mask with softer falloff
                float weatherBand = 1 - saturate(abs(normalizedHeight - _WeatherHeight) / _WeatherBandWidth);
                weatherBand = smoothstep(0, 1, weatherBand);
                
                // Rotate UVs based on time and add some variation
                float2 rotatedUV = RotateUV(input.uv, _Time.y * _RotationSpeed);
                float2 secondaryUV = RotateUV(input.uv, _Time.y * _RotationSpeed * 0.7);
                
                // Sample textures with different scales
                float4 weatherPattern1 = SAMPLE_TEXTURE2D(_WeatherTex, sampler_WeatherTex, rotatedUV);
                float4 weatherPattern2 = SAMPLE_TEXTURE2D(_WeatherTex, sampler_WeatherTex, secondaryUV * 2);
                float4 baseColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // Combine weather patterns
                float weatherMask = (weatherPattern1.r * 0.7 + weatherPattern2.r * 0.3) * weatherBand * _WeatherIntensity;
                
                // Add some variation based on normal
                float normalFactor = saturate(dot(normalize(input.normalWS), float3(0, 1, 0)) * 0.5 + 0.5);
                weatherMask *= normalFactor;
                
                // Combine colors with rim lighting effect
                float3 viewDir = normalize(_WorldSpaceCameraPos - input.positionWS);
                float rim = 1.0 - saturate(dot(normalize(input.normalWS), viewDir));
                rim = pow(rim, 3);
                
                float4 finalColor = lerp(float4(0,0,0,0), _WeatherColor, weatherMask);
                finalColor.rgb += rim * _WeatherColor.rgb * weatherMask;
                finalColor.a = weatherMask * _WeatherColor.a;
                
                // Apply fog
                float3 foggedColor = MixFog(finalColor.rgb, input.fogFactor);
                return float4(foggedColor, finalColor.a);
            }
            ENDHLSL
        }
    }
}
