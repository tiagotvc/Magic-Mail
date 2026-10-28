Shader "RoyalPostman/UI/NeutralPalette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Neutral sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _UseCustomPalette ("Use material palette", Range(0,1)) = 0
        _PaletteShadow ("Palette shadow", Color) = (0.6,0.17,0,1)
        _PaletteMid ("Palette midtone", Color) = (1,0.65,0,1)
        _PaletteHighlight ("Palette highlight", Color) = (1,0.97,0.22,1)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil { Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask] }
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
            struct appdata { float4 vertex:POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 uv:TEXCOORD0; float2 local:TEXCOORD1; };
            sampler2D _MainTex; fixed4 _Color; float4 _ClipRect;
            half _UseCustomPalette; half4 _PaletteShadow, _PaletteMid, _PaletteHighlight;
            v2f vert(appdata v) { v2f o; o.vertex=UnityObjectToClipPos(v.vertex); o.local=v.vertex.xy; o.uv=v.uv; o.color=v.color*_Color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 source=tex2D(_MainTex,i.uv);
                half light=dot(source.rgb,half3(.2126,.7152,.0722));
                half3 tint=i.color.rgb;
                half peak=max(tint.r,max(tint.g,tint.b));
                // Colorize the neutral relief instead of multiplying it into a dull grey tint.
                half3 shadow=tint*tint*.32;
                half3 rgb=lerp(shadow,tint,smoothstep(.15,.70,light));
                rgb=lerp(rgb,lerp(tint,half3(peak,peak,peak),.75),smoothstep(.70,.98,light));
                half3 palette=lerp(_PaletteShadow.rgb,_PaletteMid.rgb,smoothstep(.12,.62,light));
                palette=lerp(palette,_PaletteHighlight.rgb,smoothstep(.62,.96,light));
                rgb=lerp(rgb,palette*tint,_UseCustomPalette);
                fixed4 output=fixed4(rgb,source.a*i.color.a);
                #ifdef UNITY_UI_CLIP_RECT
                output.a*=UnityGet2DClipping(i.local,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(output.a-.001);
                #endif
                return output;
            }
            ENDCG
        }
    }
}
