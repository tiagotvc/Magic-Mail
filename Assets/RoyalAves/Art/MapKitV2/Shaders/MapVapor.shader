Shader "RoyalAves/WorldMap/Vapor"
{
    Properties { _MainTex("Sprite",2D)="white"{} _Tint("Tint",Color)=(1,1,1,1) _ReflectionInk("Reflexo creme",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _Tint;
            half _ReflectionInk;
            CBUFFER_END
            struct A { float4 position:POSITION;float2 uv:TEXCOORD0; };
            struct V { float4 position:SV_POSITION;float2 uv:TEXCOORD0; };
            V vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;return o;}
            half4 frag(V i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);c.rgb=lerp(c.rgb,half3(.99,.98,.90),_ReflectionInk);return c*_Tint;}
            ENDHLSL
        }
    }
}
