Shader "Custom/NodeBorderShader"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1,1,1,0.5)
        _BorderColor ("Border Color", Color) = (1,1,1,1)
        _BorderWidth ("Border Width", Range(0.0, 0.5)) = 0.05
        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _BorderColor;
                float _BorderWidth;
                float _Smoothness;
            CBUFFER_END
            
            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                // Transform position and normal to world space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Calculate distance from center (0.5, 0.5)
                float2 centeredUV = input.uv - 0.5;
                float distFromCenter = length(centeredUV);
                
                // Create a disc with a border
                float disc = 1.0 - step(0.5, distFromCenter);
                float border = step(0.5 - _BorderWidth, distFromCenter) * disc;
                
                // Smooth the border
                float smoothBorder = smoothstep(0.5 - _BorderWidth - 0.01, 0.5 - _BorderWidth + 0.01, distFromCenter) * disc;
                
                // Mix the colors
                float4 color = lerp(_MainColor, _BorderColor, smoothBorder);
                
                // Apply fog
                color.rgb = MixFog(color.rgb, input.fogFactor);
                
                // Apply lighting
                InputData lightingInput = (InputData)0;
                lightingInput.normalWS = normalize(input.normalWS);
                lightingInput.positionWS = input.positionWS;
                lightingInput.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lightingInput.shadowCoord = float4(0, 0, 0, 0);
                
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = color.rgb;
                surfaceData.alpha = color.a;
                surfaceData.metallic = 0;
                surfaceData.smoothness = _Smoothness;
                
                // Only apply disc mask to alpha
                color.a *= disc;
                
                return color;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
} 