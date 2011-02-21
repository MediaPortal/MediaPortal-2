/*
** Used by Controls.Brushes.VideoBrush
** This a partial shader used for rendering video frames with various effects. In order for it to work it must be assembled 
** with other files containing definitions for:

	float2 PixelTransform(in float2 texcoord);
	float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR;
*/

float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4  g_transform;
texture   g_texture; // Color texture 
float     g_opacity;
float4x4  g_relativetransform;
float4    g_imagetransform;
float4    g_framedata; // xy = width, height in pixels. z = time since rendering start in seconds. Max value 5 hours.

sampler TextureSampler = sampler_state
{
  Texture = <g_texture>;
  AddressU = BORDER;
  AddressV = BORDER;
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
  pos = pos * g_imagetransform.zw - g_imagetransform.xy;

  // Apply other transformation
  OUT.Texcoord = mul(float4(pos.x, pos.y, 0.0, 1.0), g_transform).xy;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  float2 texcoord = PixelTransform(IN.Texcoord);

  float4 color = PixelEffect(texcoord, TextureSampler, g_framedata);;
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
