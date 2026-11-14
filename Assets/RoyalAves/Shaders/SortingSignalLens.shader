Shader "RoyalAves/SortingSignalLens"
{
    Properties
    {
        [PerRendererData] _MainTex("Master original",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _LensColor("Cor solicitada",Color)=(.3,1,.2,1)
        _Mode("0 preserves selected green original",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
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
                // Preserve the chosen first icon exactly in green; other colors affect only glass.
                float2 p=(i.uv-float2(.496,.576))/.235;
                if(_Mode>.5 && length(p)<1 && c.g>c.r*1.12 && c.g>c.b*1.1)
                {
                    float shade=saturate(.72+.20*(1-length(p))+.10*p.y-.08*p.x);
                    c.rgb=lerp(_LensColor.rgb*.35,_LensColor.rgb,shade);
                    c.rgb=lerp(c.rgb,float3(1,1,1),.18*saturate(1-length(p)));
                }
                c.a=smoothstep(.18,.65,c.a);
                return c*i.color;
            }
            ENDCG
        }
    }
}
