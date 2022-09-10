Shader "Hidden/AnisotropicKuwahara" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader {

        CGINCLUDE

        #include "UnityCG.cginc"

        struct VertexData {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
        };

        struct v2f {
            float2 uv : TEXCOORD0;
            float4 vertex : SV_POSITION;
        };

        v2f vp(VertexData v) {
            v2f o;
            o.vertex = UnityObjectToClipPos(v.vertex);
            o.uv = v.uv;
            return o;
        }

        #define PI 3.14159265358979323846f
        
        sampler2D _MainTex, _K0, _TFM;
        float4 _MainTex_TexelSize;
        int _KernelSize, _N, _Size;
        float _Hardness, _Q, _Alpha;

        float gaussian(float sigma, float2 pos) {
            return (1.0f / (2.0f * PI * sigma * sigma)) * exp(-((pos.x * pos.x + pos.y * pos.y) / (2.0f * sigma * sigma)));
        }

        float gaussian(float sigma, float pos) {
            return (1.0f / sqrt(2.0f * PI * sigma * sigma)) * exp(-(pos * pos) / (2.0f * sigma * sigma));
        }

        ENDCG

        // Pre Compute Weights
        // Calculate Section
        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float2 pos = i.uv - 0.5f;
                float phi = atan2(pos.y, pos.x);
                
                float Xk[4];
                for (int k = 0; k <= 3; ++k) {
                    Xk[k] = (((2 * k - 1) * PI) / _N) < phi && phi <= (((2 * k + 1) * PI) / _N);
                }

                return dot(pos, pos) <= 0.25f ? float4(Xk[0], Xk[1], Xk[2], Xk[3]) : 0;
            }
            ENDCG
        }

        // Gaussian Filter Section
        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                // Calculated from the resolution of the gaussian weight texture, anything beyond 32x32 seems to make no difference so it is hard coded
                float sigmaR = 0.5f * ((32.0f) * 0.5f);
                float sigmaS = 0.33f * sigmaR;

                float4 col = 0;
                float kernelSum = 0.0f;
                for (int x = -floor(sigmaS); x <= floor(sigmaS); ++x) {
                    for (int y = -floor(sigmaS); y <= floor(sigmaS); ++y) {
                        float4 c = tex2D(_MainTex, i.uv + float2(x, y) * _MainTex_TexelSize.xy);
                        float gauss = gaussian(sigmaS, float2(x, y));

                        col += c * gauss;
                        kernelSum += gauss;
                    }
                }

                float4 output = (col / kernelSum) * gaussian(sigmaR, (i.uv - 0.5f) * sigmaR * 5);

                return output;
            }
            ENDCG
        }

        // Calculate Eigenvectors
        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float2 d = _MainTex_TexelSize.xy;

                float3 Sx = (
                    1.0f * tex2D(_MainTex, i.uv + float2(-d.x, -d.y)).rgb +
                    2.0f * tex2D(_MainTex, i.uv + float2(-d.x,  0.0)).rgb +
                    1.0f * tex2D(_MainTex, i.uv + float2(-d.x,  d.y)).rgb +
                    -1.0f * tex2D(_MainTex, i.uv + float2(d.x, -d.y)).rgb +
                    -2.0f * tex2D(_MainTex, i.uv + float2(d.x,  0.0)).rgb +
                    -1.0f * tex2D(_MainTex, i.uv + float2(d.x,  d.y)).rgb
                ) / 4.0f;

                float3 Sy = (
                    1.0f * tex2D(_MainTex, i.uv + float2(-d.x, -d.y)).rgb +
                    2.0f * tex2D(_MainTex, i.uv + float2( 0.0, -d.y)).rgb +
                    1.0f * tex2D(_MainTex, i.uv + float2( d.x, -d.y)).rgb +
                    -1.0f * tex2D(_MainTex, i.uv + float2(-d.x, d.y)).rgb +
                    -2.0f * tex2D(_MainTex, i.uv + float2( 0.0, d.y)).rgb +
                    -1.0f * tex2D(_MainTex, i.uv + float2( d.x, d.y)).rgb
                ) / 4.0f;

                
                return float4(dot(Sx, Sx), dot(Sy, Sy), dot(Sx, Sy), 1.0f);
            }
            ENDCG
        }

        // Blur Pass 1
        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                int kernelRadius = 1;

                float4 col = 0;
                float kernelSum = 0.0f;
                for (int x = -kernelRadius; x <= kernelRadius; ++x) {
                    float4 c = tex2D(_MainTex, i.uv + float2(x, 0) * _MainTex_TexelSize.xy);
                    float gauss = gaussian(2.0f, x);

                    col += c * gauss;
                    kernelSum += gauss;
                }

                return float4(col.rgb / kernelSum, 1.0f);
            }
            ENDCG
        }

        // Blur Eigenvectors and calculate direction and anisotropy
        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                int kernelRadius = 1;

                float4 col = 0;
                float kernelSum = 0.0f;

                for (int y = -kernelRadius; y <= kernelRadius; ++y) {
                    float4 c = tex2D(_MainTex, i.uv + float2(0, y) * _MainTex_TexelSize.xy);
                    float gauss = gaussian(2.0f, y);

                    col += c * gauss;
                    kernelSum += gauss;
                }

                float3 g = col.rgb / kernelSum;

                float lambda1 = 0.5f * (g.y + g.x + sqrt(g.y * g.y - 2.0f * g.x * g.y + g.x * g.x + 4.0f * g.z * g.z));
                float lambda2 = 0.5f * (g.y + g.x - sqrt(g.y * g.y - 2.0f * g.x * g.y + g.x * g.x + 4.0f * g.z * g.z));

                float2 v = float2(lambda1 - g.x, -g.z);
                float2 t = length(v) > 0.0 ? normalize(v) : float2(0.0f, 1.0f);
                float phi = -atan2(t.y, t.x);

                float A = (lambda1 + lambda2 > 0.0f) ? (lambda1 - lambda2) / (lambda1 + lambda2) : 0.0f;
                
                return float4(t, phi, A);
            }
            ENDCG
        }

        // Apply Kuwahara Filter
        Pass {
            CGPROGRAM
            #pragma vertex vp
            #pragma fragment fp

            float4 fp(v2f i) : SV_Target {
                float alpha = _Alpha;
                float4 t = tex2D(_TFM, i.uv);
                float a = float(_KernelSize) * clamp((alpha + t.w) / alpha, 0.1f, 2.0f);
                float b = float(_KernelSize) * clamp(alpha / (alpha + t.w), 0.1f, 2.0f);
                
                float cos_phi = cos(t.z);
                float sin_phi = sin(t.z);

                float2x2 R = {cos_phi, -sin_phi,
                              sin_phi, cos_phi};

                float2x2 S = {0.5f / a, 0.0f,
                              0.0f, 0.5f / b};

                float2x2 SR = mul(S, R);

                int max_x = int(sqrt(a * a * cos_phi * cos_phi + b * b * sin_phi * sin_phi));
                int max_y = int(sqrt(a * a * sin_phi * sin_phi + b * b * cos_phi * cos_phi));

                int k;
                float4 m[8];
                float3 s[8];

                float3 c = tex2D(_MainTex, i.uv).rgb;
                float w = tex2D(_K0, float2(0.5f, 0.5f)).x;
                for (k = 0; k < _N; ++k) {
                    m[k] = float4(c * w, w);
                    s[k] = c * c * w;
                }

                [loop]
                for (int y = 0; y <= max_y; ++y) {
                    [loop]
                    for (int x = -max_x; x <= max_x; ++x) {
                        if ((y != 0) || (x > 0)) {
                            float2 v = mul(SR, float2(x, y));
                            
                            float3 c0 = tex2D(_MainTex, i.uv + float2(x, y) * _MainTex_TexelSize.xy).rgb;
                            float3 c1 = tex2D(_MainTex, i.uv - float2(x, y) * _MainTex_TexelSize.xy).rgb;

                            float3 cc0 = c0 * c0;
                            float3 cc1 = c1 * c1;

                            float4 w0123 = tex2D(_K0, float2(0.5f, 0.5f) + v);
                            for (k = 0; k < 4; ++k) {
                                m[k] += float4(c0 * w0123[k], w0123[k]);
                                s[k] += cc0 * w0123[k];

                                m[k + 4] += float4(c1 * w0123[k], w0123[k]);
                                s[k + 4] += cc1 * w0123[k];
                            }

                            float4 w4567 = tex2D(_K0, float2(0.5f, 0.5f) - v);
                            for (k = 0; k < 4; ++k) {
                                m[k + 4] += float4(c0 * w4567[k], w4567[k]);
                                s[k + 4] += cc0 * w4567[k];

                                m[k] += float4(c1 * w4567[k], w4567[k]);
                                s[k] += cc1 * w4567[k];
                            }
                        }
                    }
                }

                float4 output = 0;
                for (k = 0; k < _N; ++k) {
                    m[k].rgb /= m[k].w;
                    s[k] = abs(s[k] / m[k].w - m[k].rgb * m[k].rgb);

                    float sigma2 = s[k].r + s[k].g + s[k].b;
                    float w = 1.0f / (1.0f + pow(_Hardness * 1000.0f * sigma2, 0.5f * _Q));

                    output += float4(m[k].rgb * w, w);
                }
                
                return output / output.w;
            }
            ENDCG
        }
    }
}