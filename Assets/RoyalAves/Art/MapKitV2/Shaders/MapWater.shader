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
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; half4 _Line; half4 _HazeColor; half _HazeNear; half _HazeFar; half _NightAmount; half4 _NightColor;
                float _MapTime;
                half _HasWakeSprite;
                half _Frozen;
                float4 _IceLights[12],_IceLightColors[12];float _IceLightCount,_IceNight,_IceClock;
            CBUFFER_END
            #include "MapFrozenIce.hlsl"
            struct A { float4 pos : POSITION; half4 col : COLOR; float2 uv : TEXCOORD0;float2 motion:TEXCOORD1; };
            struct V { float4 pos : SV_POSITION; half4 col : COLOR; float2 uv : TEXCOORD0; float depth : TEXCOORD1;float motion:TEXCOORD2; };
            V vert(A i)
            {
                V o;
                float3 ws = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(ws);
                o.col = i.col;
                o.motion=i.motion.x;
                o.uv = i.uv; // uv = (x lateral, metros de rota): as linhas pertencem ao mundo, nao a tela
                o.depth = -TransformWorldToView(ws).z;
                return o;
            }
            half4 frag(V i) : SV_Target
            {
                float time=_MapTime*(1-step(1.5,i.motion));
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
                float paintedRipples=(1-step(1.5,i.motion))*max(step(.5,i.motion),1-_HasWakeSprite);
                c = lerp(c, _Line.rgb, ripple * .24*paintedRipples);
                float reflection=exp(-pow((i.col.a-.34-.04*sin(i.uv.x*.7+time*.1))/.055,2));
                c=lerp(c,_Line.rgb,reflection*.13);
                c=lerp(c,_Line.rgb,(1-smoothstep(.008,.025,shore))*.14);
                if(_Frozen>.5)
                {
                    c=FrozenLake(i.uv,shore);
                    // A scalloped snow fringe stays opaque above the glassy ice.
                    float snowEdge=.035+.012*sin(i.uv.x*4.3)+.006*sin(i.uv.x*9.1);
                    float snow=1-smoothstep(snowEdge,snowEdge+.012,shore);
                    c=lerp(c,IcePaintColor(half3(.97,.99,1)),snow);
                }
                c *= lerp(half3(1,1,1), _NightColor.rgb, _NightAmount);
                c+=FrozenNightLight(i.uv)*_Frozen;
                half h = saturate((i.depth - _HazeNear) / max(0.01, _HazeFar - _HazeNear));
                c = lerp(c, _HazeColor.rgb, h * h * 0.85);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
