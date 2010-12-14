/*
** Used by Controls.Brushes.SolidColorBrush
** Uses a single color as an opacity mask.
*/

float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
texture  g_texture; // Color texture
float4   g_solidcolor = float4(1.0f, 1.0f, 1.0f, 1.0f);

sampler TextureSampler = sampler_state
{
  Texture = <g_texture>;
};
                          
// application to vertex structure
struct VS_Input
{
  float4 Position  : POSITION0;
  float4 Color     : COLOR0;
  float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct VS_Output
{
  float4 Position   : POSITION;
  float4 Color      : COLOR0;
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
  OUT.Texcoord = IN.Texcoord;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  float4 texPos = float4(IN.Texcoord.x, IN.Texcoord.y, 0, 1);
  OUT.Color = tex2D(TextureSampler, float2(texPos.x, texPos.y));

  OUT.Color[3] *= g_solidcolor[3];
}

technique simple
{
  pass p0
  {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader  = compile ps_2_0 RenderPixelShader();
  }
}
