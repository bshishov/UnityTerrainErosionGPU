Shader "Custom/Surface" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_StateTex("State", 2D) = "black" {}
		_NormalStrength("NormalStrength", Range(0.1, 100)) = 0.5
		_SampleSize("Sample Size", Range(0.001, 0.1)) = 0.01
	}
	SubShader {
		Tags { "RenderType" = "Opaque" }
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert
		struct Input {			
			float2 uv_MainTex;
		};
		
		float _NormalStrength;
		float _SampleSize;
		sampler2D _MainTex;
		sampler2D _StateTex;

		void vert(inout appdata_full v) {
			float4 state = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y, 0, 0));
			float4 state_l = tex2Dlod(_StateTex, float4(v.texcoord.x + _SampleSize, v.texcoord.y, 0, 0));
			float4 state_r = tex2Dlod(_StateTex, float4(v.texcoord.x - _SampleSize, v.texcoord.y, 0, 0));
			float4 state_t = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y + _SampleSize, 0, 0));
			float4 state_b = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y - _SampleSize, 0, 0));

			v.normal.x = - _NormalStrength * (state_l.r - state_r.r);
			v.normal.y = 1;
			v.normal.z = _NormalStrength * (state_b.r - state_t.r);
			v.normal = normalize(v.normal);

			v.vertex.y += state.r;	
		}
		
		void surf(Input IN, inout SurfaceOutput o) {
			o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		}
		ENDCG
	}
	Fallback "Diffuse"
}