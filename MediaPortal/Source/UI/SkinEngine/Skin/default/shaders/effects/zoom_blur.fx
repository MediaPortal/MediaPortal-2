#define CenterX (0.5f)
#define CenterY (0.5f)
#define BlurAmount (0.1f)


//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
    float4 c = 0;    
    float2 Center = { CenterX, CenterY };

	  texcoord -= Center;

	  for (int i = 0; i < 15; i++)
      {
		  float scale = 1.0 + BlurAmount * (i / 14.0);
		  c += tex2D(TextureSampler, texcoord * scale + Center);
	  }
   
	  c /= 15;
	  return c;
}