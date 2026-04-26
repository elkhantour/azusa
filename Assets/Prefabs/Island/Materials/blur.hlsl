#ifndef ISLAND_INCLUDE_INCLUDED
#define ISLAND_INCLUDE_INCLUDED

/**
 * Performs a 5-tap blur on a Texture2D input.
 * Uses TexelSize to ensure the blur radius is resolution-independent.
 */
void MultiTapBlur_float(
    Texture2D Mask, 
    SamplerState Sampler,
    float2 UV,
    float2 TexelSize,
    float BlurAmount,
    out float4 OutBlurred
) {
    // Offset based on actual texel size multiplied by the blur amount
    float2 d = TexelSize * BlurAmount;
    
    // Sample 5 points (Center + 4 Diagonals)
    float m = SAMPLE_TEXTURE2D(Mask, Sampler, UV).r;
    m += SAMPLE_TEXTURE2D(Mask, Sampler, UV + float2(d.x, d.y)).r;
    m += SAMPLE_TEXTURE2D(Mask, Sampler, UV + float2(-d.x, d.y)).r;
    m += SAMPLE_TEXTURE2D(Mask, Sampler, UV + float2(d.x, -d.y)).r;
    m += SAMPLE_TEXTURE2D(Mask, Sampler, UV + float2(-d.x, -d.y)).r;
    
    // Average the results
    float finalMask = m / 5.0;

    // Initialize all channels of the output Vector4
    OutBlurred = float4(finalMask, finalMask, finalMask, 1.0);
}

#endif