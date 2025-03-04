Shader "Custom/HexTileUnlitTransparentURP"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (0.5, 0.5, 0.5, 1)
        _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineThickness ("Outline Thickness", Range(0.001, 0.2)) = 0.05
        _OutlineIntensity ("Outline Intensity", Range(0.1, 5.0)) = 1.0
        _HotspotIntensity ("Hotspot Intensity", Range(0, 1)) = 0
        _HotspotColor ("Hotspot Color", Color) = (1, 0.5, 0, 1)
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1.0
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 1.0
        [Toggle] _IsSelected ("Is Selected", Float) = 0
    }
    SubShader
    {
        Tags { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 300
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _MainColor;
            float4 _HotspotColor;
            float _HotspotIntensity;
            float _PulseSpeed;
            float _EmissionIntensity;
            float _IsSelected;
            float4 _OutlineColor;
            float _OutlineIntensity;
            float _OutlineThickness;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "UnlitForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                float4 color        : COLOR; // Add vertex color to indicate edge vertices (e.g., red for edges, white for non-edges)
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2;
                float outlineFactor : TEXCOORD3; // Pass outline strength to fragment shader
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Transform position and normal to world space
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);

                // Check if this vertex is on an edge using vertex color (assuming red = edge, white = non-edge)
                float isEdge = input.color.r > 0.5 ? 1.0 : 0.0; // Red channel indicates edge

                // Offset edge vertices outward to create an outline
                float3 outlineOffset = normalize(input.normalOS) * _OutlineThickness * isEdge * _OutlineIntensity;
                float3 positionOSWithOutline = input.positionOS.xyz + outlineOffset;

                // Transform the outlined position to clip space
                output.positionHCS = TransformObjectToHClip(positionOSWithOutline);
                output.positionWS = vertexInput.positionWS;
                output.normalWS = normalInput.normalWS;
                output.uv = input.texcoord;
                output.outlineFactor = isEdge; // Pass edge information to fragment shader

                return output;
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Sample base color
                half4 baseColor = _MainColor;
                
                // Calculate emission for hotspot and selection
                float pulse = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
                
                // Apply hotspot glow
                float3 hotspotEmission = _HotspotColor.rgb * _HotspotIntensity * pulse * _EmissionIntensity;
                
                // Apply selection highlight
                float3 selectionEmission = float3(1, 1, 0) * _IsSelected * pulse * _EmissionIntensity;
                
                // Apply outline emission based on vertex data
                float3 outlineEmission = _OutlineColor.rgb * input.outlineFactor * _OutlineIntensity;
                
                // Combine emissions
                float3 emission = hotspotEmission + selectionEmission + outlineEmission;

                // Since this is unlit, we just add emission to the base color
                half4 finalColor = half4(baseColor.rgb + emission, baseColor.a);
                
                return finalColor;
            }
            ENDHLSL
        }
        
        // Depth prepass - needed for proper sorting
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL; // Needed for outline offset
                float4 color        : COLOR; // Edge information
            };
            
            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
            };
            
            Varyings DepthOnlyVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                // Check if this vertex is on an edge using vertex color
                float isEdge = input.color.r > 0.5 ? 1.0 : 0.0;

                // Offset edge vertices outward for depth prepass
                float3 outlineOffset = normalize(input.normalOS) * _OutlineThickness * isEdge * _OutlineIntensity;
                float3 positionOSWithOutline = input.positionOS.xyz + outlineOffset;

                // Transform to clip space
                output.positionCS = TransformObjectToHClip(positionOSWithOutline);
                return output;
            }
            
            half4 DepthOnlyFragment(Varyings input) : SV_TARGET
            {
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}