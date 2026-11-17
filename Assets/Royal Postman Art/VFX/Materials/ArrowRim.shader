// Brilho SÓ no contorno de um sprite (usado na Flecha Postal): desenha, por fora da silhueta, uma luz que vai apagando com a
// distância, e nada por dentro. O sprite precisa estar com Mesh Type = Full Rect (a luz sai um pouco da silhueta, dentro da
// margem transparente da imagem). _Radius é a distância máxima do brilho em coordenadas de textura (0.03 = 3% da imagem).
Shader "RoyalAves/ArrowRim"
{
 Properties
 {
  [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
  _Color("Cor", Color) = (1,0.7,0.2,1)
  _Radius("Raio (UV)", Float) = 0.03
  _Intensity("Intensidade", Float) = 1.6
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Cull Off ZWrite Off ZTest Always Lighting Off
  Blend SrcAlpha One
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex;
   fixed4 _Color;
   float _Radius;
   float _Intensity;
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
   struct v2f { float4 pos:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
   v2f vert(appdata v){ v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
   fixed4 frag(v2f i):SV_Target
   {
    float inside = tex2D(_MainTex, i.uv).a;
    float glow = 0;
    float weight = 0;
    // 3 anéis de 8 amostras: quanto mais perto da silhueta, mais forte
    for (int k = 1; k <= 3; k++)
    {
     float r = _Radius * k / 3.0;
     float w = 1.0 / k;
     for (int d = 0; d < 8; d++)
     {
      float a = d * 0.785398;
      glow += tex2D(_MainTex, i.uv + float2(cos(a), sin(a)) * r).a * w;
      weight += w;
     }
    }
    glow /= weight;
    float rim = saturate(glow * 1.8) * (1.0 - smoothstep(0.15, 0.7, inside));
    return fixed4(_Color.rgb * i.color.rgb, saturate(rim * _Intensity) * i.color.a);
   }
   ENDCG
  }
 }
}
