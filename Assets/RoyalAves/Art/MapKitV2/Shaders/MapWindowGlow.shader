Shader "RoyalAves/WorldMap/WindowGlow"
{
    Properties{_Color("Luz quente da janela",Color)=(1,.57,.12,.7)}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha One, Zero One
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            half4 _Color;
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A i){V o;o.pos=TransformObjectToHClip(i.pos.xyz);o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target
            {
                float2 p=i.uv-float2(.5,.75);
                float halo=exp(-dot(p/float2(.18,.14),p/float2(.18,.14)));
                return half4(_Color.rgb,_Color.a*halo*.48);
            }
            ENDHLSL
        }
    }
}
