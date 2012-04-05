float g_centerX = 0.5f;
float g_centerY = 0.5f;
float g_blurAmount = 0.1f;

//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
    float4 c = 0;    
    float2 Center = { g_centerX, g_centerY };

	  texcoord -= Center;

	  for (int i = 0; i < 15; i++)
      {
		  float scale = 1.0 + g_blurAmount * (i / 14.0);
		  c += tex2D(TextureSampler, texcoord * scale + Center);
	  }
   
	  c /= 15;
	  return c;
}