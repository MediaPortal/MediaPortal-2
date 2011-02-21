/*
** Unused?
** Renders a texture with it's color channels averaged.
*/

float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
texture  g_texture; // Color texture 

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
  float2 Texcoord1 : TEXCOORD1;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct VS_Output
{
  float4 Position   : POSITION;
  float4 Color      : COLOR0;
  float2 Texcoord   : TEXCOORD0;
  float2 Texcoord1  : TEXCOORD1;
};

// pixel shader to frame
struct PS_Output
{
  float4 Color : COLOR0;
};

void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.Color = IN.Color;
  OUT.Texcoord = IN.Texcoord;
  OUT.Texcoord1 = IN.Texcoord1;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  OUT.Color = tex2D(TextureSampler, IN.Texcoord) * IN.Color;
  OUT.Color[0] = (OUT.Color[0] + OUT.Color[1] + OUT.Color[2])/3.0f;
  OUT.Color[1] = OUT.Color[0];
  OUT.Color[2] = OUT.Color[0];
}

technique simple
{
  pass p0
  {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader  = compile ps_2_0 RenderPixelShader();
  }
}
