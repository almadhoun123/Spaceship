// Shared procedural cloud shape, used by the cloud layer (S_Clouds) and by the Earth shader for
// cloud shadows, so both always agree on where the clouds are.
#ifndef SPACESHIPS_CLOUD_NOISE_INCLUDED
#define SPACESHIPS_CLOUD_NOISE_INCLUDED

float CloudHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.zyx + 31.32);
    return frac((p.x + p.y) * p.z);
}

// Smooth value noise, 0..1.
float CloudNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    float3 u = f * f * f * (f * (f * 6 - 15) + 10);
    float a = CloudHash(i),                  b = CloudHash(i + float3(1, 0, 0));
    float c = CloudHash(i + float3(0, 1, 0)), d = CloudHash(i + float3(1, 1, 0));
    float e = CloudHash(i + float3(0, 0, 1)), g = CloudHash(i + float3(1, 0, 1));
    float h = CloudHash(i + float3(0, 1, 1)), k = CloudHash(i + float3(1, 1, 1));
    return lerp(lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y),
                lerp(lerp(e, g, u.x), lerp(h, k, u.x), u.y), u.z);
}

// Fractal noise; octaves finer than a pixel fade out (footprint = how far one pixel spans).
float CloudFbm(float3 p, int octaves, float footprint)
{
    float sum = 0, amp = 0.5, norm = 0, freq = 1;
    [loop]
    for (int o = 0; o < octaves; o++)
    {
        float fade = saturate(1.5 - footprint * freq * 2);
        sum += CloudNoise(p * freq) * amp * fade + 0.5 * amp * (1 - fade);
        norm += amp;
        freq *= 2.03; amp *= 0.5;
        p += float3(17.1, 3.7, 9.4);
    }
    return sum / norm;
}

// Billowy fractal noise: rounded puffs with creases between them.
float CloudBillow(float3 p, int octaves, float footprint)
{
    float sum = 0, amp = 0.5, norm = 0, freq = 1;
    [loop]
    for (int o = 0; o < octaves; o++)
    {
        float fade = saturate(1.5 - footprint * freq * 2);
        float n = 1 - abs(CloudNoise(p * freq) * 2 - 1);
        sum += n * amp * fade + 0.6 * amp * (1 - fade);
        norm += amp;
        freq *= 2.1; amp *= 0.5;
        p += float3(5.3, 11.9, 2.6);
    }
    return sum / norm;
}

float3 CloudRotateY(float3 p, float a)
{
    float s = sin(a), c = cos(a);
    return float3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z);
}

struct CloudParams
{
    float coverage, coverageScale, coverageSoftness;
    float detailScale, warp, puffiness, puffScale, threshold, edgeSoftness;
    float speed, evolve;
    float highScale, highStretch, highThreshold, highSpeed;
};

// High, thin, streaky layer (stretched along the drift direction), 0..1.
float CloudHighDensity(float3 dir, float t, float footprint, CloudParams c)
{
    float3 p = CloudRotateY(dir, t * c.highSpeed);
    float3 q = p * c.highScale;
    q.xz /= c.highStretch;                                  // streaks run around the axis
    float ev = t * c.evolve * 0.7;
    q += (float3(CloudFbm(q * 0.5 + float3(3.1, ev, 7.7), 3, footprint * c.highScale), 0,
                 CloudFbm(q * 0.5 + float3(ev, 5.5, 1.9), 3, footprint * c.highScale)) - 0.5) * 2;
    float d = CloudFbm(q, 5, footprint * c.highScale);
    float sys = CloudFbm(p * c.coverageScale * 0.7 + 41.7, 3, footprint * c.coverageScale);
    return smoothstep(c.highThreshold, c.highThreshold + 0.15, d) * smoothstep(0.35, 0.6, sys);
}

// Low, thick cloud layer density at a direction on the unit sphere (0..1). Also returns a smooth
// noise used to vary thickness inside the clouds.
float CloudLowDensity(float3 dir, float t, int detailOctaves, float footprint, CloudParams c, out float puff)
{
    float3 p = CloudRotateY(dir, t * c.speed);
    // Big systems: where it's cloudy at all.
    float sys = CloudFbm(p * c.coverageScale + 11.3, 4, footprint * c.coverageScale);
    float weather = smoothstep(1 - c.coverage - c.coverageSoftness, 1 - c.coverage + c.coverageSoftness, sys);
    // Gently swirled base shape.
    float3 q = p * c.detailScale;
    float ev = t * c.evolve;
    float wf = footprint * c.detailScale * 0.25;
    float3 warp = float3(CloudFbm(q * 0.25 + float3(1.7, 9.2, ev), 2, wf),
                         CloudFbm(q * 0.25 + float3(8.3, 2.8, -ev), 2, wf),
                         CloudFbm(q * 0.25 + float3(4.1, ev, 3.3), 2, wf)) - 0.5;
    q += warp * c.warp * 4;
    float base = CloudFbm(q + ev * 2, detailOctaves, footprint * c.detailScale);
    // Puffs: billowy noise at a finer scale, which also eats into the edges.
    float billow = CloudBillow(q * c.puffScale + ev * 3, max(detailOctaves - 1, 2), footprint * c.detailScale * c.puffScale);
    float shape = lerp(base, billow, c.puffiness);
    // Smooth noise for thin/thick patches inside the clouds.
    puff = CloudFbm(q * c.puffScale * 0.6 + float3(7.7, 1.3, ev * 2), 3, footprint * c.detailScale * c.puffScale * 0.6);
    // Clear areas push the shape below the threshold; cloudy areas lift it above.
    float v = shape - (1 - weather) * 0.55 + (weather - 0.5) * 0.1;
    return smoothstep(c.threshold, c.threshold + c.edgeSoftness, v);
}

#endif
