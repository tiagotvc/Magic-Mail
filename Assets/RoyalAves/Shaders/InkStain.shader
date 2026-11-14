Shader "RoyalAves/InkStain"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _InkColor ("Ink", Color) = (.48,.025,.9,1)
        _Progress ("Fill", Range(0,1)) = 0
        _Floor ("Floor surface", Float) = 0
        _RoundCorners ("Exposed corners BL BR TL TR", Vector) = (1,1,1,1)
        _ExposedEdges ("Exposed edges L R B T", Vector) = (1,1,1,1)
        _Origin ("Impact UV", Vector) = (.5,.5,0,0)
        _LocalRect ("Sprite local rect", Vector) = (0,0,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; fixed4 color:COLOR; };
            sampler2D _MainTex;
            float4 _Color, _InkColor, _Origin, _LocalRect;
            float4 _RoundCorners, _ExposedEdges;
            float _Progress, _Floor;
            float liquid(float2 p)
            {
                return sin(p.x*21+p.y*13)*.45 + sin(p.y*32-p.x*9)*.3 + sin(p.x*47+p.y*39)*.15;
            }
            v2f vert(appdata v)
            {
                v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv;
                o.local=(v.vertex.xy-_LocalRect.xy)/max(_LocalRect.zw,float2(.001,.001));
                o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 tex=tex2D(_MainTex,i.uv);
                // The radius exceeds the farthest corner at completion: no unpainted margins.
                float2 p=i.local;
                // A lobed liquid front and thin rivulets replace the previous perfect circular wipe.
                float radius=_Progress*1.75;
                float wobble=liquid(p)*.065;
                float fingers=pow(saturate(sin(p.x*29+sin(p.x*11)*2)),8)*.075*(1-_Floor);
                float distanceToFront=distance(p,_Origin.xy)+wobble-fingers;
                float edge=1-smoothstep(radius-.025,radius+.012,distanceToFront);
                if(_Progress>=.999) edge=1;
                if(_Progress<=0) edge=0;
                float contour=1;
                float cornerDistance=10;
                const float rounding=.20;
                if (_Floor>.5)
                {
                    if(p.x<rounding && p.y<rounding && _RoundCorners.x>.5)
                        cornerDistance=rounding-distance(p,float2(rounding,rounding));
                    if(p.x>1-rounding && p.y<rounding && _RoundCorners.y>.5)
                        cornerDistance=rounding-distance(p,float2(1-rounding,rounding));
                    if(p.x<rounding && p.y>1-rounding && _RoundCorners.z>.5)
                        cornerDistance=rounding-distance(p,float2(rounding,1-rounding));
                    if(p.x>1-rounding && p.y>1-rounding && _RoundCorners.w>.5)
                        cornerDistance=rounding-distance(p,float2(1-rounding,1-rounding));
                    contour=smoothstep(-.005,.007,cornerDistance);
                }
                float light=dot(tex.rgb,float3(.299,.587,.114));
                fixed3 ink=_InkColor.rgb*(.42+light*.68);
                ink=lerp(ink,float3(.83,.57,1),pow(saturate(light),6)*.25);
                // Broad satin puddle body, darker accumulated ink and curved wet reflections.
                float pools=sin(p.x*9+sin(p.y*8)*1.2)*sin(p.y*7+p.x*3);
                ink*=1+pools*.10;
                float rimDistance=min(min(lerp(10,p.x,_ExposedEdges.x),lerp(10,1-p.x,_ExposedEdges.y)),
                    min(lerp(10,p.y,_ExposedEdges.z),lerp(10,1-p.y,_ExposedEdges.w)));
                rimDistance=min(rimDistance,cornerDistance);
                ink*=lerp(1,.72,saturate(1-rimDistance/.055)*_Floor);
                float ribbon=abs(p.y-(.76+.035*sin(p.x*11)+.02*sin(p.x*23)));
                float gloss=(1-smoothstep(.009,.025,ribbon))*smoothstep(.06,.16,p.x)*(1-smoothstep(.55,.78,p.x));
                float lower=abs(p.y-(.13+.025*sin(p.x*15)));
                gloss+=(1-smoothstep(.006,.017,lower))*smoothstep(.5,.65,p.x)*(1-smoothstep(.86,.96,p.x))*.6;
                float wetFront=1-smoothstep(.008,.037,abs(distanceToFront-radius+.025));
                wetFront*=1-step(.97,_Progress);
                ink=lerp(ink,float3(.85,.58,1),saturate(gloss*.65+wetFront*.55));
                return fixed4(ink,tex.a*i.color.a*edge*contour*_InkColor.a);
            }
            ENDCG
        }
    }
}
