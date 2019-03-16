Shader "Custom/Water" {
	Properties{		
		_Color("Color", Color) = (0, 0, 1, 0.8)
		[NoScaleOffset] _StateTex("State", 2D) = "black" {}
		_NormalStrength("NormalStrength", Range(0.1, 100)) = 0.5		
		_Metallic("Metallic", Range(0, 1)) = 0.5
		_Smoothness("Smoothness", Range(0, 1)) = 1
		_DepthDecay("DepthDecay", float) = 1
	}
		
	SubShader
	{
		Tags { "Queue"="Transparent"  "RenderType"="Transparent"  "IgnoreProjector"="True" }

		Pass
		{
			Tags {"LightMode" = "ForwardBase" "IgnoreProjector" = "True" }
			//LOD 300
			Blend SrcAlpha OneMinusSrcAlpha
			//Zwrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps yet, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight multi_compile_fog			
			#include "AutoLight.cginc"

			struct v2f
			{				
				float2 uvState : TEXCOORD0;
				float4 pos : SV_POSITION;
				float3 worldPos : TEXCOORD1;
				float4 screenPos : TEXCOORD2;
				SHADOW_COORDS(3)
				UNITY_FOG_COORDS(4)
			};
			
			sampler2D _StateTex;
			float2 _StateTex_TexelSize;
			float _NormalStrength;
			float _Metallic;
			float _Smoothness;
			fixed4 _Color;

			sampler2D _CameraDepthTexture;
			float4 _CameraDepthTexture_TexelSize;
			float _DepthDecay;

			#define WATER_HEIGHT(s) (s.g)
			#define TERRAIN_HEIGHT(s) (s.r)
			#define FULL_HEIGHT(s) (TERRAIN_HEIGHT(s) + WATER_HEIGHT(s))

			v2f vert(appdata_base v)
			{
				float4 state = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y, 0, 0));
				v.vertex.y += FULL_HEIGHT(state);

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);				
				o.uvState = v.texcoord;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.screenPos = ComputeScreenPos(o.pos);

				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{				
				float4 state = tex2D(_StateTex, i.uvState);
				clip(WATER_HEIGHT(state) - 0.08);

				float2 du = float2(_StateTex_TexelSize.x * 0.5, 0);
				float2 dv = float2(0, _StateTex_TexelSize.y * 0.5);

				float4 state_l = tex2D(_StateTex, i.uvState + du);
				float4 state_r = tex2D(_StateTex, i.uvState - du);
				float4 state_t = tex2D(_StateTex, i.uvState + dv);
				float4 state_b = tex2D(_StateTex, i.uvState - dv);

				half dhdu = _NormalStrength * 0.5 * (FULL_HEIGHT(state_r) - FULL_HEIGHT(state_l));
				half dhdv = _NormalStrength * 0.5 * (FULL_HEIGHT(state_b) - FULL_HEIGHT(state_t));

				float3 normal = float3(dhdu, 1, dhdv);
				float3 worldNormal = UnityObjectToWorldNormal(normalize(normal));


				float3 lightDir = _WorldSpaceLightPos0.xyz;
				float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);

				float3 lightColor = _LightColor0.rgb;
				float3 albedo = _Color.rgb;

				float3 specularTint;
				float oneMinusReflectivity;
				albedo = DiffuseAndSpecularFromMetallic(
					albedo, _Metallic, specularTint, oneMinusReflectivity
				);

				UnityLight light;
				light.color = lightColor;
				light.dir = lightDir;
				light.ndotl = DotClamped(normal, lightDir);
				UnityIndirect indirectLight;
				indirectLight.diffuse = 0;
				indirectLight.specular = 0;
				float3 reflectionDir = reflect(-viewDir, normal);
				Unity_GlossyEnvironmentData envData;
				envData.roughness = 1 - _Smoothness;
				envData.reflUVW = reflectionDir;
				indirectLight.specular = Unity_GlossyEnvironment(
					UNITY_PASS_TEXCUBE(unity_SpecCube0), unity_SpecCube0_HDR, envData
				);

				half4 c = UNITY_BRDF_PBS(
					albedo, specularTint,
					oneMinusReflectivity, _Smoothness,
					normal, viewDir,
					light, indirectLight
				);

				float2 uv = i.screenPos.xy / i.screenPos.w;
				#if UNITY_UV_STARTS_AT_TOP
				if (_CameraDepthTexture_TexelSize.y < 0) {
					uv.y = 1 - uv.y;
				}
				#endif

				float backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
				float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(i.screenPos.z);
				float depthDifference = (backgroundDepth - surfaceDepth);
				c.a = saturate(clamp(depthDifference * _DepthDecay * 4, 0, 0.8) + 0.4 * saturate(depthDifference * _DepthDecay * 0.5));				
				//c.rgb = depthDifference;
				return c;
			}
			ENDCG
		}
	}



		/*
		SubShader{
			Tags { "RenderType" = "Transparent" "Queue"="Transparent" }			
			LOD 200
			
			ZWrite On

			CGPROGRAM
			#include "UnityCG.cginc"

			#pragma surface surf Standard vertex:vert alpha:blend
			#pragma target 3.0
			struct Input {
				float2 uv_MainTex;				
				float3 worldRefl;				
				float4 screenPos;
				INTERNAL_DATA
			};

			float _NormalStrength;
			float _SampleSize;
			fixed4 _Color;
			sampler2D _MainTex;
			sampler2D _StateTex;
			float2 _StateTex_TexelSize;
			float4 _InputControls;
			half _Metallic;
			half _Smoothness;
			sampler2D _CameraDepthTexture;
			float4 _CameraDepthTexture_TexelSize;
			float _DepthDecay;

			#define WATER_HEIGHT(s) (s.g)
			#define TERRAIN_HEIGHT(s) (s.r)
			#define FULL_HEIGHT(s) (TERRAIN_HEIGHT(s) + WATER_HEIGHT(s))


			void vert(inout appdata_full v) {
				float4 state = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y, 0, 0));	
				v.vertex.y += state.r + state.g;
			}
			
			void surf(Input IN, inout SurfaceOutputStandard  o) {
				// Normals 
				float4 state = tex2D(_StateTex, IN.uv_MainTex.x);
				float4 state_l = tex2D(_StateTex, float2(IN.uv_MainTex.x + _StateTex_TexelSize.x, IN.uv_MainTex.y));
				float4 state_r = tex2D(_StateTex, float2(IN.uv_MainTex.x - _StateTex_TexelSize.x, IN.uv_MainTex.y));
				float4 state_t = tex2D(_StateTex, float2(IN.uv_MainTex.x, IN.uv_MainTex.y + _StateTex_TexelSize.y));
				float4 state_b = tex2D(_StateTex, float2(IN.uv_MainTex.x, IN.uv_MainTex.y - _StateTex_TexelSize.y));

				half dhhx = 0.5 * (FULL_HEIGHT(state_r) - FULL_HEIGHT(state_l));
				half dhhy = 0.5 * (FULL_HEIGHT(state_t) - FULL_HEIGHT(state_b));

				
				//half dx = 0.5 * (FULL_HEIGHT(state_l) - FULL_HEIGHT(state)) - 0.5 * (FULL_HEIGHT(state_r) - FULL_HEIGHT(state));
				//half dz = 0.5 * (FULL_HEIGHT(state_t) - FULL_HEIGHT(state)) - 0.5 * (FULL_HEIGHT(state_b) - FULL_HEIGHT(state));
				half3 normal = half3(_NormalStrength * dhhx, 1, _NormalStrength * dhhy);
				//o.Normal = UnityObjectToWorldNormal(normalize(normal));
				//o.Normal = normalize(normal);


				// Brush
				float brushPresence = saturate(sign(abs(_InputControls.z) - length(IN.uv_MainTex - _InputControls.xy)));

				float2 uv = IN.screenPos.xy / IN.screenPos.w;
				#if UNITY_UV_STARTS_AT_TOP
					if (_CameraDepthTexture_TexelSize.y < 0) {
						uv.y = 1 - uv.y;
					}
				#endif

				float backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
				float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(IN.screenPos.z);
				float depthDifference = (backgroundDepth - surfaceDepth);			

				
				//clip(state.g - 0.08 + brushPresence);

				o.Metallic = _Metallic;
				o.Smoothness = _Smoothness;
				o.Alpha = saturate(clamp(depthDifference * _DepthDecay * 4, 0, 0.8) + 0.4 * saturate(depthDifference * _DepthDecay * 0.5));
				//o.Alpha = _Color.a * saturate(2 * state.g);
				//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb * (1 - d * d * 0.1);
				//o.Albedo = (o.Normal + 1) * 0.5;
				o.Albedo = _Color.rgb * (1 - clamp(depthDifference * _DepthDecay, 0, 0.6));

				//o.Alpha = 1;
				//o.Albedo = saturate(1 - depthDifference * _DepthDecay);				
				
				o.Alpha = max(o.Alpha, brushPresence);
				o.Albedo = (1 - brushPresence) * o.Albedo + brushPresence * float3(1, 0, 0);
				
			}
			ENDCG
		}
		*/
		Fallback "Diffuse"		
}