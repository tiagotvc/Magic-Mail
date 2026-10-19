Shader "RoyalAves/UI/MapNavigation"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
        _PanelSize("Panel size",Vector)=(1080,205,0,0)
        _SelectedCell("Selected tab",Float)=0
        _BasePaint("Painted purple backplate",2D)="white"{}
        _ActivePaint("Painted lilac backplate",2D)="white"{}
        _BasePaintRect("Purple artwork bounds",Vector)=(0,0,1,1)
        _ActivePaintRect("Lilac artwork bounds",Vector)=(0,0,1,1)
        _UsePaintedNav("Use painted backplates",Float)=0
        _QuietChrome("Restrained postal chrome",Float)=0
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
            float4 _PanelSize,_ClipRect;
            float _SelectedCell;
            sampler2D _BasePaint,_ActivePaint;
            float4 _BasePaintRect,_ActivePaintRect;
            float _UsePaintedNav,_QuietChrome;
            v2f vert(appdata v)
            {v2f o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
            float RoundedDistance(float2 p,float2 lo,float2 hi,float4 radii)
            {
                float2 center=(lo+hi)*.5;
                float radius=p.y>center.y?(p.x<center.x?radii.x:radii.y):(p.x<center.x?radii.z:radii.w);
                float2 q=abs(p-center)-(hi-lo)*.5+radius;
                return min(max(q.x,q.y),0)+length(max(q,0))-radius;
            }
            float Coverage(float distance)
            {float aa=max(.45,fwidth(distance)*.65);return 1-smoothstep(-aa,aa,distance);}
            float3 Gold(float t)
            {
                float3 c=lerp(float3(.43,.17,.025),float3(.85,.41,.055),saturate(t/.13));
                c=lerp(c,float3(1,.66,.13),saturate((t-.13)/.24));
                c=lerp(c,float3(1,.88,.34),saturate((t-.37)/.20));
                c=lerp(c,float3(1,.97,.72),saturate((t-.57)/.16));
                c=lerp(c,float3(1,.81,.32),saturate((t-.73)/.18));
                return lerp(c,float3(.91,.58,.13),saturate((t-.91)/.09));
            }
            float4 frag(v2f i):SV_Target
            {
                float2 size=_PanelSize.xy,p=i.uv*size;
                float rail=lerp(12.5,5,_QuietChrome),body=size.y-rail,radius=min(84,size.y*.412);
                float bodyDistance=RoundedDistance(p,float2(0,0),float2(size.x,body),float4(4,4,radius,radius));
                float bodyAlpha=Coverage(bodyDistance);
                float cellWidth=size.x/5,id=min(4,floor(p.x/cellWidth));
                float chosen=1-step(.5,abs(id-_SelectedCell));
                float2 lo=float2(id*cellWidth+1.0,1.5),hi=float2((id+1)*cellWidth-1.0,body-1.2);
                float4 radii=float4(3.5,3.5,id<.5?radius-1.5:0,id>3.5?radius-1.5:0);
                float d=RoundedDistance(p,lo,hi,radii),depth=max(0,-d);
                float t=saturate(p.y/body);
                float3 low=lerp(float3(.294,.082,.537),float3(.545,.224,.918),chosen);
                float3 high=lerp(float3(.608,.235,.757),float3(.788,.475,.996),chosen);
                float3 face=lerp(low,high,t);
                // A shallow molded lip follows all four edges, including the curved outside corners.
                // Its highlight and inner shadow stay within the piece rather than glowing outside it.
                float2 gradient=float2(
                    RoundedDistance(p+float2(.5,0),lo,hi,radii)-RoundedDistance(p-float2(.5,0),lo,hi,radii),
                    RoundedDistance(p+float2(0,.5),lo,hi,radii)-RoundedDistance(p-float2(0,.5),lo,hi,radii));
                float2 normal=gradient/max(.001,length(gradient));
                float facing=saturate(dot(normal,normalize(float2(-.55,.83))));
                float shade=exp(-pow((depth-4.2)/2.7,2))*(.045+.10*saturate(dot(normal,normalize(float2(.6,-.8)))));
                face*=1-shade*float3(.9,.9,.28);
                float lip=exp(-depth/2.3)*facing*lerp(.42,.70,chosen);
                face=lerp(face,float3(.98,.72,1),lip);
                float stroke=exp(-pow((depth-.8)/.65,2));
                face=lerp(face,lerp(float3(.67,.29,.88),float3(.91,.64,1),chosen),stroke*lerp(.42,.65,chosen));
                float topSatin=exp(-pow((body-p.y-6)/6,2))*.045;
                face=lerp(face,float3(1,.80,1),topSatin);
                float localX=(p.x-id*cellWidth)/cellWidth;
                face+=chosen*exp(-pow(localX/.85,2)-pow((1-t)/.24,2))*.022;
                // The seam is purple, not black; the neighboring bevel provides its fine lit edge.
                float3 color=lerp(float3(.36,.095,.51),face,Coverage(d));
                float outerStroke=exp(-pow((max(0,-bodyDistance)-1)/.8,2));
                color=lerp(color,lerp(float3(.58,.19,.78),float3(.77,.43,.97),chosen),outerStroke*.60);
                color*=1-exp(-max(0,body-p.y)/1.6)*.22;
                float goldDistance=RoundedDistance(p,float2(0,body-.4),size,float4(3.5,3.5,1.5,1.5));
                float goldAlpha=Coverage(goldDistance);
                float goldT=saturate((p.y-body)/rail);
                float3 gold=Gold(goldT);
                float glint=exp(-pow((i.uv.x-.50)/.032,2)-pow((goldT-.70)/.14,2))*.45;
                gold=lerp(gold,float3(1,1,.91),glint);
                color=lerp(color,gold,goldAlpha);
                float alpha=max(bodyAlpha,goldAlpha)*i.color.a;
                if(_UsePaintedNav>.5)
                {
                    // Keep the original generated pixels intact. Artwork is sampled inside its
                    // transparent safety margin and composited below the unchanged icon hierarchy.
                    float2 paintUV=i.uv;
                    paintUV.y=p.y<body?t*.9296:.9296+goldT*.0704;
                    float4 purple=tex2D(_BasePaint,_BasePaintRect.xy+paintUV*_BasePaintRect.zw);
                    float4 lilac=tex2D(_ActivePaint,_ActivePaintRect.xy+paintUV*_ActivePaintRect.zw);
                    purple.rgb=saturate(purple.rgb+float3(.085,.03+.06*t,.04));
                    lilac.rgb=saturate(lilac.rgb+float3(lerp(.085,0,t),lerp(.055,-.05,t),-.025));
                    float4 painted=lerp(purple,lilac,chosen*(1-goldAlpha));
                    // The gold face uses its painted yellow upper lobe and amber lower lobe unchanged.
                    float4 paintedGold=tex2D(_BasePaint,_BasePaintRect.xy+paintUV*_BasePaintRect.zw);
                    painted=lerp(painted,paintedGold,goldAlpha);
                    color=lerp(color,painted.rgb,smoothstep(.55,.98,painted.a));
                    alpha*=smoothstep(.05,.9,painted.a);
                }
                if(_QuietChrome>.5)
                {
                    float3 quietLow=lerp(float3(.29,.20,.43),float3(.47,.31,.64),chosen);
                    float3 quietHigh=lerp(float3(.43,.30,.56),float3(.66,.47,.79),chosen);
                    float3 quietFace=lerp(quietLow,quietHigh,t);
                    quietFace=lerp(quietFace,float3(.82,.69,.92),exp(-depth/1.8)*facing*.28);
                    color=lerp(quietFace,Gold(goldT),goldAlpha*.85);
                    alpha=max(bodyAlpha,goldAlpha)*i.color.a;
                }
                #ifdef UNITY_UI_CLIP_RECT
                alpha*=UnityGet2DClipping(i.local.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha-.001);
                #endif
                #ifndef UNITY_COLORSPACE_GAMMA
                color=GammaToLinearSpace(color);
                #endif
                return float4(color*i.color.rgb,alpha);
            }
            ENDCG
        }
    }
}
