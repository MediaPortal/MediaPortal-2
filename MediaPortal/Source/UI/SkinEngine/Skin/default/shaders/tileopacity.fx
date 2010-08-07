float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4   g_transform;
texture    g_texture; // Color texture 
texture    g_alphatex; // Alpha texture 
float      g_opacity;
float4     g_textureviewport;
float4x4   g_relativetransform;
float4     g_brushtransform;
float	   g_uoffset;
float	   g_voffset;
int		   g_tileu;
int		   g_tilev;

sampler alphaSampler = sampler_state
{
  Texture = <g_alphatex>;
  MipFilter = NONE;
  MinFilter = POINT;
  MagFilter = POINT;
  AddressU = g_tileu;
  AddressV = g_tilev;
  BorderColor = {1.0, 1.0, 1.0, 0.0};
};

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
  float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct v2p
{
  float4 Position   : POSITION;
  float2 Texcoord0   : TEXCOORD0;
  float2 Texcoord1   : TEXCOORD1;
};

// pixel shader to frame
struct p2f
{
  float4 Color : COLOR0;
};

void renderVertexShader(in a2v IN, out v2p OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);

  // Transform vertex coords to place brush texture
  OUT.Texcoord0 = float2(IN.Texcoord.x*g_brushtransform.z, IN.Texcoord.y*g_brushtransform.w) 
                 - g_brushtransform.xy;
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
  wrap = (1.0f - wrap) * isnegative + (1.0 - isnegative) * wrap;

  // Apply relative transform
  fraction = mul(float4(fraction.x, fraction.y, 0.0, 1.0), g_relativetransform).xy;

  // Convert back to texture coords
  fraction = float2(fraction.x*size.x, fraction.y*size.y) + offset;

  // Account for texture borders
  isnegative = wrap % 2;
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
  pos = mul(float4(pos.x, pos.y, 0.0, 1.0), g_transform).xy;

  OUT.Color = tex2D(textureSampler, IN.Texcoord1);
  OUT.Color[3] *= ((1.0 - discard_mul)*g_opacity) + (discard_mul * g_opacity * tex2D(alphaSampler, pos).w);
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
