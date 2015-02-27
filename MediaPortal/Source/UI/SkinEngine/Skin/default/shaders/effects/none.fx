/*
 * Default. Just sample the texture.
*/ 

float4 PixelEffect(in float2 texcoord, in Texture2D InputTexture, in sampler TextureSampler, in float4 framedata) : COLOR
{
  return InputTexture.Sample(TextureSampler, texcoord);
}
