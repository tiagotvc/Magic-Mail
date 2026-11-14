Shader "RoyalAves/Explosion"
{
 Properties {
 _MainTex("Albedo - 6x6",2D)="black"{}
 [HDR] _Color("Albedo",Color)=(1,.45,.1,1)
 [HDR] _EmissionColor("Emission",Color)=(3,.9,.12,1)
 [Toggle] _FlipbookBlending("Flipbook Frame Blending",Float)=1
 [Toggle] _SoftParticlesEnabled("Soft Particles",Float)=1
 _SoftParticlesFarFadeDistance("Far Fade",Float)=2
 [HideInInspector] _UseSceneDepth("Scene Depth Available",Float)=0
 }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
 Cull Off ZWrite Off ZTest Always Lighting Off Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata {float4 vertex:POSITION;float4 color:COLOR;float4 uv:TEXCOORD0;float blend:TEXCOORD1;};
 struct v2f {float4 pos:SV_POSITION;float4 color:COLOR;float4 uv:TEXCOORD0;float blend:TEXCOORD1;float4 screen:TEXCOORD2;float eye:TEXCOORD3;};
 sampler2D _MainTex;UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
 float4 _Color,_EmissionColor;float _FlipbookBlending,_SoftParticlesEnabled,_SoftParticlesFarFadeDistance,_UseSceneDepth;
 v2f vert(appdata v){v2f o;o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;o.blend=v.blend;o.screen=ComputeScreenPos(o.pos);o.eye=-UnityObjectToViewPos(v.vertex).z;return o;}
 float4 frag(v2f i):SV_Target {
 float3 tex=lerp(tex2D(_MainTex,i.uv.xy).rgb,tex2D(_MainTex,i.uv.zw).rgb,saturate(i.blend)*_FlipbookBlending);
 float density=max(tex.r,max(tex.g,tex.b));
 float alpha=saturate(density*2)*i.color.a*_Color.a;
 if(_SoftParticlesEnabled>.5 && _UseSceneDepth>.5){float scene=LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture,UNITY_PROJ_COORD(i.screen)));alpha*=saturate((scene-i.eye)/max(.001,_SoftParticlesFarFadeDistance));}
 return float4(tex*i.color.rgb*(_Color.rgb+_EmissionColor.rgb),alpha);
 }
 ENDCG }
 }
}
