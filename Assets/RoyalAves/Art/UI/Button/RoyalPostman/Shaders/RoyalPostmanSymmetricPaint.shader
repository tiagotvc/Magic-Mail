Shader "RoyalPostman/UI/SymmetricPaint"
{
    Properties
    {
        [PerRendererData] _MainTex ("Unchanged paint", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Saturation ("Saturation", Range(0,1)) = 1
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
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
            sampler2D _MainTex; fixed4 _Color; float4 _ClipRect; float _Saturation;
            v2f vert(appdata v) { v2f o; o.local=v.vertex.xy; o.vertex=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color*_Color; return o; }
            fixed4 frag(v2f i):SV_Target
            {
                fixed3 rgb=tex2D(_MainTex,i.uv).rgb;
                rgb=lerp(dot(rgb,fixed3(.2126,.7152,.0722)),rgb,_Saturation);
                fixed4 output=fixed4(rgb,1)*i.color;
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
