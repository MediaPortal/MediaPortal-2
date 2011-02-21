float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
texture g_texture; // Color texture 

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
  OUT.Color = IN.Color;
  OUT.Texcoord = IN.Texcoord;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  OUT.Color = tex2D(TextureSampler, IN.Texcoord) * IN.Color;
}

technique simple
{
  pass p0
  {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader  = compile ps_2_0 RenderPixelShader();
  }
}
