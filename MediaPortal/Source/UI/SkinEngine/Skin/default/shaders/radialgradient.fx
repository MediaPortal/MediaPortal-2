float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix

float4x4 g_transform;
float4x4 g_relativetransform;
float2   g_radius = {0.5f, 0.5f};
float2   g_center = {0.5f, 0.5f};
float2   g_focus = {0.5f, 0.5f};
float    g_opacity;
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
  float4 Position  : POSITION0;
  float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct v2p
{
  float4 Position   : POSITION;
  float2 Texcoord   : TEXCOORD0;
};

// pixel shader to frame
struct p2f
{
  float4 Color : COLOR0;
};

void renderVertexShader(in a2v IN, out v2p OUT)
{
  OUT.Position = mul(IN.Position, worldViewProj);

  // Apply relative transform
  float4 pos = float4(IN.Texcoord.x, IN.Texcoord.y, 0.0, 1.0);
  pos = mul(pos, g_transform);
  pos = mul(float4(pos.x, pos.y, 0.0, 1.0), g_relativetransform);
  pos.xy = (pos.xy - g_focus) / g_radius;

  OUT.Texcoord = pos.xy;
}

float GetColor(float2 pos)
{
  // Vector between center and focus, relative to radius
  float2 v1 = (g_center-g_focus) / g_radius;
  // Length of v2, squared
  float v2s = dot(pos, pos);
  float dist = v2s / (dot(v1, pos) + sqrt(v2s - pow(dot(float2(v1.y, -v1.x), pos), 2)));
  return dist;
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  OUT.Color = tex1D(textureSampler, GetColor(IN.Texcoord));
  OUT.Color[3] *= g_opacity;
}

technique simple {
  pass p0 {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader = compile ps_2_0 renderPixelShader();
  }
}
