float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
texture	g_texture; // Color texture 
float4 g_scrollpos;
float4 g_textbox;
float4 g_fadeborder;
float4 g_color;
float4 g_alignment;

sampler textureSampler = sampler_state
{
  Texture = <g_texture>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
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
  float4 Position     : POSITION;
  float4 ClipPosition : COLOR0;
  float2 Texcoord     : TEXCOORD0;
};

// pixel shader to frame
struct p2f
{
  float4 Color : COLOR0;
};

void renderVertexShader(in a2v IN, out v2p OUT)
{
  float4 pos = IN.Position;
  // Align text
  pos.x += g_alignment.x + g_alignment.y * pos.z;
  pos.z = g_alignment.z;
  // Apply scroll position
  pos.x += g_scrollpos.x;
  pos.y += g_scrollpos.y;
  // Store for clipping
  OUT.ClipPosition = pos;
  // Offset to layout position
  pos.x += g_textbox.x;
  pos.y += g_textbox.y;
  // Apply transforms
  pos = mul(pos, worldViewProj);

  OUT.Position = pos;
  OUT.Texcoord = IN.Texcoord;
}

void renderPixelShader(in v2p IN, out p2f OUT)
{
  // Clip to textBox
  clip(IN.ClipPosition.xy);
  clip(g_textbox.zw - IN.ClipPosition.xy);

  // Alpha fade at borders
  float a = saturate(IN.ClipPosition.x / g_fadeborder.x)
		  * saturate(IN.ClipPosition.y / g_fadeborder.y)
		  * saturate(-(IN.ClipPosition.x - g_textbox.z) / g_fadeborder.z)
		  * saturate(-(IN.ClipPosition.y - g_textbox.w) / g_fadeborder.w);

  OUT.Color = g_color;
  OUT.Color.a = tex2D(textureSampler, IN.Texcoord) * a;
}

technique simple
{
  pass p0
  {
    VertexShader = compile vs_2_0 renderVertexShader();
    PixelShader  = compile ps_2_0 renderPixelShader();
  }
}
