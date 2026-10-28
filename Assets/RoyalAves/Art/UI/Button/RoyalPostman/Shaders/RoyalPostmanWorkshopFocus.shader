Shader "RoyalPostman/UI/WorkshopFocus"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite",2D)="white"{}
  _Color("Tint",Color)=(1,1,1,1)
  _BlurRadius("Background blur in source pixels",Range(0,24))=12
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
  Tags{"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
  Stencil{Ref[_Stencil] Comp[_StencilComp] Pass[_StencilOp] ReadMask[_StencilReadMask] WriteMask[_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest[unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask[_ColorMask]
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
   struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;};
   struct v2f{float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;fixed4 color:COLOR;float4 world:TEXCOORD1;};
   sampler2D _MainTex;float4 _MainTex_TexelSize,_ClipRect;fixed4 _Color;float _BlurRadius;
   v2f vert(appdata v){v2f o;o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   fixed4 frag(v2f i):SV_Target
   {
    float radius=_BlurRadius*lerp(.35,1,smoothstep(.05,.4,i.uv.y));
    float2 stepUV=_MainTex_TexelSize.xy*radius*.25;
    fixed4 c=0;float total=0;
    [unroll]for(int y=-4;y<=4;y++)[unroll]for(int x=-4;x<=4;x++)
    {
     float wx=abs(x)==0?70:abs(x)==1?56:abs(x)==2?28:abs(x)==3?8:1;
     float wy=abs(y)==0?70:abs(y)==1?56:abs(y)==2?28:abs(y)==3?8:1;float weight=wx*wy;
     c+=tex2D(_MainTex,saturate(i.uv+float2(x,y)*stepUV))*weight;total+=weight;
    }
    c=c/total*i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
