float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
texture  g_texture; // Not used
float4   g_solidColor = float4(1.0f, 1.0f, 1.0f, 1.0f);

sampler textureSampler = sampler_state
{
  Texture = <g_texture>;
  MipFilter = NONE;
  MinFilter = NONE;
  MagFilter = NONE;
};

// application to vertex structure
struct a2v
{
  float4 Position  : POSITION0;
  float4 Color     : COLOR0;
  float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct v2p
{
  float4 Position   : POSITION;
};

// pixel shader to frame
struct p2f
{
  float4 Color : COLOR0;
};

void renderVertexShader(in a2v IN, out v2p OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  OUT.Color = g_solidColor;
}

technique simple
{
  pass p0
  {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader  = compile ps_2_0 renderPixelShader();
  }
}
