#ifndef WOOD_GRAIN_BW_PASS_INCLUDED
#define WOOD_GRAIN_BW_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    half4  _Color;
    half4  _RendererColor;
    half4  _LineColor;
    half4  _LightWood;
    half4  _DarkWood;
    float  _GrainScale;
    float  _GrainStrength;
    float  _RingFrequency;
    float  _NoiseStrength;
    float  _GrainAngle;
    float  _FineGrainScale;
    float  _FineGrainStrength;
    float  _LineThreshold;
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
// Noise helpers (shared with WoodGrainPass)
// ---------------------------------------------------------------------------

float2 Hash2BW(float2 p)
{
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
}

float GradientNoiseBW(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);

    return lerp(
        lerp(dot(Hash2BW(i + float2(0, 0)), f - float2(0, 0)),
             dot(Hash2BW(i + float2(1, 0)), f - float2(1, 0)), u.x),
        lerp(dot(Hash2BW(i + float2(0, 1)), f - float2(0, 1)),
             dot(Hash2BW(i + float2(1, 1)), f - float2(1, 1)), u.x),
        u.y);
}

float FBMBW(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; i++)
    {
        v += a * GradientNoiseBW(p);
        p *= float2(2.17, 1.97);
        a *= 0.5;
    }
    return v;
}

// ---------------------------------------------------------------------------
// Vertex shader
// ---------------------------------------------------------------------------

Varyings WoodGrainBWVert(Attributes v)
{
    Varyings o;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
    o.uv         = TRANSFORM_TEX(v.uv, _MainTex);
    o.color      = v.color * _Color * _RendererColor;
    return o;
}

// ---------------------------------------------------------------------------
// Fragment shader
// ---------------------------------------------------------------------------

half4 WoodGrainBWFrag(Varyings i) : SV_Target
{
    half4 spriteSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

    clip(spriteSample.a - 0.005);

    // Luminance of the B&W sprite: 0 = black line, 1 = white fill
    half lum = dot(spriteSample.rgb, half3(0.299, 0.587, 0.114));

    // Smooth mask: 0 = line pixel, 1 = fill pixel
    half isFill = smoothstep(0.0, _LineThreshold, lum);

    // --- Wood grain (same algorithm as WoodGrainPass) ---
    float angleRad = _GrainAngle * (PI / 180.0);
    float cosA = cos(angleRad);
    float sinA = sin(angleRad);
    float2 centeredUV = i.uv - 0.5;
    float2 woodUV = float2(centeredUV.x * cosA - centeredUV.y * sinA,
                            centeredUV.x * sinA + centeredUV.y * cosA);
    woodUV = (woodUV + 0.5) * _GrainScale;

    float distX = FBMBW(woodUV * 0.6 + float2(1.7, 9.2));
    float distY = FBMBW(woodUV * 0.6 + float2(8.3, 2.8));
    float2 distortedUV = woodUV + float2(distX, distY) * _NoiseStrength;

    float rings = sin(distortedUV.y * _RingFrequency * TWO_PI);
    rings = rings * 0.5 + 0.5;
    rings = smoothstep(0.1, 0.9, rings);
    rings = pow(rings, 1.3);

    float2 fineUV = float2(distortedUV.x * _FineGrainScale,
                            distortedUV.y * (_FineGrainScale * 0.05));
    float fineGrain = FBMBW(fineUV) * 0.5 + 0.5;

    float woodPattern = lerp(rings, fineGrain, _FineGrainStrength);

    // Flat fill color blended with grain
    half4 flatWood  = lerp(_DarkWood, _LightWood, 0.6); // mid-tone base
    half4 grainWood = lerp(_DarkWood, _LightWood, woodPattern);
    half4 woodColor = lerp(flatWood, grainWood, _GrainStrength);

    // Composite: line pixels use _LineColor, fill pixels use wood grain
    half4 result;
    result.rgb = lerp(_LineColor.rgb, woodColor.rgb, isFill);
    result.a   = spriteSample.a;

    result *= i.color;

    // Premultiply alpha
    result.rgb *= result.a;
    return result;
}

#endif // WOOD_GRAIN_BW_PASS_INCLUDED
