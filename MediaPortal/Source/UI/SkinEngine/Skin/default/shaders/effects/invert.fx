/*
 * Inverts colors.
*/

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
	float4 color = tex2D(TextureSampler, texcoord);

	return float4(1.0 - color.r, 1.0 - color.g, 1.0 - color.b, color.a);
}