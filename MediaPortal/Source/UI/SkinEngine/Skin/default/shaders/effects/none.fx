/*
 * Default. Just sample the texture.
*/ 

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
	return tex2D(TextureSampler, texcoord);
}
