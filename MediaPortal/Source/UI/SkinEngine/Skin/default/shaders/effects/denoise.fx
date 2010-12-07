/*
 * Reduces 'noise' in the final image.
 *
 * Original shader source: MPC-HC (http://mpc-hc.sourceforge.net/)
*/

#define CenterBias (1.0)
#define SampleBias (0.125) 
#define effect_width (0.1)

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
	float dx = 0.0f;
	float dy = 0.0f;
 	float fTap = effect_width;
	float4 cAccum = tex2D(TextureSampler, texcoord) * CenterBias;

	for ( int iDx = 0 ; iDx < 16; ++iDx )
	{
		dx = fTap / framedata.x;    // framedata.x = texture width
    dy = fTap / framedata.y;    // framedata.y = texture height

		cAccum += tex2D(TextureSampler, texcoord + float2(-dx,-dy)) * SampleBias;
		cAccum += tex2D(TextureSampler, texcoord + float2(0,-dy)) * SampleBias;
		cAccum += tex2D(TextureSampler, texcoord + float2(-dx,0)) * SampleBias;
		cAccum += tex2D(TextureSampler, texcoord + float2(dx,0)) * SampleBias;
		cAccum += tex2D(TextureSampler, texcoord + float2(0,dy)) * SampleBias;
		cAccum += tex2D(TextureSampler, texcoord + float2(dx,dy)) * SampleBias;
		cAccum += tex2D(TextureSampler, texcoord + float2(-dx,+dy)) * SampleBias;
		cAccum += tex2D(TextureSampler, texcoord + float2(+dx,-dy)) * SampleBias;

		fTap  += 0.1;
	}

	return (cAccum / 16.0);
}