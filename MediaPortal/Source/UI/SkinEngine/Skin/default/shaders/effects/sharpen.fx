/*
 * An image sharpening effect.
 *
 * Original shader source: MPC-HC (http://mpc-hc.sourceforge.net/)
 */

#define effect_width (1.6) 
#define CenterBias (2.0) 
#define SampleBias (-0.125) 

float4 PixelEffect(in float2 texcoord, in Texture2D InputTexture, in sampler TextureSampler, in float4 framedata) : COLOR
{
  float dx = effect_width / framedata.x;    // framedata.x = texture width
  float dy = effect_width / framedata.y;    // framedata.y = texture height

  float4 c1 = InputTexture.Sample(TextureSampler, texcoord + float2(-dx, -dy)) * SampleBias;
  float4 c2 = InputTexture.Sample(TextureSampler, texcoord + float2(0, -dy)) * SampleBias;
  float4 c3 = InputTexture.Sample(TextureSampler, texcoord + float2(-dx, 0)) * SampleBias;
  float4 c4 = InputTexture.Sample(TextureSampler, texcoord + float2(dx, 0)) * SampleBias;
  float4 c5 = InputTexture.Sample(TextureSampler, texcoord + float2(0, dy)) * SampleBias;
  float4 c6 = InputTexture.Sample(TextureSampler, texcoord + float2(dx, dy)) * SampleBias;
  float4 c7 = InputTexture.Sample(TextureSampler, texcoord + float2(-dx, +dy)) * SampleBias;
  float4 c8 = InputTexture.Sample(TextureSampler, texcoord + float2(+dx, -dy)) * SampleBias;
  float4 c9 = InputTexture.Sample(TextureSampler, texcoord) * CenterBias;

  float4 c0 = (c1 + c2 + c3 + c4 + c5 + c6 + c7 + c8 + c9);

  return c0;
}
