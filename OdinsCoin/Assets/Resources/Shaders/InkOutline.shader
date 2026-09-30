// Ink lines around the world's meshes: the shell (a copy of the mesh with smoothed normals, made by InkOutline.cs)
// is pushed out along its normals by a fixed number of screen pixels, thinning with distance, and only its back
// faces are drawn, so a dark line appears around the silhouette and along creases. UnityCG only, so it runs in
// URP and the built-in pipeline.
Shader "OdinsCoin/InkOutline"
{
    Properties
    {
        [MainColor] _Color ("Ink", Color) = (0.08, 0.065, 0.055, 1)
        _Width ("Width (pixels)", Float) = 2.2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "InkOutline"
            Cull Front
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Width;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
            };

            v2f vert(appdata v)
            {
                v2f o;
                float4 clip = UnityObjectToClipPos(v.vertex);
                float3 viewNormal = mul((float3x3)UNITY_MATRIX_V, UnityObjectToWorldNormal(v.normal));
                float2 dir = mul((float2x2)UNITY_MATRIX_P, viewNormal.xy);
                float len = length(dir);
                dir = len > 1e-5 ? dir / len : float2(0, 0);
                // Full width up close, thinning out to a third far away so distant islands don't go black.
                float pixels = _Width * clamp(30.0 / max(clip.w, 0.01), 0.35, 1.0);
                clip.xy += dir * pixels * 2.0 / _ScreenParams.xy * clip.w;
                o.pos = clip;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 c = _Color;
                UNITY_APPLY_FOG(i.fogCoord, c);
                return c;
            }
            ENDCG
        }
    }
}
