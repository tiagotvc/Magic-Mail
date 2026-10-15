Shader "RoyalAves/WorldMap/Shadow"
{
    // Sombra de contato: elipse macia presa ao chao, so existe sobre terreno.
    Properties { _Color ("Cor", Color) = (0.21,0.10,0.35,0.30) }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite Off
            Cull Off
            Offset -2, -2
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };
            V vert(A i) { V o; o.pos = TransformObjectToHClip(i.pos.xyz); o.uv = i.uv; return o; }
            half4 frag(V i) : SV_Target
            {
                float d = length(i.uv * 2 - 1);
                half a = saturate(1 - d);
                a = a * a * (3 - 2 * a);
                return half4(_Color.rgb, _Color.a * a);
            }
            ENDHLSL
        }
    }
}
