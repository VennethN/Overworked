Shader "Custom/CRTEffect"
{
    Properties
    {
        _Curvature ("Screen Curvature", Range(0, 10)) = 3.0
        _ScanlineIntensity ("Scanline Intensity", Range(0, 1)) = 0.25
        _ScanlineSpeed ("Scanline Scroll Speed", Range(0, 5)) = 0.5
        _RGBOffset ("RGB Separation", Range(0, 0.02)) = 0.003
        _VignetteStrength ("Vignette Strength", Range(0, 2)) = 0.8
        _Brightness ("Brightness", Range(0.5, 2.0)) = 1.1
        _Flicker ("Flicker Amount", Range(0, 0.1)) = 0.02
        _StaticIntensity ("Static Wave Intensity", Range(0, 0.2)) = 0.05
        _StaticSpeed ("Static Wave Speed", Range(0, 10)) = 2.0
        _StaticWidth ("Static Wave Width", Range(0.01, 0.3)) = 0.1
        _NoiseIntensity ("Noise Intensity", Range(0, 0.3)) = 0.05
        _StaticDisruption ("Static Disruption", Range(0, 1)) = 0.3
        [HideInInspector] _ClickUV ("Click UV", Vector) = (0.5, 0.5, 0, 0)
        [HideInInspector] _ClickTime ("Click Time", Float) = -10
        _ClickRadius ("Click Glitch Radius", Range(0.02, 0.4)) = 0.15
        _ClickStrength ("Click Glitch Strength", Range(0, 1)) = 0.6
        _ClickDuration ("Click Glitch Duration", Range(0.1, 1.0)) = 0.35
        [HideInInspector] _UIRT ("UI Render Texture", 2D) = "black" {}
        [HideInInspector] _HasUIRT ("Has UI RT", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "CRT"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            TEXTURE2D(_UIRT);
            SAMPLER(sampler_UIRT);

            float _Curvature;
            float _ScanlineIntensity;
            float _ScanlineSpeed;
            float _RGBOffset;
            float _VignetteStrength;
            float _Brightness;
            float _Flicker;
            float _StaticIntensity;
            float _StaticSpeed;
            float _StaticWidth;
            float _NoiseIntensity;
            float _StaticDisruption;
            float2 _ClickUV;
            float _ClickTime;
            float _ClickRadius;
            float _ClickStrength;
            float _ClickDuration;
            float _HasUIRT;

            // Hash function for noise
            float Hash(float n)
            {
                return frac(sin(n) * 43758.5453);
            }

            float2 BarrelDistortion(float2 uv)
            {
                float2 centered = uv * 2.0 - 1.0;
                float r2 = dot(centered, centered);
                centered *= 1.0 + _Curvature * 0.1 * r2;
                return centered * 0.5 + 0.5;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = BarrelDistortion(input.texcoord);

                // Black outside curved screen bounds
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return half4(0, 0, 0, 1);

                // Static wave band — a bright horizontal band that scrolls down the screen
                float wavePos = frac(_Time.y * _StaticSpeed * 0.1);
                float distToWave = abs(uv.y - wavePos);
                // Wrap around (wave loops seamlessly)
                distToWave = min(distToWave, 1.0 - distToWave);
                float waveMask = smoothstep(_StaticWidth, 0.0, distToWave);

                // Offset UVs horizontally inside the wave band (jitter/tear effect)
                float waveJitter = Hash(floor(uv.y * 200.0) + floor(_Time.y * 30.0)) * 2.0 - 1.0;
                uv.x += waveMask * waveJitter * _StaticIntensity * _StaticDisruption;
                uv.x = clamp(uv.x, 0.0, 1.0);

                // Extra RGB spread inside wave band, scaled by disruption
                float waveRGBBoost = waveMask * _StaticDisruption * 0.01;
                float totalRGB = _RGBOffset + waveRGBBoost;

                // Horizontal RGB sub-pixel separation (classic CRT)
                float r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, float2(uv.x + totalRGB, uv.y)).r;
                float g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                float b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, float2(uv.x - totalRGB, uv.y)).b;
                half3 color = half3(r, g, b);

                // Composite UI on top of game (both get CRT distortion via shared UV)
                if (_HasUIRT > 0.5)
                {
                    half4 ui = SAMPLE_TEXTURE2D(_UIRT, sampler_UIRT, uv);
                    color = lerp(color, ui.rgb, ui.a);
                }

                // Brightness washout inside wave band — the main readability killer
                color = lerp(color, half3(1, 1, 1), waveMask * _StaticDisruption * 0.4);

                // Static noise grain inside the wave band, scaled by disruption
                float noise = Hash(uv.x * 100.0 + uv.y * 7777.0 + _Time.y * 50.0) * 2.0 - 1.0;
                color += waveMask * noise * _NoiseIntensity * _StaticDisruption;
                // Subtle global noise grain
                color += noise * _NoiseIntensity * 0.2;

                // Click glitch — localized distortion that fades out
                float clickAge = _Time.y - _ClickTime;
                if (clickAge < _ClickDuration && clickAge >= 0.0)
                {
                    float clickFade = 1.0 - clickAge / _ClickDuration;
                    clickFade *= clickFade; // ease-out curve

                    float dist = distance(uv, _ClickUV);
                    // Expanding ring + inner fill
                    float ring = smoothstep(_ClickRadius, _ClickRadius * 0.3, dist);
                    float glitch = ring * clickFade * _ClickStrength;

                    // Horizontal tear lines inside the glitch area
                    float tearLine = Hash(floor(uv.y * 300.0) + floor(clickAge * 60.0)) * 2.0 - 1.0;
                    color.r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, float2(uv.x + tearLine * glitch * 0.04, uv.y)).r;
                    color.b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, float2(uv.x - tearLine * glitch * 0.04, uv.y)).b;

                    // Noise burst
                    float clickNoise = Hash(uv.x * 333.0 + uv.y * 5555.0 + clickAge * 100.0) * 2.0 - 1.0;
                    color += glitch * clickNoise * 0.15;

                    // Brief brightness flash
                    color += glitch * 0.1 * clickFade;
                }

                // Scanlines
                float screenY = uv.y * _ScreenParams.y;
                float scanline = sin((screenY + _Time.y * _ScanlineSpeed * 100.0) * PI) * 0.5 + 0.5;
                color *= lerp(1.0, scanline, _ScanlineIntensity);

                // Vignette (darker edges)
                float2 vigUV = uv * (1.0 - uv);
                float vig = vigUV.x * vigUV.y * 15.0;
                vig = saturate(pow(vig, _VignetteStrength * 0.25));
                color *= vig;

                // Subtle flicker
                float flicker = 1.0 - _Flicker * sin(_Time.y * 13.7);
                color *= flicker;

                // Brightness
                color *= _Brightness;

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
