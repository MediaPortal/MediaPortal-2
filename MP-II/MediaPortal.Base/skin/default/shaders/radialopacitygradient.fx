
float4 g_color[6]={ {1.0f,0.0f,0.0f,1.0f},  //red
                    {0.0f,1.0f,0.0f,1.0f},  //green
                    {0.0f,0.0f,1.0f,1.0f},  // blue
                    {0.0f,0.0f,0.0f,0.0f}, // end
                    
                    {0.0f,0.0f,0.0f,0.0f},  //red
                    {0.0f,0.0f,0.0f,0.0f}}; // end
                    
float g_offset[6]={0.0f,0.5f,1.0f, 0.0f,0.0f,0.0f};

float4x4 worldViewProj     : WORLDVIEWPROJ; //our world view projection matrix
float4x4 RelativeTransform : WORLDVIEWPROJ; 

float2   g_radius={0.5f,0.5f};
float2   g_center={0.5f,0.5f};
float2   g_focus={0.5f,0.5f};
int      g_stops=3;
texture  g_texture;                      // Color texture 
float    appTime;                   // App's time in seconds

sampler textureSampler =  
sampler_state
{
    Texture = <g_texture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};
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
  float2 v1=(g_center-g_focus)/g_radius;
  float2 v2=(pos-g_focus)/g_radius;
  float v2s=dot(v2,v2);
  float dist= v2s / ( dot(v1,v2) + sqrt( v2s-pow( dot( float2(v1.y,-v1.x),v2),2 ) ) );
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
  float4 pos=float4(IN.Texcoord.x,IN.Texcoord.y,0,1);
  pos=mul(pos, RelativeTransform);
  float4 color=GetColor( float2(pos.x,pos.y));

  OUT.Color = tex2D(textureSampler, IN.Texcoord) * IN.Color;
  OUT.Color[3]=color[3];
}

technique simple {
	pass p0 {
		VertexShader = compile vs_2_0 renderVertexShader();
		PixelShader = compile ps_2_a renderPixelShader();
	}
}
