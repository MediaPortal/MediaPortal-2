/*
 * SkinEngine Shader - tile.fx
 * This effect implements general TileBrush functionality. Some simple cases 
 * are handled in an optimised way by tilesimple.fx
*/
float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4  g_transform;
texture   g_texture; // Color texture 
float     g_opacity;
float4    g_textureviewport;
float4x4  g_relativetransform;
float4    g_brushtransform;
float	  g_uoffset;
float	  g_voffset;
int		  g_tileu;
int		  g_tilev;

sampler textureSampler = sampler_state
{
  Texture = <g_texture>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
  AddressU = g_tileu;
  AddressV = g_tilev;
  BorderColor = {1.0, 1.0, 1.0, 0.0};
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
  float2 Texcoord   : TEXCOORD0;
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
  OUT.Texcoord = mul(float4(pos.x, pos.y, 0.0, 1.0), g_transform).xy;
}

float2 wrapTextureCoord(float2 pos, float2 offset, float2 size)
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

  // Discard pixels outside of texture (outside 0-1 range)
  clip(fraction);
  clip(1.0 - fraction);

  // Add wrap factor to get correct sampler tiling in CLAMP and MIRROR modes
  return fraction + wrap;
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  OUT.Color = tex2D(textureSampler, wrapTextureCoord(IN.Texcoord, g_textureviewport.xy, g_textureviewport.zw));
  OUT.Color[3] *= g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
