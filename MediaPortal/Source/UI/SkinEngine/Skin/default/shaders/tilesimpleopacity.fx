/*
 * SkinEngine Shader - tilesimpleopacity.fx
 * This effect implements certain TileBrush functionality in an optimised way when used as an opacity mask.
 * See the code in TileBrush.RefreshEffectParameters for specific cases.
*/
float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4  g_transform;
texture   g_texture; // Color texture
texture   g_alphatex; // Alpha texture 
float     g_opacity;
float4x4  g_relativetransform;
float4    g_brushtransform;

sampler alphaSampler : register (s1) = sampler_state
{
  Texture = <g_alphatex>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
  AddressU = BORDER;
  AddressV = BORDER;
  BorderColor = {1.0, 1.0, 1.0, 0.0};
};

sampler textureSampler : register (s0) = sampler_state
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
  float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct v2p
{
  float4 Position   : POSITION;
  float2 Texcoord0  : TEXCOORD0;
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

  // Apply relative transform
  float2 pos = mul(float4(IN.Texcoord.x, IN.Texcoord.y, 0.0, 1.0), g_relativetransform).xy;

  // Transform vertex coords to place brush texture
  pos = pos * g_brushtransform.zw - g_brushtransform.xy;

  // Apply other transformation
  OUT.Texcoord0 = mul(float4(pos.x, pos.y, 0.0, 1.0), g_transform).xy;
  OUT.Texcoord1 = IN.Texcoord;
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  OUT.Color = tex2D(textureSampler, IN.Texcoord1);
  OUT.Color[3] *= g_opacity * tex2D(alphaSampler, IN.Texcoord0)[3];
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
