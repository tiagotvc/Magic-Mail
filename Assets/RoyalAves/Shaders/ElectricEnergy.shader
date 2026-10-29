Shader "RoyalAves/ElectricEnergy"
{
    Properties
    {
        _Mode ("0 ribbon, 1 star, 2 ring", Float) = 0
        [HideInInspector] _SrcBlend ("Source blend", Float) = 5
        [HideInInspector] _DstBlend ("Destination blend", Float) = 1
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off Lighting Off
        Blend [_SrcBlend] [_DstBlend]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            float _Mode;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                float2 p=i.uv*2-1;
                float light;
                if (_Mode < .5)
                {
                    // Soft transverse profile, with packets of light travelling towards the target.
                    float width=abs(p.y);
                    float packet=pow(.5+.5*cos(i.uv.x*14-_Time.y*26),8);
                    light=(exp(-width*width*19)*.86+exp(-width*width*3.5)*.17)*(.94+packet*.16);
                    light*=smoothstep(0,.025,i.uv.x)*smoothstep(1,.97,i.uv.x);
                }
                else if (_Mode < 1.5)
                {
                    float r=length(p);
                    float star=exp(-abs(p.x)*26-p.y*p.y*5)+exp(-abs(p.y)*26-p.x*p.x*5);
                    light=saturate(star*.65+exp(-r*r*12)*.6)*saturate(1-r*r);
                }
                else
                {
                    float r=length(p);
                    light=exp(-pow((r-.72)*22,2))*.7+exp(-r*r*7)*.2;
                    light*=saturate((1-r)*12);
                }
                return fixed4(i.color.rgb, saturate(light)*i.color.a);
            }
            ENDCG
        }
    }
}
