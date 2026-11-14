Shader "RoyalAves/DynamiteFuse"
{
 Properties {
 [PerRendererData] _MainTex("Sprite",2D)="white"{}
 _Color("Tint",Color)=(1,1,1,1)
 _BurnY("Burn front",Float)=1
 _Flame("Flame mode",Float)=0
 _Age("Animation time",Float)=0
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
 Cull Off ZWrite Off Lighting Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex;fixed4 _Color;float _BurnY,_Flame,_Age;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 float segment(float2 p,float2 a,float2 b){float2 d=b-a;return length(p-a-d*saturate(dot(p-a,d)/max(dot(d,d),.00001)));}
 fixed4 frag(v2f i):SV_Target {
 if(_Flame<.5){
 fixed4 c=tex2D(_MainTex,i.uv)*i.color;
 // Only the ivory rope burns; the red sticks and approved body stay intact.
 float rope=step(.39,i.uv.x)*step(i.uv.x,.62)*step(.61,i.uv.y)*max(step(.735,i.uv.y),step(c.r*.52,c.g));
 if(rope>.5){
 c.a*=1-smoothstep(_BurnY-.008,_BurnY+.008,i.uv.y);
 float ember=smoothstep(_BurnY-.035,_BurnY-.008,i.uv.y);
 c.rgb=lerp(c.rgb,float3(1,.22,.015),ember);
 }return c;
 }
 // Compact, sputtering ignition at the rope, with separate flying hot particles.
 float2 p=i.uv-float2(.5,.35);
 float angle=atan2(p.y,p.x);
 float radius=length(p);
 float2 firePoint=p*(1.35/.30);
 float fireRadius=length(firePoint);
 float flicker=.078+.012*sin(_Age*81)+.008*sin(_Age*137);
 float rim=flicker*(1+.19*sin(angle*5+_Age*39)+.12*sin(angle*9-_Age*53));
 float coal=1-smoothstep(rim*.65,rim,fireRadius);
 float fire=0;
 for(int j=0;j<6;j++){
 float n=(float)j;
 float theta=n*1.0472+sin(_Age*23+n*3)*.25;
 float2 dir=float2(cos(theta),sin(theta));
 float reach=.10+.055*(.5+.5*sin(_Age*47+n*7));
 float d=segment(firePoint,dir*.025,dir*reach);
 float taper=lerp(.028,.003,saturate(fireRadius/reach));
 fire=max(fire,1-smoothstep(taper*.45,taper,d));
 }
 float sparks=0;
 for(int k=0;k<9;k++){
 float n=(float)k;
 float cycle=_Age*5.5+n*.173;
 float t=frac(cycle);
 float theta=n*2.39996+floor(cycle)*1.73;
 float2 dir=float2(cos(theta),sin(theta));
 float2 pos=dir*(.065+t*.41)+float2(0,.06*t-.08*t*t);
 float d=segment(p,pos,pos-dir*(.042+.072*t));
 sparks=max(sparks,(1-smoothstep(.006,.024*(1-t*.55),d))*(1-t*.65));
 }
 float halo=(1-smoothstep(.05,.20,fireRadius))*.18;
 float alpha=saturate(max(coal,fire*.92)+sparks+halo);
 float3 color=lerp(float3(1,.22,.015),float3(1,.78,.13),max(coal,sparks));
 color=lerp(color,float3(1,1,.8),1-smoothstep(.018,.058,fireRadius));
 return float4(color,alpha)*i.color;
 }
 ENDCG
 }
 }
}
