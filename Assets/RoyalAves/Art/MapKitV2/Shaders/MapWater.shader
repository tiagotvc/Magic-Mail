Shader "RoyalAves/WorldMap/Water"
{
    // Agua simples: cor por vertice e linhas claras que correm devagar. Sem reflexo em tempo real.
    Properties
    {
        _Tint ("Tint", Color) = (1,1,1,1)
        _Line ("Cor das linhas", Color) = (1,1,1,0.5)
        _HazeColor ("Bruma", Color) = (0.85,0.92,1,1)
        _HazeNear ("Bruma inicio", Float) = 16
        _HazeFar ("Bruma fim", Float) = 52
        _NightAmount ("Noite", Range(0,1)) = 0
        _IceNight("Luar no gelo",Float)=0
        _IceClock("Animação do reflexo",Float)=0
        _IceLightCount("Luzes refletidas",Float)=0
        _NightColor ("Cor da noite", Color) = (0.34,0.42,0.78,1)
        _MapTime("Tempo do mapa",Float)=0
        _HasWakeSprite("Reflexos por Sprite",Float)=0
        _Frozen("Passagem congelada",Float)=0
        _LavaTex("Painted lava surface",2D)="white"{}
        _LavaRect("Lava sprite rect",Vector)=(0,0,1,1)
        _HasLavaArt("Use painted lava",Float)=0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_LavaTex);SAMPLER(sampler_LavaTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; half4 _Line; half4 _HazeColor; half _HazeNear; half _HazeFar; half _NightAmount; half4 _NightColor;
                float _MapTime;
                half _HasWakeSprite;
                half _Frozen;
                half _HasLavaArt;float4 _LavaRect;
                float4 _IceLights[12],_IceLightColors[12];float _IceLightCount,_IceNight,_IceClock;
            CBUFFER_END
            #include "MapFrozenIce.hlsl"
            struct A { float4 pos : POSITION; half4 col : COLOR; float2 uv : TEXCOORD0;float2 motion:TEXCOORD1; };
            struct V { float4 pos : SV_POSITION; half4 col : COLOR; float2 uv : TEXCOORD0; float depth : TEXCOORD1;float2 motion:TEXCOORD2; };
            V vert(A i)
            {
                V o;
                float3 ws = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(ws);
                o.col = i.col;
                o.motion=i.motion;
                o.uv = i.uv; // uv = (x lateral, metros de rota): as linhas pertencem ao mundo, nao a tela
                o.depth = -TransformWorldToView(ws).z;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float time=_MapTime*(1-step(1.5,i.motion.x));
                float liquid=floor(i.motion.y+.001);
                float gloss=frac(i.motion.y)*10;
                float winterRule=frac(i.motion.x);
                float frozen=winterRule>.15?1:winterRule>.05?0:_Frozen;
                float w = sin(i.uv.x * .8 + i.uv.y * 1.6 + time * .12);
                half3 c = i.col.rgb * _Tint.rgb;
                float shore=min(i.col.a,1-i.col.a);
                c*=lerp(.74,1.12,smoothstep(0,.40,shore))*(.98+.018*w);
                float diffuse=exp(-pow((i.col.a-.47)/.30,2)-pow(sin(i.uv.x*.19)*.4,2));
                c=lerp(c,_Line.rgb,diffuse*.08);
                // Sparse short painted ripples, never a full-width stripe across the channel.
                float2 cell=frac(i.uv*float2(.32,.8)+float2(time*.006,0));
                float ripple=(1-smoothstep(.012,.028,abs(cell.y-.42-(cell.x-.5)*(cell.x-.5)*.22)))
                    *smoothstep(.15,.25,cell.x)*(1-smoothstep(.55,.72,cell.x));
                float paintedRipples=(1-step(1.5,i.motion.x))*max(step(.5,i.motion.x),1-_HasWakeSprite);
                c = lerp(c, _Line.rgb, ripple * .24*paintedRipples);
                float reflection=exp(-pow((i.col.a-.34-.04*sin(i.uv.x*.7+time*.1))/.055,2));
                c=lerp(c,_Line.rgb,reflection*.13);
                c=lerp(c,_Line.rgb,(1-smoothstep(.008,.025,shore))*.14);
                // Broad colored ribbons, cream specular slivers and a dark glossy shoreline.
                float progress=i.col.a;
                float flow=sin(i.uv.x*.72+progress*8+time*.10);
                float ribbon=exp(-pow((progress-.52-.12*sin(i.uv.x*.43+time*.04))/.23,2));
                half3 bright=saturate(i.col.rgb*1.22+half3(.12,.10,.14));
                c=lerp(c,bright,ribbon*.45);
                // A second hue gives every liquid depth instead of a single cyan or purple fill.
                half3 deep=c*half3(.42,.66,.82);
                half3 light=c*half3(.80,1.16,1.13);
                if(liquid>.5&&liquid<2.5){deep=c*half3(.40,.44,.92);light=c*half3(1.14,.75,1.08);}
                c=lerp(deep,light,smoothstep(.02,.42,shore));
                float paintedBand=smoothstep(.58,.69,progress+.04*sin(i.uv.x*.55));
                c=lerp(c,c*half3(.67,.91,1.23),paintedBand*.50);
                float streak=exp(-pow((progress-.29-.045*sin(i.uv.x*.65+time*.05))/.022,2));
                float glint=exp(-pow((progress-.64-.035*sin(i.uv.x*.9))/.012,2))*pow(saturate(sin(i.uv.x*1.1+.8)),8);
                c=lerp(c,half3(1,.94,.86),streak*.60*gloss*pow(saturate(sin(i.uv.x*.62+.4)),5));
                c=lerp(c,half3(1,1,.96),glint*.88*gloss);
                if(liquid>2.5&&liquid<3.5)
                {
                    c=lerp(half3(.58,.06,.12),half3(1,.68,.08),saturate(ribbon*.85+flow*.12));
                    c=lerp(c,half3(1,.94,.45),glint*.85+streak*.3);
                    if(_HasLavaArt>.5)
                    {
                        // World-anchored painted currents: constant scale on narrow rivers and lakes.
                        float2 lavaUV=frac(i.uv*.145+float2(time*.002,0));
                        half3 painted=SAMPLE_TEXTURE2D(_LavaTex,sampler_LavaTex,_LavaRect.xy+lavaUV*_LavaRect.zw).rgb;
                        float edge=smoothstep(0,.10,shore);
                        c=painted*lerp(half3(.62,.43,.42),half3(1,1,1),edge);
                        c=lerp(c,half3(1,.88,.36),(1-smoothstep(.003,.012,shore))*.38*gloss);
                    }
                }
                if(frozen>.5&&liquid<.5)
                {
                    c=FrozenLake(i.uv,shore);
                    // A scalloped snow fringe stays opaque above the glassy ice.
                    float snowEdge=.035+.012*sin(i.uv.x*4.3)+.006*sin(i.uv.x*9.1);
                    float snow=1-smoothstep(snowEdge,snowEdge+.012,shore);
                    c=lerp(c,IcePaintColor(half3(.97,.99,1)),snow);
                }
                c *= lerp(half3(1,1,1), _NightColor.rgb, _NightAmount);
                c+=FrozenNightLight(i.uv)*frozen;
                half h = saturate((i.depth - _HazeNear) / max(0.01, _HazeFar - _HazeNear));
                c = lerp(c, _HazeColor.rgb, h * h * 0.85);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
