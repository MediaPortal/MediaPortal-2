/*
** Used by Controls.Brushes.LinearGradientBrush
** Uses a linear radient defined by a texture and start/end points as an opacity mask for a texture.
*/

float4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4  g_transform;
float    g_opacity;
float2    g_startpoint = {0.5f, 0.0f};
float2    g_endpoint = {0.5f, 1.0f};
float2    g_uppervertsbounds;
float2    g_lowervertsbounds;
texture  g_texture; // Color texture 
texture  g_alphatex; // Alpha gradient texture 

sampler TextureSampler = sampler_state
{
  Texture = <g_texture>;
};

sampler AlphaSampler = sampler_state
{
  Texture = <g_alphatex>;
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
  float4 texPos = float4(IN.Texcoord.x, IN.Texcoord.y, 0, 1);
  texPos = mul(texPos, g_transform);

  float4 alphaPos = float4(
      (IN.Texcoord.x - g_lowervertsbounds.x)/(g_uppervertsbounds.x - g_lowervertsbounds.x),
      (IN.Texcoord.y - g_lowervertsbounds.y)/(g_uppervertsbounds.y - g_lowervertsbounds.y), 0, 1);
  alphaPos = mul(alphaPos, g_transform);
  float dist = GetColor(float2(alphaPos.x, alphaPos.y));
  dist = clamp(dist, 0, 0.9999);

  // The opacity mask will already be pre-multiplied
  OUT.Color = tex2D(TextureSampler, float2(texPos.x, texPos.y)) * tex1D(AlphaSampler, dist).a * g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader = compile ps_2_0 RenderPixelShader();
  }
}
