/*
 * A more complex image sharpening effect.
 *
 * Original shader source: MPC-HC (http://mpc-hc.sourceforge.net/)
*/
#define CenterBias       2.0
#define SampleBias       0.125

float4 PixelEffect(in float2 texcoord, in sampler TextureSampler, in float4 framedata) : COLOR
{
	// definition des pixels : original, flouté, corigé, final
	float4 ori;
	float4 flou;
	float4 cori;
	float4 final;

  float dx = 1.0 / framedata.x;
  float dy = 1.0 / framedata.y;

	// récuppération de la matrice de 9 points
	// [ 1, 2 , 3 ]
	// [ 4,ori, 5 ]
	// [ 6, 7 , 8 ]

	ori = tex2D(TextureSampler, texcoord);
	float4 c1 = tex2D(TextureSampler, texcoord + float2(-dx,-dy));
	float4 c2 = tex2D(TextureSampler, texcoord + float2(0,-dy));
	float4 c3 = tex2D(TextureSampler, texcoord + float2(dx,-dy));
	float4 c4 = tex2D(TextureSampler, texcoord + float2(-dx,0));
	float4 c5 = tex2D(TextureSampler, texcoord + float2(dx,0));
	float4 c6 = tex2D(TextureSampler, texcoord + float2(-dx,dy));
	float4 c7 = tex2D(TextureSampler, texcoord + float2(0,dy));
	float4 c8 = tex2D(TextureSampler, texcoord + float2(dx,dy));

	// calcul image floue (filtre gaussien)
	// pour normaliser les valeurs, il faut diviser par la somme des coef
	// 1/(1+2+1+2+4+2+1+2+1) = 1/ 16 = .0625
	flou = (c1+c3+c6+c8 + 2*(c2+c4+c5+c7)+ 4*ori)*0.0625;

	// soustraction de l'image flou à l'image originale
	cori = 2*ori - flou;

	// détection des contours
	float delta1;
	float delta2;
	float value;

	// par filtre de sobel
	// Gradient horizontal
	//   [ -1, 0 ,1 ]
	//   [ -2, 0, 2 ]
	//   [ -1, 0 ,1 ]
	delta1 =  (c3 + 2*c5 + c8)-(c1 + 2*c4 + c6);

	// Gradient vertical
	//   [ -1,- 2,-1 ]
	//   [  0,  0, 0 ]
	//   [  1,  2, 1 ]
	delta2 = (c6 + 2*c7 + c8)-(c1 + 2*c2 + c3);

	// calcul
	value = sqrt( mul(delta1,delta1) + mul(delta2,delta2) ) ;

	if( value >.3 ) {
		// si contour, sharpen
		final = ori*CenterBias - (c1 + c2 + c3 + c4 + c5 + c6 + c7 + c8 ) * SampleBias;
		return final;
	}

	// sinon, image corrigée
	return cori;
}