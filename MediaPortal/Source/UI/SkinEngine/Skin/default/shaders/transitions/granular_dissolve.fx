/*
** A granular alpha dissolve. Rectangular areas of the images will dissolve at random intervals.
*/ 

// Number of dissolving squares in the texture
#define SPATIAL_RESOLUTION 40
// How quickly the squares fade out
#define FADE_SPEED 0.2

float4 Transition(in float mixAB, in float2 relativePos, in float4 colorA, in float4 colorB)
{
	// Granulate pos
	relativePos = floor(relativePos * SPATIAL_RESOLUTION) / SPATIAL_RESOLUTION;

	// Generate a psuedo-random number between 0.0 and 1.0 from pos
	float rand = 0.5 + (frac(sin(dot(relativePos, float2(12.9898, 78.233))) * 43758.5453)) * 0.5;

	// Ensure that all squares fade out in time
	rand *= 1.0f - FADE_SPEED;

	// Determine actual alpha blend for this pixel
	float blend = saturate((rand - mixAB) / FADE_SPEED);

	return colorA * blend + colorB * (1.0 - blend);
}