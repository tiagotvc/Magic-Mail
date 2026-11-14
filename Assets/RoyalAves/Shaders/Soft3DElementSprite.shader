Shader "RoyalAves/Soft3DElementSprite"
{
 Properties { [PerRendererData] _MainTex("Texture",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Mode("Mode",Float)=0 _LensColor("Lens",Color)=(1,0,0,1) }
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 struct v2f {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
 sampler2D _MainTex;fixed4 _Color,_LensColor;float _Mode;
 v2f vert(appdata v){v2f o;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
 fixed4 frag(v2f i):SV_Target
 {
  fixed4 c=tex2D(_MainTex,i.uv);
  if(_Mode>.5 && c.g>c.r*1.15 && c.g>c.b*1.18)
  {
   float2 p=(i.uv-float2(.5,.55))/.29;
   float light=saturate(.72+.14*p.y-.12*p.x);
   c.rgb=_LensColor.rgb*light;
   c.rgb=lerp(c.rgb,float3(1,1,1),.22*pow(saturate(1-length(p-float2(-.25,.35))),5));
  }
  c.a=smoothstep(.65,.92,c.a);
  return c*i.color;
 }
 ENDCG }
 }
}
