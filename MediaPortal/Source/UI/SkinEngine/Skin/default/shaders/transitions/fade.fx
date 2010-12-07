/* 
** Fade A out to 0 alpha, fade B in.
*/

float4 Transition(in float mixAB, in float2 relativePos, in float4 colorA, in float4 colorB)
{
	return colorA * saturate((0.5 - mixAB) * 2.0) + colorB * saturate((mixAB - 0.5) * 2.0);
}