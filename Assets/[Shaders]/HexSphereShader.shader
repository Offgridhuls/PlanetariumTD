Shader "Custom/HexSphereURP"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _HexSize ("Hex Size", Range(0.01, 10.0)) = 1.0
        _HexEdgeWidth ("Hex Edge Width", Range(0.0, 0.5)) = 0.05
        _HexEdgeColor ("Hex Edge Color", Color) = (0,0,0,1)
        _SelectionColor ("Selection Color", Color) = (1,0.5,0,1)
        _ResourceIconTex ("Resource Icons", 2D) = "black" {}
        _ResourceMaskTex ("Resource Icon Masks", 2D) = "black" {}
        
        // For visualization
        _HighlightStrength ("Highlight Strength", Range(0, 1)) = 0.2
        _VisualizationStrength ("Visualization Strength", Range(0, 1)) = 0
        _VisualizationColor ("Visualization Color", Color) = (0,1,0,1)
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
        }
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
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
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
            };
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_ResourceIconTex);
            SAMPLER(sampler_ResourceIconTex);
            TEXTURE2D(_ResourceMaskTex);
            SAMPLER(sampler_ResourceMaskTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _HexSize;
                float _HexEdgeWidth;
                float4 _HexEdgeColor;
                float4 _SelectionColor;
                float4 _ResourceIconTex_ST;
                float4 _ResourceMaskTex_ST;
                float _HighlightStrength;
                float _VisualizationStrength;
                float4 _VisualizationColor;
            CBUFFER_END
            
            // Buffer for instance data
            StructuredBuffer<float4> _HexData;
            
            // Helper functions for hex grid calculations
            float2 cubeToAxial(float3 cube) {
                float q = cube.x;
                float r = cube.z;
                return float2(q, r);
            }
            
            float3 axialToCube(float2 axial) {
                float x = axial.x;
                float z = axial.y;
                float y = -x - z;
                return float3(x, y, z);
            }
            
            // Convert world position to hex coordinates
            float2 posToHex(float3 pos) {
                // Normalize position to get direction from center
                float3 dir = normalize(pos);
                
                // Calculate spherical coordinates
                float phi = atan2(dir.z, dir.x);
                float theta = acos(dir.y);
                
                // Map to 2D hex grid (this requires tuning for specific sphere size)
                float q = phi * _HexSize / (3.14159 * 2);
                float r = theta * _HexSize / 3.14159;
                
                // Round to nearest hex
                return float2(q, r);
            }
            
            // Calculate distance from point to nearest hex edge
            float hexEdgeDistance(float2 hexCoord) {
                float2 nearestCenter = round(hexCoord);
                float2 diff = abs(hexCoord - nearestCenter);
                
                // Calculating distance to edge in hex space
                return min(max(diff.x, diff.y), 0.5);
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                // Standard vertex transformation
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.positionOS = input.positionOS.xyz;
                
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                // Convert sphere position to hex coordinates
                float2 hexCoord = posToHex(normalize(input.positionWS));
                
                // Calculate nearest hex center
                float2 nearestHex = round(hexCoord);
                
                // Get texture color
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Color;
                
                // Calculate distance to hex edge for drawing borders
                float edgeDist = hexEdgeDistance(hexCoord);
                
                // Create hex edge effect
                float edgeFactor = smoothstep(_HexEdgeWidth - 0.01, _HexEdgeWidth, edgeDist);
                
                // Blend texture with edges
                float4 finalColor = lerp(_HexEdgeColor, texColor, edgeFactor);
                
                // Optional: highlight specific hexes for selection/visualization
                // We'll use the hex coordinates to check if this is a selected tile
                
                // Example: Highlight based on a pattern (this would be replaced by game logic)
                float highlight = 0;
                
                // This is where you would use the _HexData buffer in actual implementation
                // to check if the current hex is selected or contains a resource
                if (frac(nearestHex.x * 7.3 + nearestHex.y * 11.9) < _HighlightStrength) {
                    highlight = 0.5;
                }
                
                // Apply visualization overlay (this would be driven by game data in real use)
                finalColor = lerp(finalColor, _VisualizationColor, _VisualizationStrength * 
                                 (0.5 + 0.5 * sin(nearestHex.x + nearestHex.y)));
                
                // Add highlight effect
                finalColor = lerp(finalColor, _SelectionColor, highlight);
                
                // Apply resource icons (mask-based approach)
                float2 iconUV = float2(frac(nearestHex.x * 0.1) * 10, frac(nearestHex.y * 0.1) * 10);
                float4 resourceMask = SAMPLE_TEXTURE2D(_ResourceMaskTex, sampler_ResourceMaskTex, iconUV);
                
                if (resourceMask.r > 0.1) {
                    float4 iconColor = SAMPLE_TEXTURE2D(_ResourceIconTex, sampler_ResourceIconTex, iconUV);
                    finalColor = lerp(finalColor, iconColor, resourceMask.r);
                }
                
                // Basic lighting
                float ndotl = saturate(dot(input.normalWS, _MainLightPosition.xyz));
                finalColor.rgb *= (0.5 + 0.5 * ndotl); // Simple half-lambert lighting
                
                return finalColor;
            }
            ENDHLSL
        }
    }
}