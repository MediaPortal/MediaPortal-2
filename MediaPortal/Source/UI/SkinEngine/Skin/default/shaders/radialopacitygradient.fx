half44x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

half4x4  g_transform; 
half2    g_radius = {0.5f, 0.5f};
half2    g_center = {0.5f, 0.5f};
half2    g_focus = {0.5f, 0.5f};
half     g_opacity;
half2    g_uppervertsbounds;
half2    g_lowervertsbounds;
texture  g_texture; // Color texture 
texture  g_alphatex; // Alpha gradient texture 

sampler textureSampler = sampler_state
{
  Texture = <g_texture>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
};

sampler alphaSampler = sampler_state
{
  Texture = <g_alphatex>;
  MipFilter = NONE;
  MinFilter = NONE;
  MagFilter = NONE;
};

// application to vertex structure
struct a2v
{
  half4 Position   : POSITION;
  half2 Texcoord   : TEXCOORD0;
};

// vertex shader to pixelshader structure
struct v2p
{
  half4 Position   : POSITION;
  half2 Texcoord   : TEXCOORD0;
};

// pixel shader to frame
struct p2f
{
  half4 Color : COLOR0;
};

half GetColor(half2 pos)
{
  half2 v1 = (g_center-g_focus) / g_radius;
  half2 v2 = (pos-g_focus) / g_radius;
  half v2s = dot(v2, v2);
  half dist = v2s / (dot(v1, v2) + sqrt(v2s-pow(dot(half2(v1.y, -v1.x), v2), 2)));
  return dist;
}

void renderVertexShader(in a2v IN, out v2p OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.Texcoord = IN.Texcoord;
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  half4 texPos = half4(IN.Texcoord.x, IN.Texcoord.y, 0, 1);
  texPos = mul(texPos, g_transform);
  OUT.Color = tex2D(textureSampler, half2(texPos.x, texPos.y));

  half4 alphaPos = half4(
      (IN.Texcoord.x - g_lowervertsbounds.x)/(g_uppervertsbounds.x - g_lowervertsbounds.x),
      (IN.Texcoord.y - g_lowervertsbounds.y)/(g_uppervertsbounds.y - g_lowervertsbounds.y), 0, 1);
  alphaPos = mul(alphaPos, g_transform);
  half dist = GetColor(half2(alphaPos.x, alphaPos.y));
  dist = clamp(dist, 0, 0.9999);

  half4 alphaColor = tex1D(alphaSampler, dist);
  OUT.Color[3] *= alphaColor[3] * g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
