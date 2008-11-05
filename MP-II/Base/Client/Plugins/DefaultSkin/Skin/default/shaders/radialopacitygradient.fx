
half44x4 worldViewProj     : WORLDVIEWPROJ; //our world view projection matrix
half4x4 RelativeTransform : WORLDVIEWPROJ; 

half2   g_radius={0.5f,0.5f};
half2   g_center={0.5f,0.5f};
half2   g_focus={0.5f,0.5f};
half    g_opacity;
texture  g_texture;                 // Color texture 
texture  g_alphatex;            // alpha gradient texture 
half    appTime;                   // App's time in seconds


sampler textureSampler = sampler_state
{
    Texture = <g_texture>;
    MipFilter = LINEAR;
    MinFilter = LINEAR;
    MagFilter = LINEAR;
};

sampler alphaSampler = sampler_state
{
    Texture = <g_alphatex>;
    MipFilter = NONE;
    MinFilter = NONE;
    MagFilter = NONE;
};


//application to vertex structure
struct a2v
{
  half4 Position   : POSITION;
  half2 Texcoord   : TEXCOORD0;
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
  half2 v1=(g_center-g_focus)/g_radius;
  half2 v2=(pos-g_focus)/g_radius;
  half v2s=dot(v2,v2);
  half dist= v2s / ( dot(v1,v2) + sqrt( v2s-pow( dot( half2(v1.y,-v1.x),v2),2 ) ) );
  return dist;
}


void renderVertexShader( in a2v IN, out v2p OUT ) 
{
  //getting to position to object space
  OUT.Position = mul(IN.Position, worldViewProj);
  OUT.Texcoord = IN.Texcoord;
}

void renderPixelShader( in v2p IN, out p2f OUT) 
{ 

  half4 pos=half4(IN.Texcoord.x,IN.Texcoord.y,0,1);
  pos=mul(pos, RelativeTransform);
  half dist=GetColor( half2(pos.x,pos.y) );
  dist=clamp(dist,0,0.999999);
  
  OUT.Color = tex2D(textureSampler, IN.Texcoord) ;
  half4 alphaColor = tex1D(alphaSampler, dist);
  OUT.Color[3]=alphaColor[0];
  
}

technique simple {
	pass p0 {
		VertexShader = compile vs_2_0 renderVertexShader();
		PixelShader = compile ps_2_0 renderPixelShader();
	}
}
