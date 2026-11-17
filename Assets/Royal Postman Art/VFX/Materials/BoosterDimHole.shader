// Escurecimento da tela de power-up: cobre a tela toda de preto, menos onde a máscara _HoleTex diz que é o tabuleiro
// (a máscara tem o formato real do tabuleiro, com cantos arredondados). _HoleRect = onde a máscara cai na tela (0..1).
Shader "RoyalAves/BoosterDimHole"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
  _Color("Tint", Color) = (1,1,1,1)
  _HoleTex("Hole mask", 2D) = "black" {}
  _HoleRect("Hole rect (minX,minY,maxX,maxY)", Vector) = (0,0,0,0)
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Cull Off ZWrite Off ZTest Always Lighting Off
  Blend SrcAlpha OneMinusSrcAlpha
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   sampler2D _HoleTex;
   fixed4 _Color;
   float4 _HoleRect;
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
   struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
   v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
   fixed4 frag(v2f i):SV_Target
   {
    float2 size = max(_HoleRect.zw - _HoleRect.xy, 1e-5);
    float2 h = (i.uv - _HoleRect.xy) / size;
    float inside = step(0, h.x) * step(h.x, 1) * step(0, h.y) * step(h.y, 1);
    float hole = tex2D(_HoleTex, h).a * inside;
    fixed4 c = i.color;
    c.a *= (1 - hole);
    return c;
   }
   ENDCG
  }
 }
}
