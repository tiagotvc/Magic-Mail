#ifndef ROYALAVES_FROZEN_ICE
#define ROYALAVES_FROZEN_ICE
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
half3 IcePaintColor(half3 value)
{
    #if !defined(UNITY_COLORSPACE_GAMMA)
    return SRGBToLinear(value);
    #else
    return value;
    #endif
}
// Painted sky reflections on a solid frozen surface. Coordinates belong to the
// ground, so reflections stay attached when scrolling rather than looking like water waves.
half3 FrozenLake(float2 p,float shore)
{
    float depth=saturate(shore*2.3);
    float sky=.5+.5*sin(p.y*.48+p.x*.26);
    half3 ice=lerp(IcePaintColor(half3(.08,.34,.65)),IcePaintColor(half3(.42,.85,.97)),sky*.60+depth*.30);
    float reflectedSky=frac(p.x*.38+p.y*.21+_IceClock*.035);
    float broad=smoothstep(.10,.14,reflectedSky)*(1-smoothstep(.29,.33,reflectedSky));
    float thin=smoothstep(.37,.385,reflectedSky)*(1-smoothstep(.415,.435,reflectedSky));
    ice=lerp(ice,IcePaintColor(half3(.91,.99,1)),broad*.70+thin*.86);
    float polish=exp(-pow((reflectedSky-.20)/.10,2));
    ice=lerp(ice,IcePaintColor(half3(1,1,1)),polish*.62);
    float gloss=exp(-pow((frac(p.x*.18-p.y*.09-_IceClock*.018)-.30)/.09,2)-pow((frac(p.y*.16)-.58)/.26,2));
    ice=lerp(ice,IcePaintColor(half3(.96,1,1)),gloss*.70);
    // Subtle angular seams below the glossy face, kept sparse and finer than the reflection.
    float2 q=p*float2(.75,.52);float2 cell=floor(q);float2 f=frac(q);
    float seed=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
    float fracture=abs(f.y-(f.x<.48?.30+f.x*.42:.69-f.x*.39));
    float crack=(1-smoothstep(.004,.012+fwidth(fracture),fracture))*step(.62,seed);
    ice=lerp(ice,IcePaintColor(half3(.82,.97,1)),crack*.42);
    float rim=1-smoothstep(.015,.065,shore);
    ice=lerp(ice,IcePaintColor(half3(.91,.99,1)),rim*.84);
    return ice;
}
// Fixed crystal points twinkle independently; the surface mask in each caller
// keeps these four-point highlights on exposed ice, away from grass and snow.
half3 FrozenSparkles(float2 p)
{
    float2 grid=p*float2(2.3,1.6);
    float2 cell=floor(grid);
    float seed=frac(sin(dot(cell,float2(127.1,311.7)))*43758.5453);
    float seed2=frac(sin(dot(cell,float2(269.5,183.3)))*22578.1459);
    float2 center=.22+.56*float2(seed,seed2);
    float2 d=abs(frac(grid)-center);
    float phase=frac(_IceClock/(3.8+seed*3.2)+seed2);
    float pulse=pow(saturate(1-abs(phase-.5)/.14),2);
    float aa=max(.001,max(fwidth(grid.x),fwidth(grid.y))*.65);
    float radius=.085*(.65+.35*pulse);
    float horizontal=(1-smoothstep(.004,.004+aa,d.y))*pow(saturate(1-d.x/radius),2);
    float vertical=(1-smoothstep(.004,.004+aa,d.x))*pow(saturate(1-d.y/(radius*1.35)),2);
    float core=exp(-dot(d,d)/(.012*.012));
    float halo=exp(-dot(d,d)/(.055*.055))*.12;
    float sparkle=(max(horizontal,vertical)+core*.65+halo)*pulse*step(.24,seed);
    return IcePaintColor(half3(.86,.97,1))*sparkle*(1.2+_IceNight*.9);
}
// Luminous reflections are added after the night grade, so ice catches nearby
// lamps/bulbs instead of dimming their reflection together with the ground.
half3 FrozenNightLight(float2 p)
{
    float moonAxis=p.x+.55+.13*sin(p.y*.6);
    float moonColumn=exp(-pow(moonAxis/1.15,2));
    float moonHalo=exp(-pow(moonAxis/2.8,2));
    float broken=.45+.55*pow(.5+.5*sin(p.y*10+p.x*2+_IceClock*.7),3);
    half3 reflected=IcePaintColor(half3(.51,.79,1))*(moonHalo*.18+moonColumn*broken*.85);
    [loop]for(int k=0;k<12;k++)
    {
        if(k>=_IceLightCount)break;
        float2 d=p-_IceLights[k].xy;
        float falloff=exp(-dot(d/float2(.55,1.25),d/float2(.55,1.25))/max(.12,_IceLights[k].z));
        float streak=.28+.72*pow(.5+.5*cos(d.y*16+d.x*4-_IceClock*.85),8);
        reflected+=_IceLightColors[k].rgb*falloff*streak*_IceLights[k].w;
    }
    float sweep=exp(-pow((frac(p.x*.24+p.y*.12-_IceClock*.06)-.5)/.020,2));
    return reflected*_IceNight+IcePaintColor(half3(.88,.97,1))*sweep*(.20+_IceNight*.85)+FrozenSparkles(p);
}
half3 FrozenMoonAura(float2 p)
{
    float pulse=.80+.20*sin(_IceClock*.7+p.y*.4);
    return IcePaintColor(half3(.30,.66,1))*(_IceNight*.16*pulse);
}
#endif
