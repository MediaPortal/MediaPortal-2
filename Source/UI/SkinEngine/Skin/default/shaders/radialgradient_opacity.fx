/*
** Used by Controls.Brushes.RadialGradientBrush
** Uses a radial gradient defined by a texture, radius, focus and gradient as an opacity mask for a texture.
*/

float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4  g_transform;
float4x4 g_relativetransform;
float2    g_radius = {0.5f, 0.5f};
float2    g_center = {0.5f, 0.5f};
float2    g_focus = {0.5f, 0.5f};
float     g_opacity;
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
  float4 Position   : POSITION;
  float2 Texcoord   : TEXCOORD0;
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
  float2 v1 = (g_center-g_focus) / g_radius;
  float2 v2 = (pos-g_focus) / g_radius;
  float v2s = dot(v2, v2);
  float dist = v2s / (dot(v1, v2) + sqrt(v2s-pow(dot(float2(v1.y, -v1.x), v2), 2)));
  return dist;
}

void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);

  // Apply relative transform
  float4 pos = float4(IN.Texcoord.x, IN.Texcoord.y, 0.0, 1.0);
  pos = mul(pos, g_transform);
  pos = mul(float4(pos.x, pos.y, 0.0, 1.0), g_relativetransform);
  pos.xy = (pos.xy - g_focus) / g_radius;

  OUT.Texcoord = pos.xy;
}

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  OUT.Color = tex2D(TextureSampler, float2(IN.Texcoord.x, IN.Texcoord.y));

  float4 alphaPos = float4(
      (IN.Texcoord.x - g_lowervertsbounds.x)/(g_uppervertsbounds.x - g_lowervertsbounds.x),
      (IN.Texcoord.y - g_lowervertsbounds.y)/(g_uppervertsbounds.y - g_lowervertsbounds.y), 0, 1);
  alphaPos = mul(alphaPos, g_transform);
  float dist = GetColor(float2(alphaPos.x, alphaPos.y));
  dist = clamp(dist, 0, 0.9999);

  float4 alphaColor = tex1D(AlphaSampler, dist);
  OUT.Color[3] *= alphaColor[3] * g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader = compile ps_2_0 RenderPixelShader();
  }
}
