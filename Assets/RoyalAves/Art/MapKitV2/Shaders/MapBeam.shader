Shader "RoyalAves/WorldMap/Beam"
{
    // Raio de sol cartoon: faixas de borda dura que afunilam para a fonte (y=1) e abrem no ponto de contato (y=0).
    Properties { _LightColor("Cor",Color)=(1,.92,.55,1) _Alpha("Opacidade",Float)=.5 _Bands("Faixas",Float)=3 _FxTime("Tempo",Float)=0 _Seed("Semente",Float)=0 }
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
                half4 _LightColor; float _Alpha; float _Bands; float _FxTime; float _Seed;
            CBUFFER_END
            struct A{float4 position:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
            V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target
            {
                float y=i.uv.y;
                float hw=lerp(.5,.10,pow(y,.8));                  // afunila
                float u=(i.uv.x-.5)/hw*.5+.5;
                float inside=step(0,u)*step(u,1);
                float cell=floor(u*_Bands);
                float f=frac(u*_Bands);
                float duty=.30+.35*frac(sin((cell+_Seed)*12.9898)*43758.5453);   // larguras variadas por faixa
                float band=step(.12,f)*step(f,.12+duty);
                float fade=smoothstep(0,.08,y)*(1-smoothstep(.72,1,y));
                float pulse=.88+.12*sin(_FxTime*1.2+_Seed+u*4);
                float a=band*inside*fade*pulse*saturate(_Alpha);
                return half4(saturate(_LightColor.rgb),min(a,.6));
            }
            ENDHLSL
        }
    }
}
