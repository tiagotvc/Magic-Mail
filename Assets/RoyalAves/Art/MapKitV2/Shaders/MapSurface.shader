Shader "RoyalAves/WorldMap/Surface"
{
    // Terreno, medalhoes e ponte: cor por vertice, luz suave tipo cartoon, bruma de distancia.
    Properties
    {
        _PaintTex ("Chao pintado", 2D) = "white" {}
        _PaintAmount ("Pintura", Range(0,1)) = 0
        _TokenMode ("Botao de fase", Float) = 0
        _GroundMode ("Terreno ilustrado", Float) = 0
        _PaintScroll ("Avanco da pintura", Float) = 0
        _TokenFaceTex ("Arte do botao", 2D) = "white" {}
        _TokenFaceRect ("Recorte do botao", Vector) = (0,0,1,1)
        _TokenFaceAmount ("Usar arte do botao", Float) = 0
        _BridgeTex ("Deck da ponte", 2D) = "white" {}
        _BridgeWoodTex ("Ponte de tábuas",2D)="white"{}
        _BridgeRect ("Recorte do deck", Vector) = (0,0,1,1)
        _BridgeWoodRect ("Deck de madeira", Vector) = (0,0,1,1)
        _BridgeLogsRect ("Deck de troncos", Vector) = (0,0,1,1)
        _TerrainTiles ("Materiais pintados", 2D) = "white" {}
        _UseTerrainTiles ("Materiais separados", Float) = 0
        _LavaTerrain ("Paleta vulcanica", Float) = 0
        _SnowCoverage ("Neve da season no chão", Range(0,1)) = 0
        _SnowTex("Chão nevado",2D)="white"{}
        _SnowRect("Atlas de neve",Vector)=(0,0,1,1)
        _HasSnowTex("Usar chão nevado",Float)=0
        _SnowRowFlip("Ordem do atlas nevado",Float)=0
        _SimpleBase("Mundo verde ilustrado",Float)=0
        _SimpleGrade("Degradês da base",Float)=0
        _GroundBrushLawn("Pintura das laterais",2D)="white"{}
        _GroundBrushPath("Pintura da trilha",2D)="white"{}
        _GroundBrushAmount("Pintura artística do terreno",Range(0,1))=0
        _PathMint("Trilha menta",Float)=0
        _SimpleGroundVariant("Paleta ilustrada da ilha",Float)=0
        _SimpleLawnColor("Cor lateral da ilha",Color)=(.4,.7,.5,1)
        _SimplePathColor("Cor da trilha da ilha",Color)=(1,.8,.6,1)
        _LaneShape ("Largura, bisel, luz, altura", Vector) = (1.18,.075,.025,.039)
        _TerrainRect ("Atlas de materiais", Vector) = (0,0,1,1)
        _GroundRimLight ("Luz da margem", Color) = (.36,.5,.28,1)
        _GrassGrade ("Suavidade do gramado", Vector) = (0,0,0,0)
        _FaceColor ("Cor do botao", Color) = (1,0.2,0.6,1)
        _Tint ("Tint", Color) = (1,1,1,1)
        _Desat ("Desaturar", Range(0,1)) = 0
        _Glow ("Brilho", Range(0,1)) = 0
        _HazeColor ("Bruma", Color) = (0.85,0.92,1,1)
        _HazeNear ("Bruma inicio", Float) = 16
        _HazeFar ("Bruma fim", Float) = 52
        _NightAmount ("Noite", Range(0,1)) = 0
        _NightColor ("Cor da noite", Color) = (0.34,0.42,0.78,1)
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            TEXTURE2D(_PaintTex); SAMPLER(sampler_PaintTex);
            TEXTURE2D(_TokenFaceTex); SAMPLER(sampler_TokenFaceTex);
            TEXTURE2D(_BridgeTex); SAMPLER(sampler_BridgeTex);
            TEXTURE2D(_BridgeWoodTex);SAMPLER(sampler_BridgeWoodTex);
            TEXTURE2D(_TerrainTiles); SAMPLER(sampler_TerrainTiles);
            TEXTURE2D(_SnowTex);SAMPLER(sampler_SnowTex);
            TEXTURE2D(_GroundBrushLawn);SAMPLER(sampler_GroundBrushLawn);
            TEXTURE2D(_GroundBrushPath);SAMPLER(sampler_GroundBrushPath);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; half _Desat; half _Glow; half4 _HazeColor; half _HazeNear; half _HazeFar; half _NightAmount; half4 _NightColor;
                half _PaintAmount; half _TokenMode; half _GroundMode; float _PaintScroll; half4 _FaceColor;
                float4 _GroundBounds[64];
                half _TokenFaceAmount;float4 _TokenFaceRect;
                float4 _BridgeRect;half4 _GroundRimLight;half4 _GrassGrade;
                float4 _BridgeWoodRect,_BridgeLogsRect;half _UseTerrainTiles,_LavaTerrain;
                float4 _LaneShape;
                float4 _TerrainRect;
                half _SnowCoverage;
                half _HasSnowTex,_SnowRowFlip;float4 _SnowRect;
                half _SimpleBase,_SimpleGrade,_PathMint;
                half _GroundBrushAmount;
                half _SimpleGroundVariant;half4 _SimpleLawnColor,_SimplePathColor;
            CBUFFER_END

            struct A { float4 pos : POSITION; float3 nrm : NORMAL; half4 col : COLOR; float2 uv : TEXCOORD0; float2 lane : TEXCOORD1; float3 lawn : TEXCOORD2; float3 path : TEXCOORD3; };
            struct V { float4 pos : SV_POSITION; float3 nrmWS : TEXCOORD0; half4 col : COLOR; float depth : TEXCOORD1; float2 uv : TEXCOORD2; float2 lane : TEXCOORD3; half3 lawn : TEXCOORD4; half3 path : TEXCOORD5; };

            V vert(A i)
            {
                V o;
                float3 ws = TransformObjectToWorld(i.pos.xyz);
                o.pos = TransformWorldToHClip(ws);
                o.nrmWS = TransformObjectToWorldNormal(i.nrm);
                o.col = i.col;
                o.uv = i.uv;
                o.lane = i.lane;
                o.lawn=i.lawn;o.path=i.path;
                o.depth = -TransformWorldToView(ws).z;
                return o;
            }

            half3 PaintedGround(float u, float v)
            {
                float row = clamp(v / .72 * 63, 0, 63);
                int index = min((int)floor(row), 62);
                float2 bounds = lerp(_GroundBounds[index].xy, _GroundBounds[index+1].xy, row - index);
                // Retarget the clean lane and grass independently. Sampling the source bevel itself
                // amplified small brush irregularities into dents along the moving mesh boundary.
                float laneU=lerp(bounds.x+.018,bounds.y-.018,saturate((u-.32)/.36));
                float outsideU=u<.5 ? saturate(u/.32)*max(.002,bounds.x-.025)
                    : min(.998,bounds.y+.025)+saturate((u-.68)/.32)*(1-min(.998,bounds.y+.025));
                half3 lane=SAMPLE_TEXTURE2D(_PaintTex,sampler_PaintTex,float2(laneU,v)).rgb;
                half3 grass=SAMPLE_TEXTURE2D(_PaintTex,sampler_PaintTex,float2(outsideU,v)).rgb;
                grass=lerp(grass,_GrassGrade.rgb,_GrassGrade.a);
                float edge=smoothstep(.316,.322,u)*(1-smoothstep(.678,.684,u));
                return lerp(grass,lane,edge);
            }

            half3 MapColor(half3 c)
            {
                #if defined(UNITY_COLORSPACE_GAMMA)
                return c;
                #else
                return SRGBToLinear(c);
                #endif
            }
            float GroundPatchHash(float row, float seed)
            {
                float h=frac((row+seed)*.1031);
                h*=h+33.33;
                return frac(h*(h+19.19));
            }
            float GroundPatchEdge(float row, float across, float spacing, float seed)
            {
                // User-drawn reference: straight diagonal boundaries, alternating / and \.
                // Each neighboring band shares the same boundary; no center fold or wavy edge.
                float offset=(GroundPatchHash(row,seed)-.5)*spacing*.05;
                float tilt=lerp(-.32,.32,frac(row*.5)*2);
                return row*spacing+offset+across*tilt;
            }
            float GroundPatches(float along, float across, float spacing, float seed)
            {
                float row=floor(along/spacing);
                float softness=max(.035,fwidth(along)*1.15);
                float tone=0;
                [unroll] for(int offset=-1;offset<=1;offset++)
                {
                    float n=row+offset;
                    float start=GroundPatchEdge(n,across,spacing,seed);
                    float end=GroundPatchEdge(n+1,across,spacing,seed);
                    float inside=smoothstep(start-softness,start+softness,along)
                        *(1-smoothstep(end-softness,end+softness,along));
                    tone+=inside*frac(n*.5)*2;
                }
                return saturate(tone);
            }
            float GroundEllipse(float2 p,float2 radius)
            {
                float d=length(p/radius);
                return 1-smoothstep(.72,1.0,d);
            }
            float2 GroundDapples(float2 world)
            {
                // Sparse painted stains and three-dot clusters, fixed to the world rather than the screen.
                float2 size=float2(1.50,1.80),cell=floor(world/size),local=frac(world/size)*size;
                float key=cell.x*17.31+cell.y*41.73;
                float h=GroundPatchHash(key,8.23),j=GroundPatchHash(key,24.17);
                float2 center=size*(float2(.30,.32)+float2(h,j)*.38);
                float2 p=local-center;
                float stain=GroundEllipse(p,float2(.30+h*.15,.18+j*.10));
                stain+=GroundEllipse(p-float2(.34,.19),float2(.16,.10))*.78;
                stain*=step(.32,h);
                float dots=GroundEllipse(p-float2(-.26,-.31),float2(.038,.055));
                dots+=GroundEllipse(p-float2(-.13,-.18),float2(.030,.042));
                dots+=GroundEllipse(p-float2(-.31,-.11),float2(.027,.038));
                dots+=GroundEllipse(p-float2(-.08,-.35),float2(.021,.032));
                dots*=step(.20,j);
                return saturate(float2(stain,dots));
            }
            half4 frag(V i) : SV_Target
            {
                half3 n = normalize(i.nrmWS);
                half3 L = normalize(half3(-0.35, 0.80, -0.48));
                half ndl = dot(n, L) * 0.5 + 0.5;
                half lit = lerp(.94, 1.03, smoothstep(.28, .82, ndl));
                half3 c = i.col.rgb * _Tint.rgb * lit;
                half paint = SAMPLE_TEXTURE2D(_PaintTex, sampler_PaintTex, frac(i.uv)).r;
                c *= lerp(1, .48 + paint * .52, _PaintAmount);
                if (_GroundMode > .5 && i.col.a <= .01)
                {
                    // The artist's frontal plate is sampled in screen proportions. Row bounds retarget
                    // its already-painted lane and shoulders to the live world without stretching UVs.
                    float2 screenUV = GetNormalizedScreenSpaceUV(i.pos);
                    float u = clamp(i.uv.x,.002,.998);
                    float v = frac(_PaintScroll + screenUV.y / .74) * .72;
                    half3 art = PaintedGround(u,v);
                    half3 seam = (PaintedGround(u,.001) + PaintedGround(u,.719)) * .5;
                    art = lerp(seam, art, smoothstep(0,.035,min(v,.72-v)));
                    half3 lawn = PaintedGround(u < .5 ? .12 : .88,v);
                    art = lerp(lawn,art,smoothstep(.001,.025,-i.col.a));
                    // Signed distance to the lane includes its elliptical end, so the inset has
                    // an uninterrupted bright outer lip and a shaded inner wall in every theme.
                    float distance=abs(i.lane.x)-_LaneShape.x;
                    if(i.lane.y<.6)
                    {
                        float2 p=float2(abs(i.lane.x),i.lane.y-.6);
                        float2 axes=float2(_LaneShape.x,.6);
                        float k0=length(p/axes),k1=length(p/(axes*axes));
                        distance=k0<.0001?-.6:k0*(k0-1)/max(k1,.0001);
                    }
                    float cap=1-smoothstep(.15,.8,i.lane.y);
                    half3 bankSurface=lawn;
                    if(_UseTerrainTiles>.5)
                    {
                        float s=i.uv.y*14;
                        float2 tile=abs(frac(float2(i.lane.x,s)*.125)*2-1);
                        tile=.025+tile*.45;
                        float row=_LavaTerrain>.5?0:.5;
                        half3 lawnTile=SAMPLE_TEXTURE2D(_TerrainTiles,sampler_TerrainTiles,_TerrainRect.xy+(tile+float2(0,row))*_TerrainRect.zw).rgb;
                        half3 laneTile=SAMPLE_TEXTURE2D(_TerrainTiles,sampler_TerrainTiles,_TerrainRect.xy+(tile+float2(.5,row))*_TerrainRect.zw).rgb;
                        #if defined(UNITY_COLORSPACE_GAMMA)
                        half3 lawnBase=_LavaTerrain>.5?half3(.38,.14,.26):half3(.18,.59,.30);
                        half3 laneBase=_LavaTerrain>.5?half3(.99,.74,.51):half3(.74,.88,.64);
                        #else
                            half3 lawnBase=_LavaTerrain>.5?half3(.119,.017,.055):half3(.027,.307,.073);
                            half3 laneBase=_LavaTerrain>.5?half3(.977,.508,.224):half3(.508,.749,.367);
                        #endif
                        lawnTile=lerp(lawnBase,lawnTile,.48);
                        laneTile=lerp(laneBase,laneTile,.42);
                        float phase=frac((s+i.lane.x*.08)/4.6);
                        float pathBand=smoothstep(.02,.055,phase)-smoothstep(.50,.535,phase);
                        float lawnBand=.5+.5*sin((s-i.lane.x*.13)*.77);
                        float broadLight=exp(-pow(i.lane.x/(_LaneShape.x*.85),2));
                        laneTile*=lerp(.965,1.035,pathBand)*lerp(.985,1.025,broadLight);
                        lawnTile*=lerp(.985,1.015,lawnBand);
                        lawnTile*=i.lawn;laneTile*=i.path;
                        bankSurface=lawnTile;
                        art=lerp(lawnTile,laneTile,1-smoothstep(-.018,.022,distance));
                    }
                    // A colored vertical face and a fine bright top, without the old black blur.
                    float wall=smoothstep(-.004,.006,distance)*(1-smoothstep(_LaneShape.y*.8,_LaneShape.y*1.05,distance));
                    float lip=exp(-pow((distance-_LaneShape.y)/(_LaneShape.z*.45),2));
                    half facing=lerp(.92,.88,smoothstep(-.5,.5,i.lane.x));
                    art=lerp(art,bankSurface*facing,wall*.70);
                    art=lerp(art,bankSurface*1.085+_GroundRimLight.rgb*.01,lip*.40);
                    if(_SimpleGrade>.5)
                    {
                        float s=i.uv.y*14;
                        float pathBand=GroundPatches(s,clamp(i.lane.x,-3.2,3.2),2.7,.31);
                        // Grass is its own painted surface: wider bands and separate phases on
                        // either bank, so trail diagonals stop at the rim instead of continuing.
                        float lawnSeed=i.lane.x<0?2.37:5.81;
                        float lawnPhase=i.lane.x<0?1.05:2.20;
                        float lawnAcross=clamp((abs(i.lane.x)-1.18)*.45,0,1.8);
                        float lawnBand=GroundPatches(s+lawnPhase,lawnAcross,3.45,lawnSeed);
                        float shoulder=exp(-pow((abs(i.lane.x)-1.2)/2.4,2));
                        // Broad warm landing light, independent of the painted color patches.
                        float broad=.12+shoulder*.70+sin(s*.19+i.lane.x*.12)*.045;
                        half3 grassLight=MapColor(half3(.83,.91,.47));
                        half3 grass=lerp(MapColor(half3(.21,.60,.49)),MapColor(half3(.52,.77,.42)),smoothstep(0,.95,broad));
                        grass=lerp(grass,MapColor(half3(.42,.77,.58)),(1-lawnBand)*.065);
                        float entrance=exp(-pow((i.lane.y+.20)/2.65,2)-pow(i.lane.x/3.0,2));
                        grass=lerp(grass,grassLight,entrance*.86);
                        float center=exp(-pow((i.lane.x+.22)/1.1,2));
                        half3 sand=lerp(MapColor(half3(.97,.65,.32)),MapColor(half3(1,.89,.61)),center*.78+.12);
                        half3 mint=lerp(MapColor(half3(.40,.76,.44)),MapColor(half3(.76,.94,.56)),center*.78+.12);
                        half3 path=lerp(sand,mint,_PathMint);
                        if(_SimpleGroundVariant>.5)
                        {
                            half3 lawn=MapColor(_SimpleLawnColor.rgb),lane=MapColor(_SimplePathColor.rgb);
                            half3 softLight=MapColor(half3(1,.96,.80));
                            half3 lawnLight=MapColor(saturate(_SimpleLawnColor.rgb*1.25+half3(.025,.035,.035)));
                            grassLight=MapColor(saturate(_SimpleLawnColor.rgb*1.30+.07));
                            grass=lerp(lawn*.77,lawnLight,smoothstep(0,.95,broad));
                            grass=lerp(grass,lawnLight,(1-lawnBand)*.045);
                            grass=lerp(grass,grassLight,entrance*.72);
                            path=lerp(lane*.86,lerp(lane,softLight,.23),center*.78+.12);
                        }
                        // Lawn bands read a little more clearly; the playable lane stays soft.
                        grass*=lerp(.955,1.045,lawnBand);
                        path*=lerp(.982,1.022,pathBand);
                        if(_GroundBrushAmount>.001)
                        {
                            // Mirrored, gently warped world UVs keep brush marks attached to the island,
                            // continuous across tile edges and independent of the current camera position.
                            float2 brushPoint=float2(i.lane.x*.11+s*.007,s*.09+sin(i.lane.x*.7)*.03);
                            float2 brushUV=.01+(1-abs(frac(brushPoint+float2(.21,.33))*2-1))*.98;
                            half3 lawnPaint=SAMPLE_TEXTURE2D(_GroundBrushLawn,sampler_GroundBrushLawn,brushUV).rgb;
                            half3 pathPaint=SAMPLE_TEXTURE2D(_GroundBrushPath,sampler_GroundBrushPath,brushUV.yx).rgb;
                            half3 weights=half3(.22,.70,.08);
                            half lawnValue=dot(lawnPaint,weights)/max(.01,dot(MapColor(half3(.40,.66,.13)),weights));
                            half pathValue=dot(pathPaint,weights)/max(.01,dot(MapColor(half3(.98,.74,.43)),weights));
                            grass*=lerp(1,clamp(lawnValue,.68,1.28),.10*_GroundBrushAmount);
                            path*=lerp(1,clamp(pathValue,.72,1.25),.14*_GroundBrushAmount);
                            grass=lerp(grass,grassLight,saturate((lawnValue-1)*2)*.02*_GroundBrushAmount);
                        }
                        // A long soft satin reflection breaks up the path without painting obstacles into it.
                        float swash=exp(-pow((i.lane.x+.20-sin(s*.43)*.28)/.60,2))*pow(.5+.5*cos(s*.74+.3),6);
                        path=lerp(path,lerp(path,MapColor(half3(1,.99,.92)),.42),swash*.24);
                        float2 dapples=GroundDapples(float2(i.lane.x,s));
                        float awayFromRim=smoothstep(.12,.35,abs(distance));
                        half3 stainPaint=lerp(grass* .82,lerp(grass,grassLight,.65),step(.48,GroundPatchHash(floor(s/1.8),3.71)));
                        grass=lerp(grass,stainPaint,dapples.x*.46*awayFromRim);
                        grass=lerp(grass,grassLight,dapples.y*.54*awayFromRim);
                        path=lerp(path,path*1.10,dapples.x*.18*awayFromRim);
                        path=lerp(path,lerp(path,MapColor(half3(1,.99,.92)),.20),dapples.y*.18*awayFromRim);
                        bankSurface=grass;
                        art=lerp(grass,path,1-smoothstep(-.018,.022,distance));
                        art=lerp(art,grass*.90,wall*.62);
                        art=lerp(art,grass*1.10,lip*.42);
                    }
                    c = _UseTerrainTiles>.5?art:art*i.col.rgb;
                    if(_SimpleGrade>.5&&_SnowCoverage>.001)
                    {
                        float s=i.uv.y*14;
                        float laneSnow=1-smoothstep(-.018,.022,distance);
                        // Broad rounded snow banks and exposed grass islands, attached to the terrain.
                        float accumulation=.52+.17*sin(s*.55+i.lane.x*.8)+.12*sin(s*1.24-i.lane.x*.62)
                            +.055*sin(s*2.31+i.lane.x*2.2);
                        float scallop=.035*sin(s*3.7)+.018*sin(s*7.1+1.4);
                        float rim=exp(-pow((distance-.09-scallop)/.18,2));
                        float threshold=1.02-_SnowCoverage*.55;
                        float edgeOffset=accumulation+rim*.15-threshold;
                        float feather=max(.006,fwidth(accumulation+rim*.15)*1.5);
                        float cover=smoothstep(-feather,feather,edgeOffset);
                        float laneCover=_SnowCoverage*.22;
                        float pathBand=GroundPatches(s,clamp(i.lane.x,-3.2,3.2),2.7,.31);
                        float lawnPhase=i.lane.x<0?1.05:2.20;
                        float lawnBand=GroundPatches(s+lawnPhase,clamp((abs(i.lane.x)-1.18)*.45,0,1.8),3.45,i.lane.x<0?2.37:5.81);
                        half3 snowLawn=lerp(MapColor(half3(.78,.86,.97)),MapColor(half3(1,.985,.935)),.65+.14*sin(s*.38+i.lane.x*.7));
                        half3 snowPath=lerp(MapColor(half3(.64,.79,.94)),MapColor(half3(.87,.94,.99)),.60+.12*cos(i.lane.x*1.4));
                        snowLawn*=lerp(.976,1.018,lawnBand);snowPath*=lerp(.984,1.016,pathBand);
                        float2 flecks=GroundDapples(float2(i.lane.x,s));
                        snowLawn=lerp(snowLawn,MapColor(half3(1,.995,.97)),flecks.x*.25+flecks.y*.50);
                        snowPath=lerp(snowPath,MapColor(half3(.97,.99,1)),flecks.y*.15);
                        // A cool contact edge and a cream ridge give the accumulation thickness.
                        float edgeShade=1-smoothstep(.008,.030,edgeOffset);
                        float crest=exp(-pow((edgeOffset-.045)/.035,2));
                        snowLawn=lerp(snowLawn,MapColor(half3(.60,.73,.90)),edgeShade*.65);
                        snowLawn=lerp(snowLawn,MapColor(half3(1,.995,.97)),crest*.55);
                        snowLawn=lerp(snowLawn,MapColor(half3(1,.995,.97)),rim*.42);
                        half3 winter=lerp(snowLawn,snowPath,laneSnow);
                        winter=lerp(winter,snowLawn*MapColor(half3(.86,.90,.97)),wall*.60);
                        winter=lerp(winter,MapColor(half3(1,.995,.97)),lip*.42);
                        c=lerp(c,winter,lerp(cover,laneCover,laneSnow));
                        // A few rounded heaps with a shaded front and raised glossy crown.
                        // They sit on the lawn, leaving the route and most of the green visible.
                        float row=floor(s/3.8),side=i.lane.x<0?-1:1;
                        float seed=GroundPatchHash(row,side<0?4.17:8.93);
                        float2 center=float2(side*(1.75+seed*.95),row*3.8+1.55+seed*.45);
                        float2 mound=(float2(i.lane.x,s)-center)/float2(.46+seed*.22,.40+seed*.20);
                        float2 small=(float2(i.lane.x,s)-center-float2(side*.38,.22))/float2(.28,.30);
                        float radius=min(length(mound),length(small));
                        float aa=max(.012,fwidth(radius)*1.2);
                        float show=smoothstep(.04,.20,_SnowCoverage)*step(seed,.22+_SnowCoverage*.72)*smoothstep(.08,.28,distance);
                        float heap=(1-smoothstep(1-aa,1+aa,radius))*show;
                        float shadowShape=min(length(mound-float2(.12,-.20)),length(small-float2(.12,-.20)));
                        float shadow=(1-smoothstep(.86,1.18,shadowShape))*show*(1-heap);
                        c*=1-shadow*.28;
                        float2 dome=length(mound)<length(small)?mound:small;
                        float top=sqrt(saturate(1-radius*radius));
                        float light=saturate(.26+top*.62-dome.x*.18+dome.y*.27);
                        half3 heapColor=lerp(MapColor(half3(.43,.62,.86)),MapColor(half3(1,.99,.94)),light);
                        float shine=exp(-pow((dome.x+.24)/.42,2)-pow((dome.y-.30)/.25,2));
                        heapColor=lerp(heapColor,MapColor(half3(1,.995,.97)),shine*.95);
                        c=lerp(c,heapColor,heap);
                        // Occasional little heaps just inside the trail edge; its center stays readable.
                        float trailRow=floor(s/5.8),trailSeed=GroundPatchHash(trailRow,11.73);
                        float trailSide=trailSeed<.5?-1:1;
                        float2 trailPoint=(float2(i.lane.x,s)-float2(trailSide*.85,trailRow*5.8+1.7))/float2(.25,.31);
                        float trailRadius=length(trailPoint);
                        float trailAA=max(.018,fwidth(trailRadius));
                        float trailMask=(1-smoothstep(1-trailAA,1+trailAA,trailRadius))*laneSnow
                            *smoothstep(.12,.50,_SnowCoverage)*step(trailSeed,.68);
                        float trailTop=sqrt(saturate(1-trailRadius*trailRadius));
                        float trailLight=saturate(.28+trailTop*.62-trailPoint.x*.15+trailPoint.y*.25);
                        half3 trailPaint=lerp(MapColor(half3(.52,.70,.90)),MapColor(half3(1,.995,.97)),trailLight);
                        float trailGloss=exp(-pow((trailPoint.x+.23)/.42,2)-pow((trailPoint.y-.28)/.24,2));
                        trailPaint=lerp(trailPaint,MapColor(half3(1,1,.98)),trailGloss*.9);
                        c=lerp(c,trailPaint,trailMask);
                    }
                    else if(_SimpleGrade<.5)
                    {
                    // Legacy/custom atlas treatment remains available outside the painted map.
                    float sn=.5+.20*sin(i.lane.x*4+sin(i.uv.y*14*.8))+.15*sin(i.uv.y*14*2.3-i.lane.x*2.4)+.06*sin(i.uv.y*14*7+i.lane.x*6);
                    float snow=smoothstep(.98-_SnowCoverage,1.035-_SnowCoverage,sn)*step(.001,_SnowCoverage);
                    float laneSnow=1-smoothstep(-.02,.02,distance);
                    half3 snowColor=lerp(half3(.70,.84,.94),half3(.94,.96,.98),laneSnow);
                    snowColor*=.97+.025*sin(i.uv.y*14*1.6+i.lane.x*.4);
                    snowColor=lerp(snowColor,snowColor*facing,wall*.8);
                    snowColor+=lip*.035;
                    c=lerp(c,snowColor,snow*.95);
                    if(_HasSnowTex>.5&&_SnowCoverage>.001)
                    {
                        float2 tile=.025+abs(frac(float2(i.lane.x,i.uv.y*14)*.125)*2-1)*.45;
                        float row=_LavaTerrain>.5?0:.5;row=lerp(row,.5-row,_SnowRowFlip);
                        half3 winterLawn=SAMPLE_TEXTURE2D(_SnowTex,sampler_SnowTex,_SnowRect.xy+(tile+float2(0,row))*_SnowRect.zw).rgb;
                        half3 winterPath=SAMPLE_TEXTURE2D(_SnowTex,sampler_SnowTex,_SnowRect.xy+(tile+float2(.5,row))*_SnowRect.zw).rgb;
                        half3 winter=lerp(winterLawn,winterPath,laneSnow);
                        winter=lerp(winter,winterLawn*facing,wall*.7)+lip*.025;
                        c=lerp(art,winter,saturate(_SnowCoverage/.72));
                    }
                    }
                }
                if(_GroundMode>.5&&i.col.a>1.5)
                {
                    float4 deckRect=i.col.a>3.5?_BridgeLogsRect:i.col.a>2.5?_BridgeWoodRect:_BridgeRect;
                    float2 deckUV=_SimpleBase>.5?i.uv:float2(i.uv.y,1-i.uv.x);
                    half4 deck=SAMPLE_TEXTURE2D(_BridgeTex,sampler_BridgeTex,deckRect.xy+deckUV*deckRect.zw);
                    if(i.col.a>2.5&&i.col.a<3.5)deck=SAMPLE_TEXTURE2D(_BridgeWoodTex,sampler_BridgeWoodTex,deckRect.xy+deckUV*deckRect.zw);
                    clip(deck.a-.5);c=deck.rgb;
                }
                if (_TokenMode > .5)
                {
                    float2 uv = i.uv;
                    // Soft enamel gradient and a narrow curved reflection, with a separate cream rim.
                    float2 p=(uv-.5)*2;
                    half radial=length(p);
                    half gradient=smoothstep(-1,1,p.y);
                    half3 face=lerp(_FaceColor.rgb*.83,lerp(_FaceColor.rgb,MapColor(half3(.98,.96,1)),.39),gradient);
                    half edge=smoothstep(.77,.91,radial);
                    face*=1-edge*.055;
                    half glint=exp(-pow((radial-.82)/max(.026,fwidth(radial)*1.2),2))*smoothstep(.05,.6,p.y)*smoothstep(-.15,.5,-p.x)*.64;
                    face=lerp(face,MapColor(half3(1,.98,.94)),glint);
                    half sheen=exp(-pow((p.x+.28)/.60,2)-pow((p.y-.48)/.23,2))*.12;
                    face=lerp(face,MapColor(half3(1,.99,.95)),sheen);
                    if(_TokenFaceAmount>.5)
                    {
                        float2 faceUV=(uv-.5)*1.074766+.5;
                        half4 paintedFace=SAMPLE_TEXTURE2D(_TokenFaceTex,sampler_TokenFaceTex,_TokenFaceRect.xy+faceUV*_TokenFaceRect.zw);
                        face=lerp(face,paintedFace.rgb,paintedFace.a);
                    }
                    c = lerp(c, face, i.col.a);
                }
                half luma = dot(c, half3(0.3, 0.59, 0.11));
                c = lerp(c, luma.xxx, _Desat);
                c += _Glow * half3(0.35, 0.28, 0.12);
                c *= lerp(half3(1,1,1), _NightColor.rgb, _NightAmount);
                c += _Glow * _NightAmount * half3(0.35, 0.28, 0.12);   // medalhao ativo brilha mais a noite
                half h = saturate((i.depth - _HazeNear) / max(0.01, _HazeFar - _HazeNear));
                c = lerp(c, _HazeColor.rgb, h * h * 0.85);
                return half4(c, 1);
            }
            ENDHLSL
        }
    }
}
