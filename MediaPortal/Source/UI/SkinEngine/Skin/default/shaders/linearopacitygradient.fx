half4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
half4x4  Transform;

float    g_opacity;
half2    g_StartPoint = {0.5f, 0.0f};
half2    g_EndPoint = {0.5f, 1.0f};
half2    g_UpperVertsBounds;
half2    g_LowerVertsBounds;
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
  MinFilter = POINT;
  MagFilter = POINT;
};

// application to vertex structure
struct a2v
{
  half4 Position  : POSITION0;
  half2 Texcoord  : TEXCOORD0;  // vertex texture coords 
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
  half2 vPos = pos-g_StartPoint;
  half2 vDist = g_EndPoint-g_StartPoint;
  half dist = dot(vPos, vDist) / dot(vDist, vDist);

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
  texPos = mul(texPos, Transform);
  OUT.Color = tex2D(textureSampler, half2(texPos.x, texPos.y));

  half4 alphaPos = half4(
      (IN.Texcoord.x - g_LowerVertsBounds.x)/(g_UpperVertsBounds.x - g_LowerVertsBounds.x),
      (IN.Texcoord.y - g_LowerVertsBounds.y)/(g_UpperVertsBounds.y - g_LowerVertsBounds.y), 0, 1);
  alphaPos = mul(alphaPos, Transform);
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
