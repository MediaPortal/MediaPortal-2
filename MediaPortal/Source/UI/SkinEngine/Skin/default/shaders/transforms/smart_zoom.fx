// Non-linear stretch horizontal
// Reduce height by 12.5% and crop top/bottom each 6.25% 
// Used for scaling video

float2 PixelTransform(in float2 texcoord)
{
	return float2(texcoord.x - 0.0265 * sin(6.28 * texcoord.x), 0.0625 + 0.875 * texcoord.y);
}
