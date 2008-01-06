float4x4 worldViewProj     : WORLDVIEWPROJ; //our world view projection matrix
float4x4 RelativeTransform ;//: WORLDVIEWPROJ; 
texture  g_texture;                      // Color texture 
float    appTime;                   // App's time in seconds

float    g_opacity;
float2   g_StartPoint={0.0f,0.0f};
float2   g_EndPoint={1.0f,1.0f};
sampler textureSampler =  
sampler_state
{
    Texture = <g_texture>;
    MipFilter = NONE;
    MinFilter = NONE;
    MagFilter = NONE;
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

float GetColor(float2 pos)
{
  float2 Vector1=pos-g_StartPoint;
  float2 Vector2=g_EndPoint-g_StartPoint;
  float dist=dot(Vector1,Vector2)/dot(Vector2,Vector2);

  return dist;
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
  float dist=GetColor( float2(pos.x,pos.y) );
  dist=clamp(dist,0,0.999999);
  OUT.Color = tex1D(textureSampler, dist);
  OUT.Color[3] *=g_opacity;
  //OUT.Color=color;
}

technique simple {
	pass p0 {
		VertexShader = compile vs_2_0 renderVertexShader();
		PixelShader = compile ps_2_0 renderPixelShader();
	}
}
