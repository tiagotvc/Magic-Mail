Shader "RoyalAves/WorldMap/Beam"
{
    // Raio de sol cartoon: faixas com bordas suavizadas que afunilam para a fonte (y=1) e abrem no ponto de contato (y=0).
    Properties { _LightColor("Cor",Color)=(1,.92,.55,1) _Alpha("Opacidade",Float)=.5 _Bands("Faixas",Float)=3 _RaySpacing("Ray spacing",Range(0,1))=.5 _FxTime("Tempo",Float)=0 _Seed("Semente",Float)=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+1" "RenderType"="Transparent"}
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off Cull Off ZTest Always   // raios por cima de tudo (como no Candy)
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _LightColor; float _Alpha; float _Bands; float _RaySpacing; float _FxTime; float _Seed;
            CBUFFER_END
            struct A{float4 position:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target
            {
                float y=i.uv.y;
                float hw=lerp(.5,.10,pow(y,.8));                  // afunila
                float u=(i.uv.x-.5)/hw*.5+.5;
                float band=0;
                float aa=max(fwidth(u),.0005);
                [unroll] for(int cell=0;cell<6;cell++)
                {
                    float duty=.30+.35*frac(sin((cell+_Seed)*12.9898)*43758.5453);
                    float center=(cell+.12+duty*.5)/max(_Bands,1);
                    center=.5+(center-.5)*lerp(.65,1,saturate(_RaySpacing));
                    float halfWidth=duty*.5/max(_Bands,1);
                    float coverage=1-smoothstep(halfWidth-aa,halfWidth+aa,abs(u-center));
                    band=max(band,coverage*step(cell+.5,_Bands));
                }
                float fade=smoothstep(0,.16,y)*(1-smoothstep(.72,1,y));
                float pulse=.88+.12*sin(_FxTime*1.2+_Seed+u*4);
                float a=band*fade*pulse*saturate(_Alpha);
                return half4(saturate(_LightColor.rgb),min(a,.6));
            }
            ENDHLSL
        }
    }
}
