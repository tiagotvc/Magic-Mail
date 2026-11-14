Shader "RoyalAves/DynamiteBlast"
{
 Properties { _Age("Age",Float)=0 _Mode("Mode",Float)=0 _Seed("Seed",Float)=0 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent"}
 Cull Off ZWrite Off ZTest Always Lighting Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };
 float _Age,_Mode,_Seed;
 float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
 float noise(float2 p){float2 c=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(c),hash(c+float2(1,0)),f.x),lerp(hash(c+float2(0,1)),hash(c+1),f.x),f.y);}
 v2f vert(appdata_base v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.texcoord.xy;return o;}
 fixed4 frag(v2f i):SV_Target {
 float2 p=i.uv*2-1;float r=length(p);float a=atan2(p.y,p.x);
 float turbulence=noise(p*5+_Seed+_Age*1.7);
 float edge=.80+.065*sin(a*3+_Seed)+.04*sin(a*5-_Seed)+(turbulence-.5)*.08;
 if(_Mode>1.5){float alpha=(1-smoothstep(edge-.18,edge,r))*sin(saturate(_Age)*3.14159)*.46;return float4(lerp(float3(.29,.22,.18),float3(.56,.48,.37),saturate(p.y*.5+.5)),alpha);}
 if(_Mode>.5){float wave=exp(-pow((r-edge)*26,2));float spokes=pow(saturate(.5+.5*sin(a*15+_Seed)),12)*smoothstep(.2,.5,r)*(1-smoothstep(.7,.96,r));return float4(1,.78,.32,(wave*.7+spokes*.28)*pow(1-_Age,2));}
 float body=1-smoothstep(edge-.10,edge,r);
 float core=1-smoothstep(.26,.77,r+_Age*.20+(turbulence-.5)*.35);
 float3 color=lerp(float3(.94,.20,.025),float3(1,.68,.07),saturate(1-r));color=lerp(color,float3(1,1,.83),core);
 return float4(color,body*pow(saturate(1-_Age),.7));
 }
 ENDCG }
 }
}
