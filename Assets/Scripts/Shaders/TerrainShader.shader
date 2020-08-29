Shader "Custom/TerrainShader"
{
    Properties
    {
        [NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
    }
        SubShader
    {
        Pass
        {
            Tags {"Queue" = "Geometry"  "RenderType" = "Opaque"  "LightMode" = "ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "Lighting.cginc"

        // compile shader into multiple variants, with and without shadows
        // (we don't care about any lightmaps yet, so skip these variants)
        #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
        // shadow helper functions and macros
        #include "AutoLight.cginc"

        struct v2f
        {
            float2 uv : TEXCOORD0;
            float3 vertex : TEXCOORD1;
            SHADOW_COORDS(2) // put shadows data into TEXCOORD2
            fixed3 diff : COLOR0;
            fixed3 ambient : COLOR1;
            float4 pos : SV_POSITION;
            float light : TEXCOORD3;
        };
        v2f vert(appdata_base v)
        {
            v2f o;
            o.pos = UnityObjectToClipPos(v.vertex);
            o.uv = v.texcoord;
            o.vertex = v.vertex;;
            half3 worldNormal = UnityObjectToWorldNormal(v.normal);
            half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
            o.diff = nl * _LightColor0.rgb;
            o.ambient = ShadeSH9(half4(worldNormal,1));
            // compute shadows data
            TRANSFER_SHADOW(o)
            return o;
        }

        sampler2D _MainTex;

        [maxvertexcount(3)]
        void geom(triangle v2f IN[3], inout TriangleStream<v2f> triStream)
        {


            // Compute the normal
            float3 vecA = IN[1].vertex - IN[0].vertex;
            float3 vecB = IN[2].vertex - IN[0].vertex;
            float3 normal = cross(vecA, vecB);
            normal = normalize(mul(normal, (float3x3) unity_WorldToObject));

            // Compute diffuse light
            float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

            for (int i = 0; i < 3; i++)
            {
                IN[i].light = dot(normal, lightDir);//v2f.light contain the flat shading illumination effect
                triStream.Append(IN[i]);
            }
        }

        fixed4 frag(v2f i) : SV_Target
        {


            fixed4 col = tex2D(_MainTex, i.uv);
            // compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
            fixed shadow = SHADOW_ATTENUATION(i);
            // darken light's illumination with shadow, keep ambient intact
            fixed3 lighting = i.diff * shadow *i.light + i.ambient * i.light ;
            col.rgb *= lighting;
            return col;
        }
    ENDCG
    }

    // shadow casting support
    UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
