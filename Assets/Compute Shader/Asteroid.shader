Shader "Unlit/AsteroidURP"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _Size("Size", Float) = 0.2
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            StructuredBuffer<float3> asteroids;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float _Size;

            // 4 vértices por quad
            static const float2 verts[4] = {
                float2(-1, -1),
                float2( 1, -1),
                float2( 1,  1),
                float2(-1,  1)
            };

            static const float2 uvs[4] = {
                float2(0,0),
                float2(1,0),
                float2(1,1),
                float2(0,1)
            };

            struct Attributes {
                uint vertexID : SV_VertexID;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings o;

                float2 offset = verts[input.vertexID] * _Size;

                float3 asteroid = asteroids[input.instanceID];

                float3 worldPos = float3(asteroid.xy + offset, 0);

                o.pos = TransformWorldToHClip(worldPos);
                o.uv = uvs[input.vertexID];
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
            }
            ENDHLSL
        }
    }
}
