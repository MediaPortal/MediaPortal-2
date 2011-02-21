/*
 * A simple greyscale effect
 *
 * Original shader source: MPC-HC (http://mpc-hc.sourceforge.net/)
*/

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
  float4 color = tex2D(TextureSampler, texcoord);
	float luminance = dot(color, float4(0.299, 0.587, 0.114, 0));

	return float4(luminance, luminance, luminance, color.a);
}