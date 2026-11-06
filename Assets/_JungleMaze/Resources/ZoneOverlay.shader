Shader "JungleMaze/ZoneOverlay"
{
    Properties
    {
        _MainTex ("UI texture", 2D) = "white" {}
        _FillAlpha ("Danger opacity", Range(0,1)) = 0.62
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
                fixed3 dangerColor = lerp(fixed3(.85, .025, .035), fixed3(.02, .025, .035), _Minimap);
                fixed4 color = fixed4(dangerColor, danger * _FillAlpha);
                float2 delta = i.world - _Next.xy;
                float nextDistance = abs(length(delta) - _Next.z);
                float worldLine = 1 - smoothstep(.35 * pixel, pixel, nextDistance);
                float mapLine = 1 - smoothstep(pixel, 2 * pixel, nextDistance);
                float angle = atan2(delta.y, delta.x) + UNITY_PI;
                float dashed = step(.4, frac(angle / (2 * UNITY_PI) * 64));
                float preview = lerp(worldLine, mapLine * dashed, _Minimap) * _Next.w;
                return lerp(color, fixed4(1, 1, 1, 1), preview);
            }
            ENDCG
        }
    }
}
