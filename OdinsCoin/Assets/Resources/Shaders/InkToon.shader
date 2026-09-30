// The storybook look for Odin's Coin: the drawn texture times the colour, soft wrapped light with cool shadows,
// and pencil hatching over the shadowed side (single strokes, then cross-hatching). It has two subshaders,
// one for URP (on URP's own shader library, so it needs the URP package) and one for the built-in pipeline. The maths mirrors
// Scripts/Core/InkStyle.cs, which the preview renderer uses; change all three together.
// Both receive the sun's shadows, which darken the tone so shadows get hatched like the rest.
Shader "OdinsCoin/InkToon"
{
    Properties
    {
        [MainColor] _Color ("Colour", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex ("Drawn texture", 2D) = "white" {}
        _Hatch ("Hatching", Range(0, 1)) = 1
        _Flat ("Flat light", Range(0, 1)) = 0
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    fixed4 _Color;
    sampler2D _MainTex;
    float4 _MainTex_ST;
    float _Hatch;
    float _Flat;
    // Set by InkStyle.SetSun; sensible defaults when they're not set yet (all zero).
    float4 _InkSunDir;
    float _InkHatchSpacing;
    float _InkStrokeDark;
    // Paper grain over the whole picture (DrawnTextures' paper), fixed to the screen like the page itself.
    sampler2D _InkPaper;
    float _InkPaperAmount;

    float InkLine(float t, float halfWidth, float spacing)
    {
        float f = frac(t);
        float dist = abs(f - 0.5) * spacing;
        return saturate(halfWidth * spacing - dist + 0.5);
    }

    // The whole look for one pixel: normal, uv, screen position and how lit it is by the sun (1 = not in shadow).
    float3 InkShade(float3 nrm, float2 uv, float4 scr, float lit)
    {
        float3 n = normalize(nrm);
        float3 sun = dot(_InkSunDir.xyz, _InkSunDir.xyz) > 0.001 ? normalize(_InkSunDir.xyz) : normalize(float3(-0.5, 0.75, 0.45));
        float spacing = _InkHatchSpacing > 0.5 ? _InkHatchSpacing : 5.0;
        float strokeDark = _InkStrokeDark > 0.001 ? _InkStrokeDark : 0.3;

        float tone = saturate((dot(n, sun) + 0.35) / 1.35);
        // A cast shadow darkens the tone to that of a surface turned away from the sun (so it gets hatched too).
        tone = lerp(min(tone, 0.25), tone, lit);
        float shade = lerp(0.55 + 0.55 * tone, 0.94 + 0.1 * tone, _Flat);
        float3 col = _Color.rgb * tex2D(_MainTex, uv).rgb * shade;
        col *= float3(0.96 + 0.06 * tone, 1.0, 1.04 - 0.06 * tone);

        float2 sp = scr.xy / max(scr.w, 1e-5) * _ScreenParams.xy;
        float wobble = 0.8 * sin(sp.y * 0.21) + 0.5 * sin(sp.x * 0.13 + 1.7);
        float k = 1.0;
        float c1 = saturate((0.55 - tone) / 0.4);
        if (c1 > 0.0) k *= 1.0 - strokeDark * InkLine((sp.x + sp.y + wobble) / spacing, c1 * 0.32, spacing);
        float c2 = saturate((0.3 - tone) / 0.3);
        if (c2 > 0.0) k *= 1.0 - strokeDark * InkLine((sp.x - sp.y - wobble) / spacing + 0.5, c2 * 0.28, spacing);
        col *= lerp(1.0, k, _Hatch);
        col *= lerp(1.0, tex2D(_InkPaper, sp / 256.0).r, _InkPaperAmount);
        return col;
    }

    struct appdata_ink
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
        float2 uv : TEXCOORD0;
    };

    // Shadow casting, shared by both pipelines' subshaders.
    struct v2f_caster { V2F_SHADOW_CASTER; };

    v2f_caster vertCaster(appdata_base v)
    {
        v2f_caster o;
        TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
        return o;
    }

    float4 fragCaster(v2f_caster i) : SV_Target
    {
        SHADOW_CASTER_FRAGMENT(i)
    }
    ENDCG

    // URP: written against URP's own shader library, so it receives the main light's shadows too.
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "InkToon"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MainTex_ST;
                float _Hatch;
                float _Flat;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_InkPaper); SAMPLER(sampler_InkPaper);
            float4 _InkSunDir;
            float _InkHatchSpacing;
            float _InkStrokeDark;
            float _InkPaperAmount;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float4 scr : TEXCOORD3;
                float fog : TEXCOORD4;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.scr = ComputeScreenPos(o.positionCS);
                o.fog = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            float InkLineU(float t, float halfWidth, float spacing)
            {
                float f = frac(t);
                float dist = abs(f - 0.5) * spacing;
                return saturate(halfWidth * spacing - dist + 0.5);
            }

            half4 frag(Varyings i) : SV_Target
            {
                // The same maths as InkShade (and InkStyle.cs), with the sun's shadow from URP.
                float lit = GetMainLight(TransformWorldToShadowCoord(i.positionWS)).shadowAttenuation;
                float3 n = normalize(i.normalWS);
                float3 sun = dot(_InkSunDir.xyz, _InkSunDir.xyz) > 0.001 ? normalize(_InkSunDir.xyz) : normalize(float3(-0.5, 0.75, 0.45));
                float spacing = _InkHatchSpacing > 0.5 ? _InkHatchSpacing : 5.0;
                float strokeDark = _InkStrokeDark > 0.001 ? _InkStrokeDark : 0.3;
                float tone = saturate((dot(n, sun) + 0.35) / 1.35);
                tone = lerp(min(tone, 0.25), tone, lit);
                float shade = lerp(0.55 + 0.55 * tone, 0.94 + 0.1 * tone, _Flat);
                float3 col = _Color.rgb * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv).rgb * shade;
                col *= float3(0.96 + 0.06 * tone, 1.0, 1.04 - 0.06 * tone);
                float2 sp = i.scr.xy / max(i.scr.w, 1e-5) * _ScreenParams.xy;
                float wobble = 0.8 * sin(sp.y * 0.21) + 0.5 * sin(sp.x * 0.13 + 1.7);
                float k = 1.0;
                float c1 = saturate((0.55 - tone) / 0.4);
                if (c1 > 0.0) k *= 1.0 - strokeDark * InkLineU((sp.x + sp.y + wobble) / spacing, c1 * 0.32, spacing);
                float c2 = saturate((0.3 - tone) / 0.3);
                if (c2 > 0.0) k *= 1.0 - strokeDark * InkLineU((sp.x - sp.y - wobble) / spacing + 0.5, c2 * 0.28, spacing);
                col *= lerp(1.0, k, _Hatch);
                col *= lerp(1.0, SAMPLE_TEXTURE2D(_InkPaper, sampler_InkPaper, sp / 256.0).r, _InkPaperAmount);
                col = MixFog(col, i.fog);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex vertCaster
            #pragma fragment fragCaster
            #pragma multi_compile_shadowcaster
            ENDCG
        }

        // Depth for URP's depth prepass (screen-space shadows and the like).
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask 0
            HLSLPROGRAM
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            float4 vertDepth(float4 positionOS : POSITION) : SV_POSITION { return TransformObjectToHClip(positionOS.xyz); }
            half4 fragDepth() : SV_Target { return 0; }
            ENDHLSL
        }
    }

    // Built-in pipeline: the forward base pass, which also receives the sun's shadows.
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "InkToon"
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #include "AutoLight.cginc"

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 nrm : TEXCOORD1;
                float4 scr : TEXCOORD2;
                UNITY_FOG_COORDS(3)
                SHADOW_COORDS(4)
            };

            v2f vert(appdata_ink v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.nrm = UnityObjectToWorldNormal(v.normal);
                o.scr = ComputeScreenPos(o.pos);
                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_SHADOW(o);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = fixed4(InkShade(i.nrm, i.uv, i.scr, SHADOW_ATTENUATION(i)), 1.0);
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            CGPROGRAM
            #pragma vertex vertCaster
            #pragma fragment fragCaster
            #pragma multi_compile_shadowcaster
            ENDCG
        }
    }
    Fallback "Diffuse"
}
