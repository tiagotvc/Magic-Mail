Shader "RoyalAves/WorldMap/Glow"
{
    Properties {_Color("Luz",Color)=(1,.7,.2,.25)}
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent"}
        Pass
        {
            Blend SrcAlpha One, Zero One
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            half4 _Color;
            struct A{float4 position:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target{float r=length((i.uv-.5)*2);return half4(_Color.rgb*1.3,_Color.a*pow(saturate(1-r),2));}
            ENDHLSL
        }
    }
}
