
float4 g_color[12]={ {0.0f,0.0f,0.0f,0.0f},  //red
                    {0.0f,0.0f,0.0f,0.0f},  //green
                    {0.0f,0.0f,0.0f,0.0f},  // blue
                    {0.0f,0.0f,0.0f,0.0f}, // end
                    
                    {0.0f,0.0f,0.0f,0.0f},  //red
                    {0.0f,0.0f,0.0f,0.0f},  //green
                    {0.0f,0.0f,0.0f,0.0f},  // blue
                    {0.0f,0.0f,0.0f,0.0f}, // end
                    
                    {0.0f,0.0f,0.0f,0.0f},  //red
                    {0.0f,0.0f,0.0f,0.0f},  //green
                    {0.0f,0.0f,0.0f,0.0f},  // blue
                    {0.0f,0.0f,0.0f,0.0f}}; // end
                    
float g_offset[12]={0.0f,0.0f,0.0f,0.0f ,0.0f,0.0f,0.0f,0.0f ,0.0f,0.0f,0.0f,0.0f};

float4x4 worldViewProj     : WORLDVIEWPROJ; //our world view projection matrix
float4x4 RelativeTransform ;//: WORLDVIEWPROJ; 
float2   g_radius={0.5f,0.5f};
float2   g_center={0.5f,0.5f};
int      g_stops=3;
texture  g_texture;                      // Color texture 
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
  float2 Texcoord1 : TEXCOORD1;  // vertex texture coords 
};

// pixel shader to frame
struct p2f 
{
  float4 Color : COLOR0;
};

float4 GetColor(float2 pos):COLOR
{
  float distx = length(pos - g_center) / g_radius.x;
  float disty = length(pos - g_center) / g_radius.y;
  float dist = sqrt(dot(distx,disty));

  int index=0;
  while (dist >= g_offset[index] && index+1<g_stops)
  {
    index=index+1;
  }
  index =index-1;
  
  float distance=g_offset[index+1]-g_offset[index];
  float off=abs(dist-g_offset[index]);
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
  float2 pos=mul(IN.Texcoord, RelativeTransform);
  float4 color=GetColor(pos);
  OUT.Color=color;
}

technique simple {
	pass p0 {
		VertexShader = compile vs_3_0 renderVertexShader();
		PixelShader = compile ps_3_0 renderPixelShader();
	}
}
