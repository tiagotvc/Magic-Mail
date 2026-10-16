Shader "RoyalAves/UI/PostalArtwork"
{
    Properties
    {
        [PerRendererData] _MainTex("Artwork",2D)="white"{}
        _FooterPalette("Lavender footer",Float)=0
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
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct input {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;};
            struct output {float4 vertex:SV_POSITION;fixed4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;};
            sampler2D _MainTex;
            float4 _ClipRect;
            float _FooterPalette;
            output vert(input v){output o;o.local=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv;return o;}
            fixed4 frag(output i):SV_Target
            {
                fixed4 c=tex2D(_MainTex,i.uv);
                // Limit palette grading to the cream center; painted gold, purple and crystal stay intact.
                float center=smoothstep(.338,.345,i.uv.y)*(1-smoothstep(.595,.602,i.uv.y))*_FooterPalette;
                float3 lavender=float3(.70,.48,.90);
                #ifndef UNITY_COLORSPACE_GAMMA
                lavender=GammaToLinearSpace(lavender);
                #endif
                float light=dot(c.rgb,float3(.2126,.7152,.0722));
                c.rgb=lerp(c.rgb,lavender*(.94+.14*light),center);
                // Reject low-coverage extraction fringe without changing the opaque painted art.
                c.a=smoothstep(.68,.98,c.a);c*=i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a*=UnityGet2DClipping(i.local.xy,_ClipRect);
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
