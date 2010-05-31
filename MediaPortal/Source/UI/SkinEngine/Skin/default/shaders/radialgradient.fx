half4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
half4x4 Transform; 

half2   g_radius = {0.5f, 0.5f};
half2   g_center = {0.5f, 0.5f};
half2   g_focus = {0.5f, 0.5f};
half    g_opacity;
texture g_texture; // Color texture 

sampler textureSampler = sampler_state
{
  Texture = <g_texture>;
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
  half2 v1 = (g_center-g_focus) / g_radius;
  half2 v2 = (pos-g_focus) / g_radius;
  half v2s = dot(v2, v2);
  half dist = v2s / (dot(v1,v2) + sqrt(v2s-pow(dot(half2(v1.y, -v1.x), v2), 2)));
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
  pos = mul(pos, Transform);
  half dist = GetColor(half2(pos.x, pos.y));
  dist = clamp(dist,0,0.9999);
  OUT.Color = tex1D(textureSampler, dist);
  OUT.Color[3] *= g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
