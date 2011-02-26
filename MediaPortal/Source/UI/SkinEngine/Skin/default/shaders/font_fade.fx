/*
** Used by Rendering.TextBuffer
** Renders text from a character set packed into a texture. Supports scrolling and clipping to a boundary box.
** Similar to font.fx, but adds an alpha fade at the edges of the bounding box.
*/

float4x4 worldViewProj : WORLDVIEWPROJ; // Our world view projection matrix
texture	g_texture; // Color texture 
float4 g_scrollpos;
float4 g_textbox;
float4 g_fadeborder;
float4 g_color;
float4 g_alignment;

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
  float4 Position     : POSITION;
  float4 ClipPosition : COLOR0;
  float2 Texcoord     : TEXCOORD0;
};

// pixel shader to frame
struct PS_Output
{
  float4 Color : COLOR0;
};

void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
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

void RenderPixelShader(in VS_Output IN, out PS_Output OUT)
{
  // Clip to textBox
  clip(IN.ClipPosition.xy);
  clip(g_textbox.zw - IN.ClipPosition.xy);

  // Alpha fade at borders
  float a = saturate(IN.ClipPosition.x / g_fadeborder.x)
		  * saturate(IN.ClipPosition.y / g_fadeborder.y)
		  * saturate(-(IN.ClipPosition.x - g_textbox.z) / g_fadeborder.z)
		  * saturate(-(IN.ClipPosition.y - g_textbox.w) / g_fadeborder.w);

  // Remember to pre-multiply alpha
  float alpha = g_color.a * tex2D(TextureSampler, IN.Texcoord) * a;
  OUT.Color = float4(g_color.xyz * alpha, alpha);
}

technique simple
{
  pass p0
  {
    VertexShader = compile vs_2_0 RenderVertexShader();
    PixelShader  = compile ps_2_0 RenderPixelShader();
  }
}
