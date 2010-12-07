/*
** Linear wipe from top-left corner to bottom-right.
*/ 

float4 Transition(in float mixAB, in float2 relativePos, in float4 colorA, in float4 colorB)
{
	float v = step(max(relativePos.x, relativePos.y), mixAB);
	return colorA * (1.0 - v) + colorB * v;
}