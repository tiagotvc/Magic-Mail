Shader "RoyalAves/WorldMap/Card"
{
    // Cartao ilustrado em pe no chao: corpo solido com alpha clip, escreve profundidade (oclusao correta).
    Properties
    {
        _MainTex ("Textura", 2D) = "white" {}
        _Tint ("Tint", Color) = (1,1,1,1)
        _Cutoff ("Corte alpha", Range(0,1)) = 0.5
        _Desat ("Desaturar", Range(0,1)) = 0
        _GroundedFoot ("Apoio no terreno", Float) = 0
        _Emission ("Luz pintada",Float)=0
        _ClipBelow ("Limite inferior local",Float)=-999
        _SnowDust("Neve seguindo o contorno",Float)=0
        _SnowOnly("Somente a camada de neve",Float)=0
        _SnowRect("Sprite para neve",Vector)=(0,0,1,1)
        _IceGlint("Reflexos do gelo",Float)=0
        _IceTime("Tempo do gelo",Float)=0
        _IceRect("Recorte do gelo",Vector)=(0,0,1,1)
        _HazeColor ("Bruma", Color) = (0.85,0.92,1,1)
        _HazeNear ("Bruma inicio", Float) = 16
        _HazeFar ("Bruma fim", Float) = 52
        _NightAmount ("Noite", Range(0,1)) = 0
        _NightColor ("Cor da noite", Color) = (0.34,0.42,0.78,1)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Off
            AlphaToMask On
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; half4 _Tint; half _Cutoff; half _Desat; half4 _HazeColor; half _HazeNear; half _HazeFar; half _NightAmount; half4 _NightColor;
                half _GroundedFoot;
                half _Emission;
                float _ClipBelow;
                half _SnowDust,_SnowOnly;float4 _SnowRect;
                half _IceGlint;float _IceTime;float4 _IceRect;
            CBUFFER_END

            struct A { float4 pos : POSITION; float2 uv : TEXCOORD0; float2 foot : TEXCOORD1; };
            struct V { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float depth : TEXCOORD1; float foot : TEXCOORD2; float localY:TEXCOORD3; };

            V vert(A i)
            {
                V o;
                float3 ws = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(ws);
                o.uv = TRANSFORM_TEX(i.uv, _MainTex);
                o.foot=i.foot.y;
                o.localY=i.pos.y;
                o.depth = -TransformWorldToView(ws).z;
                return o;
            }

            half4 frag(V i) : SV_Target
            {
                clip(i.localY-_ClipBelow);
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = saturate((t.a - _Cutoff) / max(fwidth(t.a), 0.0001) + 0.5);
                half low=min(t.r,min(t.g,t.b));half high=max(t.r,max(t.g,t.b));
                half paleBorder=step(.55,low)*(1-step(.32,high-low));
                a*=1-_GroundedFoot*paleBorder*(1-smoothstep(.025,.06,i.foot));
                clip(a - 0.01);
                half3 c = t.rgb * _Tint.rgb;
                if(_SnowDust>.001)
                {
                    float2 localUV=(i.uv-_SnowRect.xy)/_SnowRect.zw;
                    float thickness=.035+.010*sin(localUV.x*19+.5);
                    float edge=0;
                    [unroll]for(int k=1;k<=6;k++)
                    {
                        float2 uv=i.uv+float2(0,_SnowRect.w*thickness*(k/6.0));
                        float inside=step(uv.y,_SnowRect.y+_SnowRect.w);
                        float alpha=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,clamp(uv,_SnowRect.xy,_SnowRect.xy+_SnowRect.zw)).a*inside;
                        edge+=1-smoothstep(.35,.65,alpha);
                    }
                    float mask=smoothstep(.30,1.25,edge)*smoothstep(.22,.30,i.foot)*_SnowDust;
                    half3 snow=lerp(half3(.52,.68,.89),half3(1,.985,.94),smoothstep(0,5.5,edge));
                    #if !defined(UNITY_COLORSPACE_GAMMA)
                    snow=SRGBToLinear(snow);
                    #endif
                    c=lerp(c,snow,mask);
                    if(_SnowOnly>.5){c=snow*_Tint.rgb;a*=mask;clip(a-.01);}
                }
                if(_IceGlint>.001)
                {
                    float2 uv=(i.uv-_IceRect.xy)/_IceRect.zw;
                    float sweep=abs(uv.x*.85+uv.y*.42-(frac(_IceTime*.22)*1.8-.30));
                    float streak=1-smoothstep(.005,.014+fwidth(sweep),sweep);
                    float2 sparkleDelta=uv-float2(.46,.53);
                    float star=saturate(1-sqrt(abs(sparkleDelta.x)/.035)-sqrt(abs(sparkleDelta.y)/.065));
                    float flash=pow(saturate(sin(_IceTime*1.7+1.1)),6);
                    c=lerp(c,half3(1,1,1),saturate(streak*.80+star*flash)*_IceGlint);
                }
                half luma = dot(c, half3(0.3, 0.59, 0.11));
                c = lerp(c, luma.xxx, _Desat);
                // so tons quentes (amarelo/laranja: janelas, lampadas, luzes) acendem a noite; folhas verdes nao
                half warm=step(.8,t.r/max(t.g,.01))*step(.55,t.r)*step(t.b,.55);
                half3 emis=c*_Emission*smoothstep(.60,.92,high)*smoothstep(.15,.5,high-low)*lerp(1,warm,_NightAmount);
                c+=emis;
                // noite: escurece a pintura, mas pontos claros e saturados (janelas, luzes de Natal, lampadas) ficam acesos
                half lit=smoothstep(.62,.9,high)*smoothstep(.12,.45,high-low)*warm;
                c=lerp(c*lerp(half3(1,1,1),_NightColor.rgb,_NightAmount), c*1.15, _NightAmount*lit*step(.01,_Emission+.06));
                c+=emis*_NightAmount*1.4;
                half h = saturate((i.depth - _HazeNear) / max(0.01, _HazeFar - _HazeNear));
                c = lerp(c, _HazeColor.rgb, h * h * 0.8);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
