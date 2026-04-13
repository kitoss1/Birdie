// UI Wood Grain shader for URP Canvas Image components.
// Works on colored sprites (light brown, etc.) by adding procedural
// wobbly grain lines on top of the existing sprite color — no B&W source needed.
//
// Key properties:
//   Grain Scale        - overall zoom of the grain pattern (higher = more zoomed in)
//   Grain Angle        - rotate grain direction in degrees (0 = horizontal lines)
//   Ring Frequency     - number of visible grain lines
//   Noise Distortion   - how wobbly/organic the lines are
//   Line Darkness      - how dark the grain lines are (0 = black, 1 = no darkening)
//   Grain Strength     - 0 = flat sprite, 1 = fully procedural grain applied
//   Line Sharpness     - sharper lines with higher values (try 1.5–4)
//   Fine Grain Scale   - density of longitudinal streaks
//   Fine Grain Strength- blend between ring lines and fine streaks

Shader "Custom/UIWoodGrain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Grain Shape)]
        _GrainScale    ("Grain Scale",     Float)            = 3.0
        _GrainAngle    ("Grain Angle",     Range(-180, 180)) = 0.0
        _RingFrequency ("Ring Frequency",  Float)            = 30.0
        _MaxStrokes    ("Max Strokes",     Range(1, 60))     = 30.0
        _StrokeLength  ("Stroke Length",   Range(0.05, 1))   = 0.35
        _NoiseStrength ("Noise Distortion",Range(0, 3))      = 0.8

        [Header(Grain Appearance)]
        _GrainStrength ("Grain Strength",  Range(0, 1))      = 0.9
        _LineDarkness  ("Line Darkness",   Range(0, 1))      = 0.45
        _LineScale     ("Line Scale",      Range(0.1, 10))   = 1.0
        _LineSharpness ("Line Sharpness",  Range(0.5, 10))   = 5.0
        _StrokeTaper   ("Stroke Taper",    Range(0, 1))      = 0.65

        // Required by Unity UI system for stencil masking (Mask / RectMask2D)
        [HideInInspector] _StencilComp     ("Stencil Comparison",  Float) = 8
        [HideInInspector] _Stencil         ("Stencil ID",          Float) = 0
        [HideInInspector] _StencilOp       ("Stencil Operation",   Float) = 0
        [HideInInspector] _StencilReadMask ("Stencil Read Mask",   Float) = 255
        [HideInInspector] _StencilWriteMask("Stencil Write Mask",  Float) = 255
        [HideInInspector] _ColorMask       ("Color Mask",          Float) = 15
        [HideInInspector] _UseUIAlphaClip  ("Use Alpha Clip",      Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
            "RenderPipeline"    = "UniversalPipeline"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    [unity_GUIZTestMode]
        Blend    One OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "UIWoodGrain"

            HLSLPROGRAM
            #pragma vertex   UIWoodGrainVert
            #pragma fragment UIWoodGrainFrag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UIWoodGrainPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "UI/Default"
}
