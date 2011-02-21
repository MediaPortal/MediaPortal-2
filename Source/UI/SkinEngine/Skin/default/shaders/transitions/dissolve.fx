/*
** Basic alpha dissolve between A/B
*/ 

float4 Transition(in float mixAB, in float2 relativePos,in  float4 colorA, in float4 colorB)
{
	return colorA * (1.0 - mixAB) + colorB * mixAB;
}