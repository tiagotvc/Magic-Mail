Shader "RoyalAves/MulticolorTargetCharge"
{
    Properties
    {
        _MainTex ("Sprite", 2D) = "white" {}
        _GlowColor ("Charged light", Color) = (1, .98, .86, 1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Cull Off Lighting Off ZWrite Off
        Blend SrcAlpha One
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            sampler2D _MainTex;
            fixed4 _GlowColor;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color;return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 sprite=tex2D(_MainTex,i.uv);
                float sweep=pow(saturate(1-abs(frac((i.uv.x+i.uv.y)*.55-_Time.y*.9)*2-1)),10);
                // Fill the available brightness instead of adding flat white and clipping the art.
                float3 light=max(0,1-sprite.rgb)*_GlowColor.rgb*(.78+sweep*.2);
                return fixed4(light,sprite.a*i.color.a);
            }
            ENDCG
        }
    }
}
