/**************************************************************************************/
/* Variables comunes */
/**************************************************************************************/

//Matrices de transformacion
float4x4 matWorld; //Matriz de transformacion World
float4x4 matWorldView; //Matriz World * View
float4x4 matWorldViewProj; //Matriz World * View * Projection
float4x4 matInverseTransposeWorld; //Matriz Transpose(Invert(World))

//Textura para DiffuseMap
texture texDiffuseMap;
sampler2D diffuseMap = sampler_state
{
    Texture = (texDiffuseMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture g_txCubeMap;
samplerCUBE cubeMap = sampler_state
{
    Texture = (g_txCubeMap);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

texture texPerlin;
sampler2D perlinNoise = sampler_state
{
    Texture = (texPerlin);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

//Factor de translucidez
float alphaValue = 1;


float time = 0;



float2 random2(float2 input)
{
    return frac(sin(float2(dot(input, float2(127.1, 311.7)), dot(input, float2(269.5, 183.3)))) * 43758.5453);
}


float random(float input)
{
    return frac(sin(dot(input, 127.1)) * 43758.5453);
}



/**************************************************************************************/
/* BlinnPhong */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT_BLINN
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float4 Color : COLOR;
    float2 Texcoord : TEXCOORD0;
    float2 NormalCoord : TEXCOORD1;
};

//Output del Vertex Shader
struct VS_OUTPUT_BLINN
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
    float3 WorldNormal : TEXCOORD1;
    float3 LightVec : TEXCOORD2;
    float3 HalfAngleVec : TEXCOORD3;
    float3 ViewVec : TEXCOORD4;
    float3 WorldPosition : TEXCOORD5;
};

float3 eyePosition; // Posicion camara
float3 lightPosition; // Posicion luz
texture normal_map;
sampler2D normalMap =
sampler_state
{
    Texture = <normal_map>;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};

//Vertex Shader
VS_OUTPUT_BLINN vs_BlinnPhong(VS_INPUT_BLINN input)
{    
    VS_OUTPUT_BLINN output;

	//Proyectar posicion
    output.Position = mul(input.Position, matWorldViewProj);

	//Enviar Texcoord directamente
    output.Texcoord = input.Texcoord;
    
	/* Pasar normal a World-Space
	Solo queremos rotarla, no trasladarla ni escalarla.
	Por eso usamos matInverseTransposeWorld en vez de matWorld */
    output.WorldNormal = mul(input.Normal, matInverseTransposeWorld).xyz;

	//LightVec (L): vector que va desde el vertice hacia la luz. Usado en Diffuse y Specular
    float3 worldPosition = output.Position.xyz;
    output.LightVec = lightPosition - worldPosition;

	//ViewVec (V): vector que va desde el vertice hacia la camara.
    output.ViewVec = eyePosition.xyz - worldPosition;

	//HalfAngleVec (H): vector de reflexion simplificado de Phong-Blinn (H = |V + L|). Usado en Specular
    output.HalfAngleVec = output.ViewVec + output.LightVec;
    
	//Posicion pasada a World-Space
    output.WorldPosition = mul(input.Position, matWorld).xyz;

    return output;
}

//Input del Pixel Shader
struct PS_BLINN
{
    float2 Texcoord : TEXCOORD0;
    float3 WorldNormal : TEXCOORD1;
    float3 LightVec : TEXCOORD2;
    float3 HalfAngleVec : TEXCOORD3;
    float3 ViewVec : TEXCOORD4;
    float3 WorldPosition : TEXCOORD5;
};

float3 lightColor;
float Ka;
float Kd;
float Ks;
float shininess;
float reflection;

//Pixel Shader
float4 ps_BlinnPhong(PS_BLINN input) : COLOR0
{     
	//Normalizar vectores
    float3 Nn = normalize(input.WorldNormal + tex2D(normalMap, input.Texcoord).xyz); // Esto no es asi, pero bueno...
    float3 Ln = normalize(input.LightVec);
    float3 Hn = normalize(input.HalfAngleVec);
    float3 Vn = normalize(input.ViewVec);
    lightColor = normalize(lightColor);

	//Obtener texel de la textura
    float4 texelColor = tex2D(diffuseMap, input.Texcoord);
    
	//Obtener texel de CubeMap
    float3 R = reflect(Vn, Nn);
    float3 reflectionColor = texCUBE(cubeMap, R).rgb;
    
	//Componente Diffuse: N dot L
    float3 n_dot_l = dot(Nn, Ln);
    float3 diffuseLight = lightColor * max(0.0, n_dot_l); //Controlamos que no de negativo

	//Componente Specular: (N dot H)^exp
    float3 n_dot_h = dot(Nn, Hn);
    float3 specularLight = n_dot_l <= 0.0
			? float3(0.0, 0.0, 0.0)
			: lightColor * pow(max(0.0, n_dot_h), shininess);

    return float4(texelColor.rgb * Ka + texelColor.rgb * diffuseLight * Kd + specularLight * Ks + texelColor.rgb * reflectionColor * reflection, texelColor.a);
}

/*
* Technique DIFFUSE_MAP
*/
technique BlinnPhong
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 vs_BlinnPhong();
        PixelShader = compile ps_3_0 ps_BlinnPhong();
    }
}


/**************************************************************************************/
/* Explosion */
/**************************************************************************************/


//Input del Vertex Shader
struct VS_INPUT_EXPLOSION
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT_EXPLOSION
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
};

//Vertex Shader
VS_OUTPUT_EXPLOSION vs_Explosion(VS_INPUT_EXPLOSION input)
{
    VS_OUTPUT_EXPLOSION output;
    
    //input.Position.xyz = input.Position.xyz + input.Normal.xyz * random2(input.Normal.xy).x * 10;
    input.Position.xyz = input.Position.xyz + input.Normal.xyz * random(input.Position.x) * 10;
    input.Position.xyz = lerp(0, input.Position.xyz, sin(time * .5) * 2);
    
	//Proyectar posicion
    output.Position = mul(input.Position, matWorldViewProj);

	//Enviar Texcoord directamente
    output.Texcoord = input.Texcoord;        

    return output;
}

//Input del Pixel Shader
struct PS_EXPLOSION
{
    float2 Texcoord : TEXCOORD0;
};

float4 colorEquipo;
//Pixel Shader
float4 ps_Explosion(PS_EXPLOSION input) : COLOR0
{    
	//Obtener texel de la textura
    float4 texelColor = tex2D(perlinNoise, input.Texcoord);
    
    if (1 / texelColor.r < time)
        discard;
        
    texelColor = lerp(texelColor * normalize(colorEquipo), 0, time * 0.5);
    
    return texelColor;
}

/*
* Technique EXPLOSION
*/
technique Explosion
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 vs_Explosion();
        PixelShader = compile ps_3_0 ps_Explosion();
    }
}


/**************************************************************************************/
/* Pasto */
/**************************************************************************************/

//Input del Vertex Shader
struct VS_INPUT_Pasto
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT_Pasto
{
    float4 Position : POSITION0;
    float2 Texcoord : TEXCOORD0;
};

//Vertex Shader
VS_OUTPUT_Pasto vs_Pasto(VS_INPUT_Pasto input)
{
    VS_OUTPUT_Pasto output;

	//Proyectar posicion
    output.Position = mul(input.Position, matWorldViewProj);

	//Enviar Texcoord directamente
    output.Texcoord = input.Texcoord;

    return output;
}

//Input del Pixel Shader
struct PS_INPUT_Pasto
{
    float2 Texcoord : TEXCOORD0;
};

float nivel = 0; // Altura del pasto. Va del 0 la capa mas baja, al 1 la capa mas alta
//Pixel Shader
float4 ps_Pasto(PS_INPUT_Pasto input) : COLOR0
{
    float4 color = tex2D(diffuseMap, input.Texcoord + sin(time) * nivel * .01f);
    if (color.r <= nivel)
        discard;
    color.r = 0;
    color.b /= 2;
    return color;
}


/*
* Technique Pasto
*/
technique Pasto
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 vs_BlinnPhong();
        //VertexShader = compile vs_3_0 vs_Pasto();
        PixelShader = compile ps_3_0 ps_Pasto();
    }
}

/**************************************************************************************/
/* PostProcess */
/**************************************************************************************/

static const int radius = 7;
static const int kernelSize = 15;
static const float kernel[kernelSize] =
{
    0.000489, 0.002403, 0.009246, 0.02784, 0.065602, 0.120999, 0.174697, 0.197448, 0.174697, 0.120999, 0.065602, 0.02784, 0.009246, 0.002403, 0.000489
};

//Input del Vertex Shader
struct VS_INPUT_POSTPROCESS
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
};

//Output del Vertex Shader
struct VS_OUTPUT_POSTPROCESS
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
};

//Vertex Shader
VS_OUTPUT_POSTPROCESS VSPostProcess(VS_INPUT_POSTPROCESS input)
{
    VS_OUTPUT_POSTPROCESS output;

	// Propagamos la posicion, ya que esta en espacio de pantalla
    output.Position = input.Position;

	// Propagar coordenadas de textura
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}


bool activo = false;
int screenWidth;
int screenHeight;
texture texBloom;
sampler2D bloomSampler = sampler_state
{
    Texture = (texBloom);
    ADDRESSU = WRAP;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};
float4 blurearTextura(sampler2D textura, float2 texCoord)
{
    float4 horizontalSum = float4(0, 0, 0, 1);
    for (float x = 0; x < kernelSize; x++)
    {
        float2 delta = float2((x - radius + 1) / (screenWidth / 2), 0);
        horizontalSum += tex2D(textura, texCoord + delta) * kernel[x];
    }
    float4 verticalSum = float4(0, 0, 0, 1);
    for (float y = 0; y < kernelSize; y++)
    {
        float2 delta = float2(0, (y - radius + 1) / (screenHeight / 2));
        verticalSum += tex2D(textura, texCoord + delta) * kernel[y];
    }
    
    return (horizontalSum + verticalSum) * 0.5;

}
//Pixel Shader
float4 PSPostProcess(VS_OUTPUT_POSTPROCESS input) : COLOR0
{
    float4 tex = tex2D(diffuseMap, input.TextureCoordinates);
    
    float4 bloom = blurearTextura(bloomSampler, input.TextureCoordinates);
    
    return !activo ? (bloom + tex) : tex;
}

technique PostProcess
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 VSPostProcess();
        PixelShader = compile ps_3_0 PSPostProcess();
    }
}

/**************************************************************************************/
/* SplitScreen */
/**************************************************************************************/

texture texPrimerJugador;
sampler2D primerJugador = sampler_state
{
    Texture = (texPrimerJugador);
    ADDRESSU = BORDER;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};
texture texSegundoJugador;
sampler2D segundoJugador = sampler_state
{
    Texture = (texSegundoJugador);
    ADDRESSU = BORDER;
    ADDRESSV = WRAP;
    MINFILTER = LINEAR;
    MAGFILTER = LINEAR;
    MIPFILTER = LINEAR;
};
//Vertex Shader
VS_OUTPUT_POSTPROCESS VSSplitScreen(VS_INPUT_POSTPROCESS input)
{
    VS_OUTPUT_POSTPROCESS output;

	// Propagamos la posicion, ya que esta en espacio de pantalla
    output.Position = input.Position;

	// Propagar coordenadas de textura
    output.TextureCoordinates = input.TextureCoordinates * float2(2, 1);

    return output;
}

//Pixel Shader
float4 PSSplitScreen(VS_OUTPUT_POSTPROCESS input) : COLOR0
{
    return tex2D(primerJugador, input.TextureCoordinates - 1) + tex2D(segundoJugador, input.TextureCoordinates);
}

technique SplitScreen
{
    pass Pass_0
    {
        VertexShader = compile vs_3_0 VSSplitScreen();
        PixelShader = compile ps_3_0 PSSplitScreen();
    }
}