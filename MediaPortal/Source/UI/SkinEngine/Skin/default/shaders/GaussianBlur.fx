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
https://github.com/SickheadGames/HL2GLSL/blob/master/tests/BloomPostprocess/GaussianBlur.fx
but modified by FreakyJ @ Team MediaPortal
*/

texture g_texture;
float4x4 worldViewProj : WorldProjection;
float4x4 matViewProjection : ViewProjection;


// Pixel shader applies a one dimensional gaussian blur filter.
// This is used twice by the bloom postprocess, first to
// blur horizontally, and then again to blur vertically.
sampler TextureSampler = sampler_state {
	Texture = <g_texture>;
};
#define SAMPLE_COUNT 15


// application to vertex structure
struct VS_Input
{
	float4 Position  : POSITION0;
	float4 Color     : COLOR0;
	float2 Texcoord  : TEXCOORD0;  // vertex texture coords 
	float2 Texcoord1 : TEXCOORD1;  // vertex texture coords 
};

// vertex shader to pixelshader structure
struct VS_Output
{
	float4 Position   : POSITION;
	float4 Color      : COLOR0;
	float2 Texcoord   : TEXCOORD0;
	float2 Texcoord1  : TEXCOORD1;
};

void RenderVertexShader(in VS_Input IN, out VS_Output OUT)
{
	OUT.Position = mul(IN.Position, worldViewProj);
	OUT.Color = IN.Color;
	OUT.Texcoord = IN.Texcoord;
	OUT.Texcoord1 = IN.Texcoord1;
}


float PI = 3.14159265358979323846;
float EPSILON = 0.0001;

float ComputeGaussian(float n)
{
	float theta = 2.0f + EPSILON; //float.Epsilon;

	return theta = (float)((1.0 / sqrt(2 * PI * theta)) *
		exp(-(n * n) / (2 * theta * theta)));
}


float4 PixelShaderFunction(in VS_Output IN) : COLOR0
{
	float4 c = 0;

	float2 SampleOffsets[SAMPLE_COUNT];
	float SampleWeights[SAMPLE_COUNT];

	// The first sample always has a zero offset.
	float2 initer = { 0.0f, 0.0f };
	SampleWeights[0] = ComputeGaussian(0);
	SampleOffsets[0] = initer;

	// Maintain a sum of all the weighting values.
	float totalWeights = SampleWeights[0];

	// Add pairs of additional sample taps, positioned
	// along a line in both directions from the center.
	for (int i = 0; i < SAMPLE_COUNT / 2; i++)
	{
		// Store weights for the positive and negative taps.
		float weight = ComputeGaussian(i + 1);

		SampleWeights[i * 2 + 1] = weight;
		SampleWeights[i * 2 + 2] = weight;

		totalWeights += weight * 2;


		float sampleOffset = i * 2 + 1.5f;;

		float2 delta = { (1.0f / 512), (1.0f / 512) };
			delta = delta * sampleOffset;

		// Store texture coordinate offsets for the positive and negative taps.
		SampleOffsets[i * 2 + 1] = delta;
		SampleOffsets[i * 2 + 2] = -delta;
	}

	// Normalize the list of sample weightings, so they will always sum to one.
	for (int k = 0; k < SAMPLE_COUNT; k++)
	{
		SampleWeights[k] /= totalWeights;
	}

	// Combine a number of weighted image filter taps.
	for (int l = 0; l < SAMPLE_COUNT; l++)
	{
		c += tex2D(TextureSampler, IN.Texcoord + SampleOffsets[l]) * SampleWeights[l];
	}
	return c;
}
technique GaussianBlur
{
	pass p0
	{
		VertexShader = compile vs_2_0 RenderVertexShader();
		PixelShader = compile ps_2_0 PixelShaderFunction();
	}
}