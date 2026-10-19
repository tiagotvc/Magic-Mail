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
        _MagicFlow("Fluxo de tinta independente",Float)=0
        _MagicClock("Tempo da tinta",Float)=0
        _ClipBelow ("Limite inferior local",Float)=-999
        _SnowDust("Neve seguindo o contorno",Float)=0
        _SnowOnly("Somente a camada de neve",Float)=0
        _SnowFloor("Altura mínima da neve",Float)=.22
        _LampLight("Vidro do poste aceso",Float)=0
        _LampGlass("Centro e raio do vidro",Vector)=(.5,.70,.29,.14)
        _LampTint("Cor da luz do poste",Color)=(1,.88,.47,1)
        _LampCold("Luz roxa",Float)=0
        _SnowRect("Sprite para neve",Vector)=(0,0,1,1)
        _IceGlint("Reflexos do gelo",Float)=0
        _IceTime("Tempo do gelo",Float)=0
        _IceRect("Recorte do gelo",Vector)=(0,0,1,1)
        _FrozenPuddle("Poça congelada",Float)=0
        _WindowLight("Janelas acesas",Float)=0
        _WindowCount("Quantidade de janelas",Float)=0
        _WindowWarm("Vidro quente do farol",Float)=0
        _WindowPaintedGlass("Preservar pintura do vidro",Float)=0
        _UseEffectClip("Recorte de peça independente",Float)=0
        _HazeColor ("Bruma", Color) = (0.85,0.92,1,1)
        _HazeNear ("Bruma inicio", Float) = 16
        _HazeFar ("Bruma fim", Float) = 52
        _NightAmount ("Noite", Range(0,1)) = 0
        _IceNight("Luar no gelo",Float)=0
        _IceClock("Animação do reflexo",Float)=0
        _IceLightCount("Luzes refletidas",Float)=0
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
                float _MagicFlow,_MagicClock;
                float _ClipBelow;
                half _SnowDust,_SnowOnly,_SnowFloor,_LampLight,_LampCold;half4 _LampTint;float4 _SnowRect,_IceWorld,_LampRect,_LampGlass;
                half _IceGlint;float _IceTime;float4 _IceRect;
                half _FrozenPuddle;
                half _WindowLight,_WindowCount,_WindowWarm,_WindowPaintedGlass;float4 _WindowSpriteRect,_WindowRect0,_WindowRect1,_WindowRect2,_WindowRect3;
                half _UseEffectClip;float4 _EffectClip;
                float4 _IceLights[12],_IceLightColors[12];float _IceLightCount,_IceNight,_IceClock;
            CBUFFER_END
            #include "MapFrozenIce.hlsl"

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
                if(_UseEffectClip>.5){clip(i.uv-_EffectClip.xy);clip(_EffectClip.xy+_EffectClip.zw-i.uv);}
                half4 t = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half a = saturate((t.a - _Cutoff) / max(fwidth(t.a), 0.0001) + 0.5);
                a *= _Tint.a;
                half low=min(t.r,min(t.g,t.b));half high=max(t.r,max(t.g,t.b));
                half paleBorder=step(.55,low)*(1-step(.32,high-low));
                a*=1-_GroundedFoot*paleBorder*(1-smoothstep(.025,.06,i.foot));
                clip(a - 0.01);
                half3 c = t.rgb * _Tint.rgb;
                if(_MagicFlow>.5)
                {
                    // Only the separately authored liquid layer receives this material block.
                    // Its alpha silhouette is fixed; the ink highlights move inside it.
                    float liquid=smoothstep(.04,.20,t.b-t.g);
                    float flow=pow(saturate(sin(i.uv.y*31+_MagicClock*5.8+sin(i.uv.x*28)*.8)),9);
                    float radius=length((i.uv-float2(.5,.48))*float2(1,1.9));
                    float ripple=pow(saturate(sin(radius*36-_MagicClock*3.3)),12);
                    float gleam=lerp(flow,ripple,step(1.5,_MagicFlow));
                    c=lerp(c,half3(1,.85,1),gleam*liquid*.44);
                }
                if(_FrozenPuddle>.5)
                {
                    float2 uv=(i.uv-_IceRect.xy)/_IceRect.zw;
                    // Freeze only the saturated blue water, preserving the painted snowy/cream rim.
                    float water=smoothstep(.06,.22,t.b-t.r)*smoothstep(.02,.13,t.g-t.r);
                    c=lerp(c,FrozenLake(uv*3.5,saturate(t.a)*.3),water);
                }
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
                    float mask=smoothstep(.30,1.25,edge)*smoothstep(_SnowFloor,_SnowFloor+.08,i.foot)*_SnowDust;
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
                float windowMask=0;
                if(_WindowLight>.001&&_WindowCount>0)
                {
                    float2 uv=(i.uv-_WindowSpriteRect.xy)/_WindowSpriteRect.zw;
                    float4 regions[4]={_WindowRect0,_WindowRect1,_WindowRect2,_WindowRect3};
                    [unroll]for(int k=0;k<4;k++)
                    {
                        float4 r=regions[k];float2 inside=smoothstep(r.xy,r.xy+.006,uv)*(1-smoothstep(r.xy+r.zw-.006,r.xy+r.zw,uv));
                        windowMask=max(windowMask,inside.x*inside.y*step(k+.5,_WindowCount));
                    }
                    float blueGlass=smoothstep(.025,.13,t.b-t.r)*smoothstep(.02,.12,t.g-t.r);
                    float darkGlass=(1-smoothstep(.26,.50,low))*smoothstep(.03,.12,high);
                    float warmGlass=smoothstep(.14,.35,t.r-t.b)*smoothstep(.35,.65,t.g)*_WindowWarm;
                    windowMask*=max(max(blueGlass,darkGlass*(1-_WindowPaintedGlass)),warmGlass);
                }
                half luma = dot(c, half3(0.3, 0.59, 0.11));
                c = lerp(c, luma.xxx, _Desat);
                // so tons quentes (amarelo/laranja: janelas, lampadas, luzes) acendem a noite; folhas verdes nao
                half warm=step(.8,t.r/max(t.g,.01))*step(.55,t.r)*step(t.b,.55);
                half3 emis=c*_Emission*smoothstep(.60,.92,high)*smoothstep(.15,.5,high-low)*lerp(1,warm,_NightAmount);
                c+=emis;
                // noite: escurece a pintura, mas pontos claros e saturados (janelas, luzes de Natal, lampadas) ficam acesos
                half lit=smoothstep(.62,.9,high)*smoothstep(.12,.45,high-low)*warm;
                c=lerp(c*lerp(half3(1,1,1),_NightColor.rgb,_NightAmount), c*1.15, _NightAmount*lit*step(.12,_Emission));
                c+=emis*_NightAmount*1.4;
                half glassShading=lerp(1,.45+.7*max(t.g,t.b),_WindowPaintedGlass);
                c=lerp(c,IcePaintColor(half3(1,.72,.23))*(1+_WindowLight*.65)*glassShading,windowMask*_WindowLight);
                if(_LampLight>.001)
                {
                    float2 uv=(i.uv-_LampRect.xy)/_LampRect.zw;
                    float glass=1-smoothstep(.85,1.0,length((uv-_LampGlass.xy)/max(_LampGlass.zw,float2(.001,.001))));
                    float warmGlass=step(.34,t.r)*step(.24,t.g)*(1-step(t.r+.15,t.b));
                    float coldGlass=smoothstep(.16,.52,max(t.r,t.b));
                    glass*=lerp(warmGlass,coldGlass,_LampCold);
                    c=lerp(c,lerp(c,_LampTint.rgb*(1+_NightAmount*.65),.48+_NightAmount*.30),glass*_LampLight);
                }
                if(_FrozenPuddle>.5)
                {
                    float2 uv=(i.uv-_IceRect.xy)/_IceRect.zw;
                    float water=smoothstep(.06,.22,t.b-t.r)*smoothstep(.02,.13,t.g-t.r);
                    c+=FrozenNightLight(_IceWorld.xy+(uv-.5)*_IceWorld.zw)*water;
                }
                half h = saturate((i.depth - _HazeNear) / max(0.01, _HazeFar - _HazeNear));
                c = lerp(c, _HazeColor.rgb, h * h * 0.8);
                return half4(c, a);
            }
            ENDHLSL
        }
    }
}
