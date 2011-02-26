/*
** Used by Control.Brushes.TileBrush (also ImageBrush, VisualBrush)
** A simpler, more optimised version of tile.fx that doesn't allow cropping or wrapping.
*/

float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4  g_transform;
texture   g_texture; // Color texture 
float     g_opacity;
float4x4  g_relativetransform;
float4    g_textureviewport;
float4    g_brushtransform;

sampler TextureSampler = sampler_state
{
  Texture = <g_texture>;
  AddressU = BORDER;
  AddressV = BORDER;
  BorderColor = {1.0, 1.0, 1.0, 0.0};
};

// application to vertex structure
struct VS_Input
{
  float4 Position  : POSITION0;
  float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct VS_Output
{
  float4 Position   : POSITION;
  float2 Texcoord   : TEXCOORD0;
};

// pixel shader to frame
struct PS_Output
{
  float4 Color : COLOR0;
};

void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);

  // Apply relative transform
  float2 pos = mul(float4(IN.Texcoord.x, IN.Texcoord.y, 0.0, 1.0), g_relativetransform).xy;

  // Transform vertex coords to place brush texture
  pos = pos * g_brushtransform.zw - g_brushtransform.xy;

  // Apply other transformation
  pos = (pos - g_textureviewport.xy) / g_textureviewport.zw;
  pos = mul(float4(pos.x, pos.y, 0.0, 1.0), g_transform).xy;
  OUT.Texcoord = pos * g_textureviewport.zw + g_textureviewport.xy;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  float4 color = tex2D(TextureSampler, IN.Texcoord);
  color.a *= g_opacity;

  // Remember to pre-multiply alpha
  OUT.Color = float4(color.xyz * color.a, color.a);
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader = compile ps_2_0 RenderPixelShader();
  }
}
