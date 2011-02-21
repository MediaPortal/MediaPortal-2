/*
** Used by Rendering.ImageContext
** This is a simple shader used for rendering images when no additional effects or transformations are applied.
*/
float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

texture   g_texture; // Color texture 
float     g_opacity;
float4    g_imagetransform;
float4    g_framedata;

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
  float2 Texcoord  : TEXCOORD0;
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
  OUT.Color = tex2D(TextureSampler, IN.Texcoord);
  OUT.Color.a *= g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader = compile ps_2_0 RenderPixelShader();
  }
}