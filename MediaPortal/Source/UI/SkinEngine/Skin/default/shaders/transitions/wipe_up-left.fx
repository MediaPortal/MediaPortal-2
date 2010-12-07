/*
** Linear wipe from bottom-right corner to top-left.
*/ 

float4 Transition(in float mixAB, in float2 relativePos, in float4 colorA, in float4 colorB)
{
	float v = step(1.0 - min(relativePos.x, relativePos.y), mixAB);
	return colorA * (1.0 - v) + colorB * v;
}