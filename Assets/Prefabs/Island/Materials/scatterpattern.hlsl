#ifndef ISLAND_INCLUDE_SCATTER_PATTERN
#define ISLAND_INCLUDE_SCATTER_PATTERN

// Helper hash for randomizing grid cells
float2 island_hash22(float2 p) {
    p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
    return frac(sin(p) * 43758.5453123);
}

/**
 * Scatters a detail pattern randomly across a masked area.
 * Uses stochastic tiling to prevent visible repetition.
 */
void ScatterPattern_float(
    Texture2D DetailTex,
    SamplerState Sampler,
    float2 UV,
    float DetailScale,
    float Randomness,
    out float4 OutColor
) {
    // 1. Setup Grid
    float2 uv = UV * DetailScale;
    float2 iuv = floor(uv);
    float2 fuv = frac(uv);

    // 2. Generate Random Transform for this grid cell
    float2 rand = island_hash22(iuv);
    
    // Random Offset
    float2 offset = rand * Randomness;
    
    // Random Rotation (quantized to 90 deg steps for better tiling or free rotate)
    float angle = rand.x * 6.2831;
    float s = sin(angle);
    float c = cos(angle);
    float2x2 rot = float2x2(c, -s, s, c);

    // 3. Apply Transform to UVs
    // Center the UVs before rotating to rotate around the middle of the cell
    float2 detailUV = mul(fuv - 0.5, rot) + 0.5 + offset;

    // 4. Sample Detail Texture
    float detail = SAMPLE_TEXTURE2D(DetailTex, Sampler, detailUV).r;

    // Output as a Vector4 for Shader Graph compatibility
    OutColor = float4(detail, detail, detail, 1.0);
}

#endif