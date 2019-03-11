Shader "Custom/Water" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_Color("Color", Color) = (0, 0, 1, 0.8)
		_StateTex("State", 2D) = "black" {}
		_NormalStrength("NormalStrength", Range(0.1, 100)) = 0.5
		_SampleSize("Sample Size", Range(0.001, 0.1)) = 0.01		
		_Metallic("Metallic", Range(0, 1)) = 0.5
		_Smoothness("Smoothness", Range(0, 1)) = 1
	}
		SubShader{
			Tags { "RenderType" = "Transparent" "Queue"="Transparent" }			
			LOD 200
			
			ZWrite On

			CGPROGRAM
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

			void vert(inout appdata_full v) {
				float4 state = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y, 0, 0));

				float4 state_l = tex2Dlod(_StateTex, float4(v.texcoord.x + _SampleSize, v.texcoord.y, 0, 0));
				float4 state_r = tex2Dlod(_StateTex, float4(v.texcoord.x - _SampleSize, v.texcoord.y, 0, 0));
				float4 state_t = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y + _SampleSize, 0, 0));
				float4 state_b = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y - _SampleSize, 0, 0));

				v.normal.x = -_NormalStrength * (state_l.r + state_l.g - state_r.r - state_r.g);
				v.normal.y = 1;
				v.normal.z = _NormalStrength * (state_b.r + state_b.g - state_t.r - state_t.g);
				v.normal = normalize(v.normal);
				
				v.vertex.y += state.r + state.g;
			}

			void surf(Input IN, inout SurfaceOutputStandard  o) {
				float2 uv = IN.screenPos.xy / IN.screenPos.w;
				#if UNITY_UV_STARTS_AT_TOP
					if (_CameraDepthTexture_TexelSize.y < 0) {
						uv.y = 1 - uv.y;
					}
				#endif

				float backgroundDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
				float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(IN.screenPos.z);
				float depthDifference = (backgroundDepth - surfaceDepth);

				float4 state = tex2D(_StateTex, IN.uv_MainTex);
				float d = saturate(10 * state.g);
				//clip(state.g - 0.01);

				o.Metallic = _Metallic;
				o.Smoothness = _Smoothness;
				//o.Alpha = saturate(depthDifference / 5 + 0.1);
				o.Alpha = _Color.a * d;
				//o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb * _Color.rgb * (1 - d * d * 0.1);
				//o.Albedo = (o.Normal + 1) * 0.5;
				o.Albedo = _Color.rgb * (1 - clamp(depthDifference / 20, 0, 0.6));


				// Brush
				float dd = length(IN.uv_MainTex - _InputControls.xy);
				float k = saturate(sign(abs(_InputControls.z) - dd));
				//float x = dd / _InputControls.z;
				//float k = saturate(1 - pow(x, _InputControls.w));
				o.Alpha = max(o.Alpha, k);
				o.Albedo = (1 - k) * o.Albedo + k * float3(1, 0, 0);
				
			}
			ENDCG
		}
		Fallback "Diffuse"
}