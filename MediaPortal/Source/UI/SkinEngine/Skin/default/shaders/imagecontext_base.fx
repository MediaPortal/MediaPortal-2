/*
** Used by Rendering.ImageContext
** This a partial shader used for rendering images with various effects. In order for it to work it must be assembled 
** with other files containing definitions for:

	float2 PixelTransform(in float2 texcoord);
	float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR;
*/

float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float     g_opacity;
texture   g_texture; // Color texture 
float4    g_imagetransform;
float4    g_framedata; // xy = width, height in pixels. z = time since rendering start in seconds. Max value 5 hours.

sampler TextureSampler = sampler_state
{
  Texture = <g_texture>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
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
  OUT.Texcoord = IN.Texcoord * g_imagetransform.zw - g_imagetransform.xy;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  float2 texcoord = PixelTransform(IN.Texcoord);

  OUT.Color = PixelEffect(texcoord, TextureSampler, g_framedata);
  OUT.Color.a *= g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader = compile ps_2_0 RenderPixelShader();
  }
}