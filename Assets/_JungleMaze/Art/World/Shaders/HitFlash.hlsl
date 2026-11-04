#ifndef JUNGLEMAZE_HIT_FLASH_INCLUDED
#define JUNGLEMAZE_HIT_FLASH_INCLUDED

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);

CBUFFER_START(UnityPerMaterial)
    half4 _FlashColor;
    half _FlashAmount;
CBUFFER_END

struct Attributes
{
    float3 positionOS : POSITION;
    float4 color : COLOR;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float2 uv : TEXCOORD0;
};

Varyings Vert(Attributes input)
{
    Varyings output;
    output.positionCS = TransformObjectToHClip(input.positionOS);
    output.color = input.color;
    output.uv = input.uv;
    return output;
}

half4 Frag(Varyings input) : SV_Target
{
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
    color.rgb = lerp(color.rgb, _FlashColor.rgb, saturate(_FlashAmount) * _FlashColor.a);
    return color;
}

#endif
