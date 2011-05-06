/*
** Used by Controls.Brushes.LinearGradientBrush
** Renders a linear gradient defined by a texture and start/end points.
*/

float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4  g_transform;
texture   g_texture; // Color texture 
float     g_opacity;
float2    g_startpoint = {0.0f, 0.0f};
float2    g_endpoint = {1.0f, 1.0f};

sampler TextureSampler = sampler_state
{
  Texture = <g_texture>;
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

float GetColor(float2 pos)
{
  float2 vPos = pos-g_startpoint;
  float2 vDist = g_endpoint-g_startpoint;
  float dist = dot(vPos, vDist) / dot(vDist, vDist);

  return dist;
}

void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.Texcoord = IN.Texcoord;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  float4 pos = float4(IN.Texcoord.x, IN.Texcoord.y, 0, 1);
  pos = mul(pos, g_transform);
  float dist = GetColor(float2(pos.x, pos.y));
  dist = clamp(dist, 0, 0.9999);

  float4 color = tex1D(TextureSampler, dist);
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
