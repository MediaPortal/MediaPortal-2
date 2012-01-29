float g_horizontalPixelCounts = 80;
float g_verticalPixelCounts   = 80;

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
   float2 brickCounts = { g_horizontalPixelCounts, g_verticalPixelCounts };
   float2 brickSize = 1.0 / brickCounts;

   // Offset every other row of bricks
   float2 offsetuv = texcoord;
   bool oddRow = floor(offsetuv.y / brickSize.y) % 2.0 >= 1.0;
   if (oddRow)
   {
       offsetuv.x += brickSize.x / 2.0;
   }
   
   float2 brickNum = floor(offsetuv / brickSize);
   float2 centerOfBrick = brickNum * brickSize + brickSize / 2;
   float4 color = tex2D(TextureSampler, centerOfBrick);

   return color;
}
