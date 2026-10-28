Shader "RoyalPostman/UI/PaintedPalette"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Low ("Paint low", Color) = (0.01,0.36,0.34,1)
        _Mid ("Paint middle", Color) = (0.01,0.68,0.61,1)
        _High ("Paint highlight", Color) = (0.43,0.91,0.80,1)
        _UsePalette ("Recolor grayscale master", Float) = 1
        _ButtonPart ("Button part: 0 whole, 1 fixed gold, 2 moving center", Float) = 0
        _ButtonUVRect ("Trimmed sprite UV rectangle", Vector) = (0,0,1,1)
        _CircleCenterRadius ("Raised circular center radius", Float) = .38
        _GlyphHoleRadius ("Native glyph aperture radius", Float) = 0
        _GlyphHoleCenter ("Native glyph aperture center", Vector) = (.5,.49,0,0)
        [Toggle] _RecolorRed ("Recolor existing red paint", Float) = 0
        _TargetColor ("Button color", Color) = (0,.6,.55,1)
        [Toggle] _RecolorPurple ("Recolor purple face preserving gold", Float) = 0
        [Toggle] _RemoveCloseGlyph ("Existing close body without X", Float) = 0
        [Toggle] _ExtractCloseGlyph ("Isolate existing cream X", Float) = 0
        [Toggle] _ClipCircle ("Trim circular silhouette", Float) = 0
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
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float2 texcoord:TEXCOORD0; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct v2f { float4 vertex:SV_POSITION; fixed4 color:COLOR; float2 texcoord:TEXCOORD0; float4 worldPosition:TEXCOORD1; UNITY_VERTEX_OUTPUT_STEREO };
            sampler2D _MainTex; fixed4 _Color, _TextureSampleAdd, _Low, _Mid, _High; float4 _ClipRect, _ButtonUVRect, _GlyphHoleCenter; float _UsePalette, _ButtonPart, _CircleCenterRadius, _GlyphHoleRadius;
            fixed4 _TargetColor;float4 _MainTex_TexelSize;float _RecolorRed,_RecolorPurple,_RemoveCloseGlyph,_ExtractCloseGlyph,_ClipCircle;
            float creamMask(float2 uv)
            {
                fixed3 rgb=tex2D(_MainTex,uv).rgb;return smoothstep(.55,.70,rgb.r)*smoothstep(.45,.65,rgb.g);
            }
            float3 toHSV(float3 c)
            {
                float4 k=float4(0,-1.0/3.0,2.0/3.0,-1);
                float4 p=lerp(float4(c.bg,k.wz),float4(c.gb,k.xy),step(c.b,c.g));
                float4 q=lerp(float4(p.xyw,c.r),float4(c.r,p.yzx),step(p.x,c.r));
                float d=q.x-min(q.w,q.y);return float3(abs(q.z+(q.w-q.y)/(6*d+.00001)),d/(q.x+.00001),q.x);
            }
            float3 fromHSV(float3 c)
            {
                float3 p=abs(frac(c.xxx+float3(0,2.0/3.0,1.0/3.0))*6-3);
                return c.z*lerp(float3(1,1,1),saturate(p-1),c.y);
            }
            float closeBar(float2 p,float2 size)
            {
                float2 q=abs(p)-size+.035;return min(max(q.x,q.y),0)+length(max(q,0))-.035;
            }
            v2f vert(appdata v)
            {
                v2f o; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition=v.vertex; o.vertex=UnityObjectToClipPos(v.vertex); o.texcoord=v.texcoord; o.color=v.color*_Color; return o;
            }
            fixed4 frag(v2f i):SV_Target
            {
                fixed4 paint=tex2D(_MainTex,i.texcoord)+_TextureSampleAdd;
                float2 controlUV=(i.texcoord-_ButtonUVRect.xy)/max(_ButtonUVRect.zw,.00001);
                if(_RemoveCloseGlyph>.5)
                {
                    // UI compositing leaves the source PNG and its authored rim/highlights intact.
                    // Only the baked X footprint is filled from four unobstructed face samples.
                    float2 p=controlUV-.5,q=float2(p.x+p.y,p.y-p.x)*.70710678;
                    float footprint=min(closeBar(q,float2(.12,.34)),closeBar(q,float2(.34,.12)));
                    float mask=1-smoothstep(-.005,.025,footprint);
                    fixed3 up=tex2D(_MainTex,_ButtonUVRect.xy+_ButtonUVRect.zw*float2(.5,.78)).rgb;
                    fixed3 down=tex2D(_MainTex,_ButtonUVRect.xy+_ButtonUVRect.zw*float2(.5,.22)).rgb;
                    fixed3 left=tex2D(_MainTex,_ButtonUVRect.xy+_ButtonUVRect.zw*float2(.22,.5)).rgb;
                    fixed3 right=tex2D(_MainTex,_ButtonUVRect.xy+_ButtonUVRect.zw*float2(.78,.5)).rgb;
                    fixed3 face=saturate((up+down+left+right)*.25+(up-down)*(p.y/.56)+(right-left)*(p.x/.56));
                    paint.rgb=lerp(paint.rgb,face,mask);
                }
                if(_RecolorRed>.5||_ExtractCloseGlyph>.5)
                {
                    float3 hsv=toHSV(paint.rgb);float hueDistance=min(hsv.x,1-hsv.x);
                    float red=(1-smoothstep(.055,.09,hueDistance))*smoothstep(.02,.10,hsv.y);
                    if(_ExtractCloseGlyph>.5)
                    {
                        // Retain the original cream symbol AND its authored short outline.
                        float2 stepUV=_MainTex_TexelSize.xy*2.6;
                        float mask=creamMask(i.texcoord);
                        mask=max(mask,max(creamMask(i.texcoord+float2(stepUV.x,0)),creamMask(i.texcoord-float2(stepUV.x,0))));
                        mask=max(mask,max(creamMask(i.texcoord+float2(0,stepUV.y)),creamMask(i.texcoord-float2(0,stepUV.y))));
                        mask=max(mask,max(creamMask(i.texcoord+stepUV),creamMask(i.texcoord-stepUV)));
                        mask=max(mask,max(creamMask(i.texcoord+float2(stepUV.x,-stepUV.y)),creamMask(i.texcoord+float2(-stepUV.x,stepUV.y))));
                        float2 p=controlUV-.5,q=float2(p.x+p.y,p.y-p.x)*.70710678;
                        mask*=1-smoothstep(0,.025,min(closeBar(q,float2(.16,.50)),closeBar(q,float2(.50,.16))));
                        paint.a*=mask;
                    }
                    if(_RecolorRed>.5)
                    {
                        float3 target=toHSV(_TargetColor.rgb);
                        float signedHue=hsv.x>.5?hsv.x-1:hsv.x;
                        float3 changed=float3(frac(target.x+signedHue*.15),saturate(hsv.y*target.y/.9),hsv.z*lerp(1,target.z/.88,saturate(hsv.y/.8)));
                        paint.rgb=lerp(paint.rgb,fromHSV(changed),red);
                    }
                }
                if(_RecolorPurple>.5)
                {
                    float3 hsv=toHSV(paint.rgb),target=toHSV(_TargetColor.rgb);
                    float purple=smoothstep(.60,.67,hsv.x)*(1-smoothstep(.90,.96,hsv.x))*smoothstep(.12,.35,hsv.y);
                    paint.rgb=lerp(paint.rgb,fromHSV(float3(target.x,hsv.y,hsv.z)),purple);
                }
                if(_ClipCircle>.5)paint.a*=1-smoothstep(.496,.502,length(controlUV-.5));
                float y=paint.r; // neutral masters; also permits the red banner's separate gold underlay
                // One painted relief, with a three-tone color ramp for every state.
                float t=saturate((y-.24)/.70);
                fixed3 mapped=t<.56 ? lerp(_Low.rgb,_Mid.rgb,t/.56) : lerp(_Mid.rgb,_High.rgb,(t-.56)/.44);
                fixed4 color=fixed4(lerp(paint.rgb,mapped,saturate(_UsePalette)),paint.a)*i.color;
                // Independent UI parts retain the exact same authored fit. Only the colored
                // center moves; gold is a separate fixed Graphic, not a flattened button.
                if(_ButtonPart>2.5)
                {
                    float2 localUV=(i.texcoord-_ButtonUVRect.xy)/_ButtonUVRect.zw;
                    float center=1-smoothstep(_CircleCenterRadius-.006,_CircleCenterRadius+.006,length(localUV-.5));
                    color.a*=(_ButtonPart<3.5)?(1-center):center;
                }
                else if(_ButtonPart>.5)
                {
                    float gold=step(.015,paint.r-paint.b);
                    color.a*=(_ButtonPart<1.5)?gold:(1-gold);
                }
                if(_GlyphHoleRadius>0)
                {
                    float2 glyphUV=(i.texcoord-_ButtonUVRect.xy)/_ButtonUVRect.zw;
                    color.a*=smoothstep(_GlyphHoleRadius-.002,_GlyphHoleRadius+.002,length(glyphUV-_GlyphHoleCenter.xy));
                }
                #ifdef UNITY_UI_CLIP_RECT
                color.a*=UnityGet2DClipping(i.worldPosition.xy,_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
