Shader "Hidden/RoyalAves/PotionMetaballsPreview"
{
    Properties
    {
        _Transparent ("Transparent gameplay overlay", Float) = 0
        _DomainHeight ("Domain height", Float) = 7
        _DomainOffset ("Domain offset", Float) = 1.8
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            float4 _Balls[28];
            float4 _Ink;
            float _Aspect;
            float _Fusion;
            float _Transparent;
            float _DomainHeight;
            float _DomainOffset;

            float4 frag(v2f_img i) : SV_Target
            {
                float2 p = (i.uv - .5) * float2(_DomainHeight * _Aspect, _DomainHeight);
                p.y -= _DomainOffset;
                float field = 0;
                float2 gradient = 0;
                [unroll] for (int b = 0; b < 28; b++)
                {
                    float2 d = p - _Balls[b].xy;
                    float r2 = _Balls[b].z * _Balls[b].z;
                    float f = exp(-2 * dot(d, d) / r2);
                    field += f;
                    gradient += -4 * d / r2 * f;
                }
                float threshold = _Fusion;
                float aa = max(fwidth(field), .008);
                float alpha = smoothstep(threshold - aa, threshold + aa, field);
                float2 normal = -gradient / max(length(gradient), .0001);
                float light = saturate(dot(normal, normalize(float2(-.6, .8))));
                float rim = 1 - smoothstep(threshold, threshold + .6, field);
                float3 liquid = _Ink.rgb * (.65 + .35 * light);
                liquid = lerp(liquid, lerp(_Ink.rgb, float3(1,1,1), .72), rim * light * .8);
                float shine = pow(light, 18) * rim * .65;
                liquid += shine;
                float checker = fmod(floor(p.x) + floor(p.y) + 100, 2);
                float3 background = lerp(float3(.10,.12,.19), float3(.13,.16,.24), checker);
                float grid = max(1 - smoothstep(.0,.012,abs(frac(p.x + .5) - .5)),
                    1 - smoothstep(.0,.012,abs(frac(p.y + .5) - .5)));
                background += grid * .025;
                if (_Transparent > .5) return float4(liquid, alpha);
                return float4(lerp(background, liquid, alpha), 1);
            }
            ENDCG
        }
    }
}
