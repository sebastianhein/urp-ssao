Shader "Custom/RenderFeature/SSAO"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _RandomTexture("_RandomTexture", 2D) = "grey" {}
    //     _TotalStrength("Total Strength", Float) = 1
    //     _Base("Base", Float) = 0.2
    //     _Area("Area", Float) = 0.0075  
    //     _Falloff("Falloff", Float) = 0.000001
    //     _Radius("Radius", Float) = 0.0002  
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _RandomTexture;

            float4 _MainTex_TexelSize;
            float4 _MainTex_ST;

            float _TotalStrength;
            float _Base;
            float _Area;
            float _Falloff;
            float _Radius;  
            float _Debug;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float3 normalFromDepth(float depth, float2 texCoords) {
                const float2 offset1 = float2(0.0,0.001);
                const float2 offset2 = float2(0.001,0.0);
  
                float depth1 = LinearEyeDepth(tex2D(_CameraDepthTexture, texCoords + offset1)).r;
                float depth2 = LinearEyeDepth(tex2D(_CameraDepthTexture, texCoords + offset2)).r;
  
                float3 p1 = float3(offset1, depth1 - depth);
                float3 p2 = float3(offset2, depth2 - depth);
  
                float3 normal = cross(p1, p2);
                normal.z = -normal.z;
  
                return normalize(normal);
            } 

            fixed4 frag (v2f input) : SV_Target
            {
                float2 res = _MainTex_TexelSize.xy;
// 
                const int samples = 16;
                float3 sample_sphere[samples] = {
                    float3( 0.5381, 0.1856,-0.4319), float3( 0.1379, 0.2486, 0.4430),
                    float3( 0.3371, 0.5679,-0.0057), float3(-0.6999,-0.0451,-0.0019),
                    float3( 0.0689,-0.1598,-0.8547), float3( 0.0560, 0.0069,-0.1843),
                    float3(-0.0146, 0.1402, 0.0762), float3( 0.0100,-0.1924,-0.0344),
                    float3(-0.3577,-0.5301,-0.4358), float3(-0.3169, 0.1063, 0.0158),
                    float3( 0.0103,-0.5869, 0.0046), float3(-0.0897,-0.4940, 0.3287),
                    float3( 0.7119,-0.0154,-0.0918), float3(-0.0533, 0.0596,-0.5411),
                    float3( 0.0352,-0.0631, 0.5460), float3(-0.4776, 0.2847,-0.0271)
                };
  
                float3 random = normalize( tex2D(_RandomTexture, input.uv * 4.123).rgb );
                
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, input.uv)).r;

                float3 position = float3(input.uv, depth);
                float3 normal = normalFromDepth(depth, input.uv);
                
                float radius = _Radius;

                float radius_depth = radius / depth;
                float occlusion = 0.0;
                for(int i=0; i < samples; i++) {
                
                    float3 ray = radius_depth * reflect(sample_sphere[i], random);
                    float3 hemi_ray = position + sign(dot(ray, normal)) * ray;
                    
                    float occ_depth = LinearEyeDepth(tex2D(_CameraDepthTexture, saturate(hemi_ray.xy))).r;
                    float difference = depth - occ_depth;
                    
                    occlusion += step(_Falloff, difference) * (1.0-smoothstep(_Falloff, _Area, difference));
                }
                
                float ao = 1.0 - _TotalStrength * occlusion * (1.0 / samples);
                fixed4 color;
                color.rgb =  saturate(ao + _Base);

                if (_Debug > 0) {
                    color.rgb *= tex2D( _MainTex, input.uv ).rgb;
                }

                return color;
            }
            ENDCG
        }
    }
}
