#ifndef UI_WOOD_GRAIN_PASS_INCLUDED
#define UI_WOOD_GRAIN_PASS_INCLUDED

// UI Wood Grain — per-stroke arc approach.
//
// Each grain line is evaluated independently: it has its own Y position,
// its own arc curve (primary + secondary wave for S-curve feel), its own
// X start/end, and an optional mid-line gap.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    half4  _Color;
    float  _GrainScale;
    float  _GrainAngle;
    float  _RingFrequency;
    float  _MaxStrokes;
    float  _NoiseStrength;
    float  _GrainStrength;
    float  _LineDarkness;
    float  _LineScale;
    float  _LineSharpness;
    float  _StrokeLength;
    float  _StrokeTaper;
CBUFFER_END

struct Attributes
{
    float4 positionOS : POSITION;
    float4 color      : COLOR;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half4  color      : COLOR;
    float2 uv         : TEXCOORD0;
    UNITY_VERTEX_OUTPUT_STEREO
};

// ---------------------------------------------------------------------------
// Per-stroke hash — no texture lookup needed.
// ---------------------------------------------------------------------------

float Hash1(float n)
{
    return frac(sin(n * 127.371 + 0.573) * 43758.545);
}

// ---------------------------------------------------------------------------
// Vertex shader
// ---------------------------------------------------------------------------

Varyings UIWoodGrainVert(Attributes v)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
    o.color      = v.color * _Color;
    return o;
}

// ---------------------------------------------------------------------------
// Fragment shader
// ---------------------------------------------------------------------------

half4 UIWoodGrainFrag(Varyings i) : SV_Target
{
    half4 spriteSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    clip(spriteSample.a - 0.005);

    // Rotate UVs and lift into grain space.
    float2 centeredUV = i.uv - 0.5;
    float  rad  = _GrainAngle * (PI / 180.0);
    float  cosA = cos(rad), sinA = sin(rad);
    float2 grainUV = float2(
        centeredUV.x * cosA - centeredUV.y * sinA,
        centeredUV.x * sinA + centeredUV.y * cosA);
    grainUV = (grainUV + 0.5) * _GrainScale;

    // Grain-space spacing and base line half-width.
    float spacing = _GrainScale / _RingFrequency;
    float halfW   = spacing * 0.025 * _LineScale / max(_LineSharpness * 0.4, 0.4);

    // -----------------------------------------------------------------------
    // Per-stroke arc evaluation.
    // Hard upper bound is 20; _MaxStrokes gates which strokes contribute.
    // -----------------------------------------------------------------------
    float lineContrib = 0.0;

    UNITY_LOOP
    for (int li = 0; li < 60; li++)
    {
        float fi     = float(li);
        float active = (1.0 - step(_MaxStrokes, fi));

        float h1 = Hash1(fi * 1.0 + 10.0);
        float h2 = Hash1(fi * 2.0 + 20.0);
        float h3 = Hash1(fi * 3.0 + 30.0);
        float h4 = Hash1(fi * 4.0 + 40.0);
        float h5 = Hash1(fi * 5.0 + 50.0);
        float h6 = Hash1(fi * 6.0 + 60.0);
        float h7 = Hash1(fi * 7.0 + 70.0);

        // Y center: distributed across grain height with per-stroke jitter.
        float strokeY = (fi + 0.15 + h1 * 0.7) * spacing;

        // Arc shape: primary wave + smaller secondary for S-curve feel.
        float amp1   = spacing * (h2 * 0.35 + 0.15) * _NoiseStrength;
        float freq1  = h3 * 0.30 + 0.08;
        float phase1 = h4 * TWO_PI;
        float amp2   = amp1 * (h5 * 0.25 + 0.10);
        float freq2  = freq1 * (h6 * 0.8 + 1.3);
        float phase2 = h5 * TWO_PI;

        float strokeYatX = strokeY
            + sin(grainUV.x * freq1 * TWO_PI + phase1) * amp1
            + sin(grainUV.x * freq2 * TWO_PI + phase2) * amp2;

        // X extent: each stroke has a random centre and a length controlled
        // by _StrokeLength. A small per-stroke length jitter keeps them varied.
        float strokeLen  = _StrokeLength * (h6 * 0.4 + 0.8); // ±20% jitter
        float xCenter    = h2 * (1.0 - strokeLen) + strokeLen * 0.5;
        float xStart     = xCenter - strokeLen * 0.5;
        float xEnd       = xCenter + strokeLen * 0.5;
        float xFade      = strokeLen * 0.12; // fade is proportional to length
        float xEnv       = smoothstep(xStart,        xStart + xFade, i.uv.x)
                         * smoothstep(xEnd,           xEnd   - xFade, i.uv.x);

        // Distance → smooth stroke, with taper (thicker at centre, thin at ends).
        float dist        = abs(grainUV.y - strokeYatX);
        float strokeHalfW = halfW * (h1 * 0.4 + 0.8);
        // Normalised position along the stroke [0..1]; sine peaks at the middle.
        float xNorm       = saturate((i.uv.x - xStart) / max(strokeLen, 0.001));
        float taperEnv    = sin(xNorm * PI);
        // ~50% of strokes get the taper; the rest stay uniform.
        float hasTaper    = step(0.5, h7);
        strokeHalfW      *= 1.0 + taperEnv * _StrokeTaper * 6.0 * hasTaper;
        float stroke      = 1.0 - smoothstep(0.0, strokeHalfW, dist);
        stroke            = pow(stroke, 1.4);

        // Mid-stroke gap removed — short strokes don't need it.
        float gapMask = 1.0;

        // Per-stroke darkness variation.
        float intensity = h1 * 0.30 + 0.70;

        lineContrib = max(lineContrib, stroke * xEnv * gapMask * intensity * active);
    }

    // -----------------------------------------------------------------------
    // Apply darkening to sprite color.
    // -----------------------------------------------------------------------
    half3 darkened   = spriteSample.rgb * _LineDarkness;
    half3 grainedRGB = lerp(spriteSample.rgb, darkened, lineContrib * _GrainStrength);

    half4 result;
    result.rgb = grainedRGB * i.color.rgb;
    result.a   = spriteSample.a * i.color.a;
    result.rgb *= result.a;
    return result;
}

#endif // UI_WOOD_GRAIN_PASS_INCLUDED
