/*
 * Applies a sepia color effect.
*/ 

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{ 
    float4 color = tex2D(TextureSampler, texcoord);
 
    float4 output = color;
    output.r = (color.r * 0.393) + (color.g * 0.769) + (color.b * 0.189);
    output.g = (color.r * 0.349) + (color.g * 0.686) + (color.b * 0.168);    
    output.b = (color.r * 0.272) + (color.g * 0.534) + (color.b * 0.131);
 
    return output;
}