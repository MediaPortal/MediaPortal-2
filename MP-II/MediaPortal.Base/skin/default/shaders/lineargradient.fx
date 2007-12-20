
float4 g_color[12]={ {1.0f,0.0f,0.0f,1.0f},  //red
                    {0.0f,1.0f,0.0f,1.0f},  //green
                    {0.0f,0.0f,1.0f,1.0f},  // blue
                    {0.0f,0.0f,1.0f,1.0f}, // end
                    
                    {1.0f,0.0f,0.0f,1.0f},  //red
                    {0.0f,1.0f,0.0f,1.0f},  //green
                    {0.0f,0.0f,1.0f,1.0f},  // blue
                    {0.0f,0.0f,1.0f,1.0f}, // end
                    
                    {1.0f,0.0f,0.0f,1.0f},  //red
                    {0.0f,1.0f,0.0f,1.0f},  //green
                    {0.0f,0.0f,1.0f,1.0f},  // blue
                    {0.0f,0.0f,1.0f,1.0f}}; // end
                    
float g_offset[12]={0.0f,0.4f,1.0f,2.0f ,2.0f,2.0f,2.0f,2.0f ,2.0f,2.0f,2.0f,2.0f};

float4x4 worldViewProj : WORLDVIEWPROJ; //our world view projection matrix
texture g_texture;                      // Color texture 
float    appTime;                   // App's time in seconds

//application to vertex structure
struct a2v
{
    float4 Position  : POSITION0;
    float4 Color     : COLOR0;
    float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
    float2 Texcoord1 : TEXCOORD1;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct v2p 
{
  float4 Position   : POSITION;
  float4 Color      : COLOR0;
  float2 Texcoord   : TEXCOORD0;
  float2 Texcoord1  : TEXCOORD1;
};

// pixel shader to frame
struct p2f 
{
  float4 Color : COLOR0;
};

float4 GetColor(float2 pos:TEXCOORD0):COLOR
{
  int index=0;
  while (pos.y >= g_offset[index])
  {
    index=index+1;
  }
  index =index-1;
  
  float distance=g_offset[index+1]-g_offset[index];
  float off=abs(pos.y-g_offset[index]);
  off = off / distance;
  float4 color1=g_color[index];
  float4 color2=g_color[index+1];
  float4 diff=(color2-color1) * off;
  return g_color[index]+diff;
}


void renderVertexShader( in a2v IN, out v2p OUT ) 
{
  //getting to position to object space
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.Color = IN.Color;
  OUT.Texcoord = IN.Texcoord;
  OUT.Texcoord1 = IN.Texcoord1;
}

void renderPixelShader( in v2p IN, out p2f OUT) 
{ 

  float4 color=GetColor(IN.Texcoord.y);
  OUT.Color=color;
}

technique simple {
	pass p0 {
		VertexShader = compile vs_3_0 renderVertexShader();
		PixelShader = compile ps_3_0 renderPixelShader();
	}
}