half4x4  worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

half4x4  g_transform;
texture  g_texture; // Color texture 
half     g_opacity;
half2    g_startpoint = {0.0f, 0.0f};
half2    g_endpoint = {1.0f, 1.0f};

sampler textureSampler = sampler_state
{
  Texture = <g_texture>;
  MipFilter = NONE;
  MinFilter = NONE;
  MagFilter = NONE;
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
  half2 vPos = pos-g_startpoint;
  half2 vDist = g_endpoint-g_startpoint;
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
  half4 pos = half4(IN.Texcoord.x, IN.Texcoord.y, 0, 1);
  pos = mul(pos, g_transform);
  half dist = GetColor(half2(pos.x, pos.y));
  dist = clamp(dist, 0, 0.9999);
  OUT.Color = tex1D(textureSampler, dist);
  OUT.Color[3] *= g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
