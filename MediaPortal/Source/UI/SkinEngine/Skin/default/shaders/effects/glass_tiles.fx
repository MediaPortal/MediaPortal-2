/// <class>GlassTileEffect</class>
/// <description>An effect mimics the look of glass tiles.</description>
//  ------------------------------------------------------------------------------------

// contributed by Fakhruddin Faizal
// http://hdprogramming.blogspot.com/ 
// modifications by Walt Ritscher
// -----------------------------------------------------------------------------------------
// Shader constant register mappings (scalars - float, double, Point, Color, Point3D, etc.)
// -----------------------------------------------------------------------------------------


/// <summary>The approximate number of tiles per row/column.</summary>
/// <minValue>0</minValue>
/// <maxValue>20</maxValue>
///<defaultValue>5</defaultValue>
#define Tiles (5.0f)

/// <summary>The gap width between the tiles.</summary>
/// <minValue>0/minValue>
/// <maxValue>10</maxValue>
///<defaultValue>1</defaultValue>
#define BevelWidth (1.0f)

/// <summary>The offset for the upper left corner of the tiles.</summary>
/// <minValue>0/minValue>
/// <maxValue>3</maxValue>
///<defaultValue>1</defaultValue>
#define Offset (1.0f)

/// <summary>The color for the gap between the tiles.</summary>
/// <minValue>0/minValue>
/// <maxValue>10</maxValue>
///<defaultValue>1</defaultValue>
#define GroutColor (0x000000)

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
	float2 newUV1;
	newUV1.xy = texcoord.xy + tan((Tiles*2.5)*texcoord.xy + Offset)*(BevelWidth/100);
	
	float4 c1 = tex2D(TextureSampler, newUV1); 
	if(newUV1.x<0 || newUV1.x>1 || newUV1.y<0 || newUV1.y>1)
	{	
	  c1 = GroutColor;
	}
	c1.a=1;
	return c1;
}

