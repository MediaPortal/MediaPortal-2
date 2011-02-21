/*
** Used by Rendering.ImageContext
** This a partial shader used for rendering transitions between images with various effects. In order for it to work it must be assembled 
** with other files containing definitions for:

	float2 PixelTransform(in float2 texcoord);
	float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR;
	float4 Transition(in float mixAB, in float2 relativePos, in float4 colorA, in float4 colorB);
*/

float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
float     g_opacity;
// Parameters for 'old' frame A
texture   g_textureA; 
float4    g_imagetransformA;
float4    g_framedataA; // xy = width, height in pixels. z = time since rendering start in seconds. Max value 5 hours.
// Parameters for 'new' frame B
texture   g_texture; 
float4    g_imagetransform;
float4    g_framedata; // xy = width, height in pixels. z = time since rendering start in seconds. Max value 5 hours.
// Transition control value 0.0 = A, 1.0 = B.
float	    g_mixAB; 

sampler TextureSamplerA = sampler_state
{
  Texture = <g_textureA>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
  AddressU = BORDER;
  AddressV = BORDER;
};

sampler TextureSamplerB = sampler_state
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
  float2 TexcoordA : TEXCOORD0;
  float2 TexcoordB : TEXCOORD1;
  float2 OriginalTexcoord : TEXCOORD2;
};

// pixel shader to frame
struct PS_Output
{
  float4 Color : COLOR0;
};

void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.TexcoordA = IN.Texcoord * g_imagetransformA.zw - g_imagetransformA.xy;
  OUT.TexcoordB = IN.Texcoord * g_imagetransform.zw - g_imagetransform.xy;
  OUT.OriginalTexcoord = IN.Texcoord;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  float2 texcoordA = PixelTransform(IN.TexcoordA);
  float2 texcoordB = PixelTransform(IN.TexcoordB);

  float4 colorA = PixelEffect(texcoordA, TextureSamplerA, g_framedataA);
  float4 colorB = PixelEffect(texcoordB, TextureSamplerB, g_framedata);

  OUT.Color = Transition(g_mixAB, IN.OriginalTexcoord, colorA, colorB);
  OUT.Color.a *= g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader = compile ps_2_0 RenderPixelShader();
  }
}