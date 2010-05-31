float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
texture  g_texture; // Color texture 
 
sampler textureSampler = sampler_state
{
  Texture = <g_texture>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
};
                          
// application to vertex structure
struct a2v
{
  float4 Position  : POSITION0;
  float4 Color     : COLOR0;
  float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
  float2 Texcoord1 : TEXCOORD1;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct v2p
{
  float4 Position   : POSITION;
  float4 Color      : COLOR0;
  float2 Texcoord   : TEXCOORD0;
  float2 Texcoord1  : TEXCOORD1;
};

// pixel shader to frame
struct p2f
{
  float4 Color : COLOR0;
};

void renderVertexShader(in a2v IN, out v2p OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.Color = IN.Color;
  OUT.Texcoord = IN.Texcoord;
  OUT.Texcoord1 = IN.Texcoord1;
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  // Calculate the offsets
  float2 Offset;
  Offset.x = 2.0f / float(256);
  Offset.y = 2.0f / float(256);
  // Create the blur
  // By extracting pixels from the texture we can shift them as we extract them.
  float4 top = tex2D(textureSampler, float2(IN.Texcoord.x, IN.Texcoord.y + Offset.y));
  float4 top_left = tex2D(textureSampler, float2(IN.Texcoord.x - Offset.x, IN.Texcoord.y + Offset.y));
  float4 top_right = tex2D(textureSampler, float2(IN.Texcoord.x + Offset.x, IN.Texcoord.y + Offset.y));
  float4 bottom = tex2D(textureSampler, float2(IN.Texcoord.x, IN.Texcoord.y - Offset.y));
  float4 bottom_left = tex2D(textureSampler, float2(IN.Texcoord.x - Offset.x, IN.Texcoord.y - Offset.y));
  float4 bottom_right = tex2D(textureSampler, float2(IN.Texcoord.x + Offset.x, IN.Texcoord.y + Offset.y));
  float4 left = tex2D(textureSampler, float2(IN.Texcoord.x - Offset.x, IN.Texcoord.y));
  float4 right = tex2D(textureSampler, float2(IN.Texcoord.x - Offset.x, IN.Texcoord.y));

  // Extract the color from the texture
  float4 average = (top + bottom + right + left + top_left + top_right + bottom_left + bottom_right) / 8;
  // Color output
  average[3] = IN.Color[3]; // Keep alpha as it is
  OUT.Color = average;
}

technique simple
{
  pass p0
  {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader  = compile ps_2_0 renderPixelShader();
  }
}
