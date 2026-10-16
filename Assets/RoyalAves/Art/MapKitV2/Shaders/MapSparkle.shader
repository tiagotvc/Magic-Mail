Shader "RoyalAves/WorldMap/Sparkle"
{
    // Brilhos em estrela de quatro pontas que piscam; fase/velocidade vem da cor do vertice (r, g).
    Properties { _Color("Luz",Color)=(1,.9,.45,1) _FxTime("Tempo",Float)=0 _Intensity("Intensidade",Float)=1 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+2" "RenderType"="Transparent"}
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color; float _FxTime; float _Intensity;
            CBUFFER_END
            struct A{float4 position:POSITION;float2 uv:TEXCOORD0;half4 col:COLOR;};
            struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;half4 col:COLOR;};
            V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.col=i.col;return o;}
            half4 frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float tw=pow(saturate(sin(_FxTime*(1.2+i.col.g*2.4)+i.col.r*6.2831)*.5+.5),3);
                float cross=exp(-abs(p.x*p.y)*16)*saturate(1-length(p));   // 4 pontas
                float core=exp(-dot(p,p)*14);
                float a=saturate(cross*1.1+core)*tw*_Intensity;
                return half4(_Color.rgb*1.2,a*_Color.a);
            }
            ENDHLSL
        }
    }
}
