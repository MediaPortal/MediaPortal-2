/*
** Used by Rendering.ImageContext
** This is a simple shader used for rendering images when no additional effects or transformations are applied.
*/

Texture2D    InputTexture   : register(t0);

cbuffer constants : register(b0)
{
  float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
  float4x4  g_relativetransform;
  float4    g_imagetransform;
  float4    g_framedata;
  float     g_opacity;
}

sampler TextureSampler = sampler_state
{
  Texture = <InputTexture>;
  AddressU = BORDER;
  AddressV = BORDER;
};

// application to vertex structure to match Direct2D
struct VS_Input
{
  float4 clipSpaceOutput : SV_POSITION;
  float4 Position : SCENE_POSITION;
  float4 Texcoord : TEXCOORD0;
};

// vertex shader to pixelshader structure to match Direct2D
struct VS_Output
{
  float4 clipSpaceOutput : SV_POSITION;
  float4 Position : SCENE_POSITION;
  float4 Texcoord : TEXCOORD0;
};

// pixel shader to frame
struct PS_Output
{
  float4 Color : SV_TARGET;
};


void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
{
  OUT.clipSpaceOutput = IN.clipSpaceOutput;

  OUT.Position = mul(IN.Position, worldViewProj);

  // Apply relative transform
  float2 pos = mul(float4(IN.Texcoord.x, IN.Texcoord.y, 0.0, 1.0), g_relativetransform).xy;

  // Transform vertex coords to place brush texture
  pos = pos * g_imagetransform.zw - g_imagetransform.xy;
  OUT.Texcoord = float4(pos.x, pos.y, 0.0, 1.0);
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  float4 color = InputTexture.Sample(TextureSampler, IN.Texcoord);
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
