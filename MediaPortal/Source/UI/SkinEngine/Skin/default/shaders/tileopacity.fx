/*
 * SkinEngine Shader - tileopacity.fx
 * This effect implements general TileBrush functionality when used as an opacity mask. Some simple cases 
 * are handled in an optimised way by tilesimpleopacity.fx
*/
float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4   g_transform;
float      g_opacity;
float4     g_textureviewport;
float4x4   g_relativetransform;
float4     g_brushtransform;
float	   g_uoffset;
float	   g_voffset;
int		   g_tileu;
int		   g_tilev;
texture    g_texture; // Color texture 
texture    g_alphatex; // Alpha texture 

sampler alphaSampler : register (s1) = sampler_state
{
  Texture = <g_alphatex>;
  MipFilter = NONE;
  MinFilter = POINT;
  MagFilter = POINT;
  AddressU = g_tileu;
  AddressV = g_tilev;
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

float2 wrapTextureCoord(float2 pos, float2 offset, float2 size, out float discard_mul)
{
  float2 fraction;
  float2 wrap;
  float2 isnegative;

  // Convert to viewport coords
  fraction = (pos - offset) / size;
  
  // Split ratio into integer and fractional components
  fraction = modf(fraction, wrap);

  // Map negative fractions to 1.0 - fraction, ensuring 0-1 range
  isnegative = 1.0 - step(float2(0.0, 0.0), fraction);
  fraction += isnegative;

  // Also increment (and make positive) wrap number for negative coords to ensure correct sampler tiling in MIRROR mode
  wrap -= isnegative;

  // Clamp to slightly within viewport to prevent rounding errors creating borderes when tiling
  fraction = clamp(fraction, 0.01, 0.99);

  // Convert back to texture coords
  fraction = fraction*size + offset;

  // Account for texture borders
  isnegative = abs(wrap) % 2;
  fraction += float2(isnegative.x*g_uoffset, isnegative.y*g_voffset);

  // Set discard return if pixel is outside of texture (outside 0-1 range), 0.0f for outside, 1.0f for inside
  discard_mul = step(fraction, float2(0.0, 0.0)) * step(1.0 - fraction, float2(0.0, 0.0));

  // Add wrap factor to get correct sampler tiling in CLAMP and MIRROR modes
  return fraction + wrap;
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  float discard_mul;
  float2 pos = wrapTextureCoord(IN.Texcoord0, g_textureviewport.xy, g_textureviewport.zw, discard_mul);

  OUT.Color = tex2D(textureSampler, IN.Texcoord1);
  OUT.Color[3] = g_opacity * ((1.0 - discard_mul) + discard_mul *  tex2D(alphaSampler, pos)[3]);
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
