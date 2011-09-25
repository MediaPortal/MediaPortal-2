#define Angle (45.0f)
#define BlurAmount (0.005f)


//--------------------------------------------------------------------------------------
// Pixel Shader
//--------------------------------------------------------------------------------------

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
    float4 c = 0;
    float rad = Angle * 0.0174533f;
    float xOffset = cos(rad);
    float yOffset = sin(rad);

    for(int i=0; i<16; i++)
    {
        texcoord.x = texcoord.x - BlurAmount * xOffset;
        texcoord.y = texcoord.y - BlurAmount * yOffset;
        c += tex2D(TextureSampler, texcoord);
    }
    c /= 16;
    
    return c;
}