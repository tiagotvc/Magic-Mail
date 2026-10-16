Shader "RoyalAves/WorldMap/Leaf"
{
    Properties
    {
        _MainTex("Folha",2D)="white"{}
        _Tint("Cor e opacidade",Color)=(1,1,1,1)
        _HazeColor("Bruma",Color)=(.85,.92,1,1)
        _HazeNear("Inicio da bruma",Float)=16
        _HazeFar("Fim da bruma",Float)=56
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint,_HazeColor;float _HazeNear,_HazeFar;
            CBUFFER_END
            struct A{float4 pos:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float depth:TEXCOORD1;};
            V vert(A i){V o;float3 ws=TransformObjectToWorld(i.pos.xyz);o.pos=TransformWorldToHClip(ws);o.uv=i.uv;o.depth=-TransformWorldToView(ws).z;return o;}
            half4 frag(V i):SV_Target
            {
                half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*_Tint;
                clip(c.a-.005);
                float haze=saturate((i.depth-_HazeNear)/max(.01,_HazeFar-_HazeNear));
                c.rgb=lerp(c.rgb,_HazeColor.rgb,haze*haze*.8);
                return c;
            }
            ENDHLSL
        }
    }
}
