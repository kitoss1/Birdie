// Procedural wood grain shader for black-and-white URP 2D sprites.
// The sprite must use black for lines/outline and white for the fill area.
//
// Key properties:
//   Line Color             - color applied to the black line pixels (for variations)
//   Light/Dark Wood Color  - palette of the grain on the fill area
//   Line Threshold         - luminance cutoff between lines and fill (default 0.5)
//   Grain Strength         - 0 = flat wood color, 1 = fully procedural grain
//   Ring Frequency         - number of growth rings visible
//   Noise Distortion       - how much the rings wobble
//   Grain Angle            - rotate grain direction in degrees
//   Fine Grain Scale       - density of longitudinal streaks
//   Fine Grain Strength    - blend between rings and fine streaks

Shader "Custom/WoodGrainBW"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Line)]
        _LineColor     ("Line Color",      Color)        = (0.15, 0.08, 0.03, 1)
        _LineThreshold ("Line Threshold",  Range(0, 1))  = 0.5

        [Header(Wood Colors)]
        _LightWood ("Light Wood Color", Color) = (0.84, 0.64, 0.38, 1)
        _DarkWood  ("Dark Wood Color",  Color) = (0.38, 0.22, 0.10, 1)

        [Header(Grain Shape)]
        _GrainScale        ("Grain Scale",        Float)            = 4.0
        _GrainAngle        ("Grain Angle",         Range(-180, 180)) = 0
        _RingFrequency     ("Ring Frequency",      Float)            = 5.0
        _NoiseStrength     ("Noise Distortion",    Range(0, 2))      = 0.6
        _FineGrainScale    ("Fine Grain Scale",    Float)            = 14.0
        _FineGrainStrength ("Fine Grain Strength", Range(0, 1))      = 0.25

        [Header(Blending)]
        _GrainStrength ("Grain Strength", Range(0, 1)) = 1.0

        // Hidden properties used by SpriteRenderer
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector][PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector][PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "RenderType"        = "Transparent"
            "RenderPipeline"    = "UniversalPipeline"
            "CanUseSpriteAtlas" = "True"
        }

        Blend One OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   WoodGrainBWVert
            #pragma fragment WoodGrainBWFrag
            #include "WoodGrainBWPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "Universal2D"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex   WoodGrainBWVert
            #pragma fragment WoodGrainBWFrag
            #include "WoodGrainBWPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default"
}
