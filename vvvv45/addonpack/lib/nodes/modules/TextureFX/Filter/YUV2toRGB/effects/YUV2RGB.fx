//@author: Elliot Woods
//@help: Converts a YUV2 4-byte texture stored as RGBA to an RGB texture
//@tags: template, basic
//@credits:

// --------------------------------------------------------------------------------------------------
// PARAMETERS:
// --------------------------------------------------------------------------------------------------

//texture
texture Tex <string uiname="Texture";>;
sampler Samp = sampler_state    //sampler for doing the texture-lookup
{
    Texture   = (Tex);          //apply a texture to the sampler
    MipFilter = POINT;         //sampler states
    MinFilter = POINT;
    MagFilter = POINT;
};

//the data structure: vertexshader to pixelshader
//used as output data with the VS function
//and as input data with the PS function
struct vs2ps
{
    float4 Pos : POSITION;
    float4 TexCd : TEXCOORD0;
};

float4x4 tWVP : WORLDVIEWPROJECTION;

// --------------------------------------------------------------------------------------------------
// VERTEXSHADERS
// --------------------------------------------------------------------------------------------------

vs2ps VS(
    float4 Pos : POSITION,
    float4 TexCd : TEXCOORD0)
{
    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;

    //transform position
    Out.Pos = mul(Pos, tWVP);

    //transform texturecoordinates
    Out.TexCd = TexCd;

    return Out;
}

// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

int CompressedWidth = 1920/2;
float4 PS(vs2ps In): COLOR
{
	int width = CompressedWidth * 2;
	int pixel = lerp(0, width, In.TexCd.x);
	bool rightPixel = pixel % 2 == 0;
	
    float4 uyvy = tex2D(Samp, In.TexCd);
	float y1 = uyvy.a;
	float y2 = uyvy.g;
	float u = uyvy.b;
	float v = uyvy.r;
	
	float y = rightPixel ? y2 : y1;
	
	float c = y - (16.0f / 256.0f);
	float d = u - 0.5f;
	float e = v - 0.5;
	
	float4 col;
	col.r = 1.164383 * c + 1.596027 * e;
	col.g = 1.164383 * c - (0.391762 * d) - (0.812968 * e);
	col.b = 1.164383 * c +  2.017232 * d;
	
	//col.rgb = rightPixel;
	col.a = 1.0f;
	
    return col;
}

// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique TYUV2toRGB
{
    pass P0
    {
        //Wrap0 = U;  // useful when mesh is round like a sphere
        VertexShader = compile vs_2_0 VS();
        PixelShader = compile ps_2_0 PS();
    }
}