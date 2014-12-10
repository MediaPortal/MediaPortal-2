/*
Copyright (C) 2007-2014 Team MediaPortal
http://www.team-mediaportal.com

This file is part of MediaPortal 2

MediaPortal 2 is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

MediaPortal 2 is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.

Original version is taken from:
http://glslsandbox.com/e#20998.0
but translated to HLSL and modified by FreakyJ @ Team MediaPortal
*/

//**************************************************************//
//  Effect File exported by RenderMonkey 1.6
//
//  - Although many improvements were made to RenderMonkey FX  
//    file export, there are still situations that may cause   
//    compilation problems once the file is exported, such as  
//    occasional naming conflicts for methods, since FX format 
//    does not support any notions of name spaces. You need to 
//    try to create workspaces in such a way as to minimize    
//    potential naming conflicts on export.                    
//    
//  - Note that to minimize resulting name collisions in the FX 
//    file, RenderMonkey will mangle names for passes, shaders  
//    and function names as necessary to reduce name conflicts. 
//**************************************************************//

//--------------------------------------------------------------//
// Default_DirectX_Effect
//--------------------------------------------------------------//
//--------------------------------------------------------------//
// Pass 0
//--------------------------------------------------------------//
//string Default_DirectX_Effect_Pass_0_Model : ModelData = "C:\\Program Files (x86)\\AMD\\RenderMonkey 1.82\\Examples\\Media\\Models\\Sphere.3ds";

texture g_texture;
// worldViewProj is a transform used to map a 1x1 quad
// to fill the screen.
float4x4 worldViewProj : WorldProjection;

float4x4 matViewProjection : ViewProjection;
uniform float time : Time0_X;
uniform float2 mouse : MouseCoordinateXYNDC;
uniform float2 resolution : ViewportDimensions;
uniform float transparency;
uniform float4 backgroundColor; //RGBA

const float AMPLITUDE_1 = 0.3;
const float AMPLITUDE_2 = 0.5;
const float AMPLITUDE_3 = 0.6;

const float STAR_VISIBILITY_FACTOR = 0.001;

const float3 wave1_1 = float3(0.02, 0.03, 0.13);
const float3 wave1_2 = float3(0.03, 0.06, 0.23);
const float3 wave1_3 = float3(0.04, 0.08, 0.26);

const float3 wave3_1 = float3(0.01, 0.01, 0.3);

const float3 wave4_1 = float3(0.02, 0.05, 0.03);
const float3 wave4_2 = float3(0.02, 0.05, 0.03);
const float3 wave4_3 = float3(0.02, 0.05, 0.03);

const float WAVE_OFFSET_SMALL = 5.0;
const float WAVE_OFFSET_MEDIUM = 15.0;

struct VS_INPUT 
{
   float4 Position : POSITION0;
   
};

struct VS_OUTPUT 
{
   float4 Position : POSITION0;
   
};

VS_OUTPUT Default_DirectX_Effect_Pass_0_Vertex_Shader_vs_main( VS_INPUT Input )
{
   VS_OUTPUT Output;

   Output.Position = mul( Input.Position, matViewProjection );
   
   return( Output );
   
}



// shader
  


float4 Default_DirectX_Effect_Pass_0_Pixel_Shader_ps_main(in float2 screenPos : VPOS) : COLOR0
{   
	float2 p = ( screenPos.yx / resolution.yx ) * 2.0 - 0.6;
	p.y += (mouse.x / 5.);
      
	//float3 c = float3(0.0, 0.0, 0.0);
	float3 c = backgroundColor.yyz;
      
	float waveShineFactor = lerp( 0.10, 0.4, 0.3 * sin(time) + 0.5);
	float starShineFactor = lerp( 0.10, 0.4, 1.5 * sin(atan(time * 0.2)) + 0.9);
      
	c += wave1_1 * (  waveShineFactor *        abs( 1.0 / sin( p.x         + sin( p.y + time )  * AMPLITUDE_1 ) ));
	c += wave1_2 * ( (waveShineFactor * 0.4) * abs( 1.0 / sin((p.x + 0.04) + sin( p.y + time )  * AMPLITUDE_1 - 0.01 ) ));
	c += wave1_3 * ( (waveShineFactor * 0.1) * abs( 1.0 / sin((p.x + 0.07) + sin( p.y + time )  * AMPLITUDE_1 - 0.02 ) ));
      
	c += float3(0.05, 0.05, 0.15) * (  waveShineFactor        * abs( 1.0 / sin( p.x + 0.04  + sin( p.y + time + WAVE_OFFSET_SMALL )    * AMPLITUDE_2 ) ));
	c += float3(0.05, 0.05, 0.15) * (  waveShineFactor * 0.4  * abs( 1.0 / sin( p.x + 0.07  + sin( p.y + time + WAVE_OFFSET_SMALL )    * AMPLITUDE_2 - 0.01 ) ));
	c += float3(0.05, 0.05, 0.15) * (  waveShineFactor * 0.3  * abs( 1.0 / sin( p.x + 0.11  + sin( p.y + time + WAVE_OFFSET_SMALL )    * AMPLITUDE_2 - 0.02 ) ));
	c += float3(0.05, 0.05, 0.15) * (  waveShineFactor * 0.2  * abs( 1.0 / sin( p.x + 0.14  + sin( p.y + time + WAVE_OFFSET_SMALL )    * AMPLITUDE_2 - 0.03 ) ));
	c += float3(0.05, 0.05, 0.15) * (  waveShineFactor * 0.1  * abs( 1.0 / sin( p.x + 0.15  + sin( p.y + time + WAVE_OFFSET_SMALL )    * AMPLITUDE_2 - 0.04 ) ));
      
      
	c += wave3_1 * (  waveShineFactor        * abs(  .8 / sin( p.x         + sin( p.y + time         + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
      
	c += wave4_1 * (  waveShineFactor        * abs( 1.0 / sin( p.x         + sin( p.y + sin(time)    + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
	c += wave4_2 * (  waveShineFactor        * abs( 1.0 / sin( p.x         + sin( p.y + sin(time/2.) + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
	c += wave4_3 * (  waveShineFactor        * abs( 1.0 / sin( p.x         + sin( p.y + sin(time/4.) + WAVE_OFFSET_MEDIUM )      * AMPLITUDE_3 ) ));
      
      
	float4 color = float4(c, backgroundColor.w);
      
	return ( color );   
}
//--------------------------------------------------------------//
// Technique Section for Default_DirectX_Effect
//--------------------------------------------------------------//
technique Default_DirectX_Effect
{
   pass Pass_0
   {
      VertexShader = compile vs_2_0 Default_DirectX_Effect_Pass_0_Vertex_Shader_vs_main();
      PixelShader = compile ps_3_0 Default_DirectX_Effect_Pass_0_Pixel_Shader_ps_main();
   }

}

