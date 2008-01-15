float4x4 worldViewProj : WORLDVIEWPROJ; //our world view projection matrix
texture g_texture;                      // Color texture 
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
  float2 Texcoord1  : TEXCOORD1;
};

// pixel shader to frame
struct p2f 
{
  float4 Color : COLOR0;
};
  
// the vertex shader
void renderVertexShader( in a2v IN, out v2p OUT ) 
{
  //getting to position to object space
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.Color = IN.Color;
  OUT.Texcoord = IN.Texcoord;
  OUT.Texcoord1 = IN.Texcoord1;
}

// the pixel shader
void renderPixelShader( in v2p IN, out p2f OUT)
{ 
  OUT.Color = tex2D(textureSampler, IN.Texcoord) * IN.Color;
  OUT.Color[0] = (OUT.Color[0] + OUT.Color[1] + OUT.Color[2])/3.0f;
  OUT.Color[1] = OUT.Color[0];
  OUT.Color[2] = OUT.Color[0];
}


technique simple
{
    pass p0
    {
        vertexshader = compile vs_2_0 renderVertexShader();
        pixelshader  = compile ps_2_0 renderPixelShader();
    }
}