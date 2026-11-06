Shader "JungleMaze/ZoneOverlay"
{
    Properties
    {
        _MainTex ("UI texture", 2D) = "white" {}
        _FillAlpha ("Danger opacity", Range(0,1)) = 0.4
        _Minimap ("Minimap coordinates", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest Always
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            float4 _Current, _Next, _MapBounds;
            float _FillAlpha, _Minimap;
            struct Input { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct Output { float4 vertex : SV_POSITION; float2 world : TEXCOORD0; };
            Output vert(Input v)
            {
                Output o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = lerp(mul(unity_ObjectToWorld,v.vertex).xy, _MapBounds.xy + v.uv * _MapBounds.zw, _Minimap);
                return o;
            }
            fixed4 frag(Output i) : SV_Target
            {
                float distanceNow = length(i.world - _Current.xy) - _Current.z;
                float pixel = max(length(fwidth(i.world)), .001);
                float danger = _Current.z <= .0001 ? 1 : smoothstep(-pixel, pixel, distanceNow);
                float edge = (1 - smoothstep(pixel, 2.5 * pixel, abs(distanceNow))) * step(.0001, _Current.z);
                fixed4 color = fixed4(.23,.05,.32, danger * _FillAlpha);
                color = lerp(color, fixed4(.86,.37,1,1), edge);
                float2 delta = i.world - _Next.xy;
                float nextEdge = 1 - smoothstep(pixel, 2 * pixel, abs(length(delta) - _Next.z));
                float angle = atan2(delta.y, delta.x) + UNITY_PI;
                float dashed = step(.4, frac(angle / (2 * UNITY_PI) * 64));
                return lerp(color, fixed4(1,1,1,1), nextEdge * dashed * _Next.w);
            }
            ENDCG
        }
    }
}
