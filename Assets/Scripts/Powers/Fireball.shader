Shader "Custom/Fireball"
{
	Properties
	{
		[HDR] _CoreColor ("Hot Core", Color) = (1, 0.85, 0.2, 1)
		[HDR] _FlameColor ("Flame Color", Color) = (1, 0.08, 0.005, 1)
		[HDR] _RimColor ("Rim Glow", Color) = (1, 0.3, 0.02, 1)
		_NoiseScale ("Flame Detail", Range(1, 20)) = 6
		_ScrollSpeed ("Flame Speed", Range(0, 10)) = 2
		_RimPower ("Rim Sharpness", Range(0.1, 8)) = 2.5
		_Emission ("Emission", Range(0, 10)) = 3
		_Alpha ("Opacity", Range(0, 1)) = 0.95
	}

	SubShader
	{
		Tags
		{
			"RenderPipeline" = "UniversalPipeline"
			"Queue" = "Transparent"
			"RenderType" = "Transparent"
		}

		Blend SrcAlpha One
		ZWrite Off
		Cull Back

		Pass
		{
			Name "Fireball"

			HLSLPROGRAM
			#pragma target 3.0
			#pragma vertex Vert
			#pragma fragment Frag
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct Varyings
			{
				float4 positionHCS : SV_POSITION;
				float3 positionWS : TEXCOORD0;
				float3 normalWS : TEXCOORD1;
				float2 uv : TEXCOORD2;
			};

			CBUFFER_START(UnityPerMaterial)
				half4 _CoreColor;
				half4 _FlameColor;
				half4 _RimColor;
				half _NoiseScale;
				half _ScrollSpeed;
				half _RimPower;
				half _Emission;
				half _Alpha;
			CBUFFER_END

			// Συνάρτηση παραγωγής τυχαίων τιμών για αποφυγή παράλληλων γραμμών
			float hash(float2 p)
			{
				p = frac(p * float2(123.34f, 456.21f));
				p += dot(p, p + 45.32f);
				return frac(p.x * p.y);
			}

			// Smooth Value Noise 2D
			float ValueNoise(float2 st)
			{
				float2 i = floor(st);
				float2 f = frac(st);

				// Smoothstep παρεμβολή για ομαλή μετάβαση
				float2 u = f * f * (3.0f - 2.0f * f);

				return lerp(lerp(hash(i + float2(0.0f, 0.0f)), hash(i + float2(1.0f, 0.0f)), u.x),
							lerp(hash(i + float2(0.0f, 1.0f)), hash(i + float2(1.0f, 1.0f)), u.x), u.y);
			}

			Varyings Vert(Attributes input)
			{
				Varyings output;
				VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

				output.positionHCS = positionInputs.positionCS;
				output.positionWS = positionInputs.positionWS;
				output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
				output.uv = input.uv;
				return output;
			}

			half4 Frag(Varyings input) : SV_Target
			{
				half3 normalWS = normalize(input.normalWS);
				half3 viewDirectionWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
				half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirectionWS)), _RimPower);

				// Χρήση Noise αντί για ημίτονα (sin) για να μην εμφανίζονται παράλληλες λωρίδες
				float2 st = input.uv * _NoiseScale;
				float time = _Time.y * _ScrollSpeed;

				// Διπλό στρώμα noise (fbm) για φυσικό στροβιλισμό
				float n1 = ValueNoise(st + float2(time * 0.5f, time * 0.8f));
				float n2 = ValueNoise(st * 2.0f - float2(time * 0.9f, time * 0.4f));
				half flamePattern = saturate((n1 + n2 * 0.5f) / 1.5f);

				half core = saturate(1.0h - fresnel + flamePattern * 0.35h);
				half3 flameColor = lerp(_FlameColor.rgb, _CoreColor.rgb, core);
				half3 finalColor = flameColor * _Emission + _RimColor.rgb * fresnel * _Emission;
				half alpha = saturate(_Alpha * (0.65h + flamePattern * 0.35h + fresnel * 0.5h));

				return half4(finalColor, alpha);
			}
			ENDHLSL
		}
	}
}