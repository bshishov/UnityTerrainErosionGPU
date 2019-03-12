Shader "Custom/Terrain" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset] _StateTex("State", 2D) = "black" {}
		_NormalStrength("NormalStrength", Range(0.1, 100)) = 0.5
		_SampleSize("Sample Size", Range(0.001, 0.1)) = 0.01
	}
	SubShader 
	{
		Pass
		{
			Tags {"LightMode" = "ForwardBase"}

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps yet, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight multi_compile_fog
			// shadow helper functions and macros
			#include "AutoLight.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float2 uvState : TEXCOORD1;				
				fixed3 diff : COLOR0;
				fixed3 ambient : COLOR1;
				float4 pos : SV_POSITION;
				SHADOW_COORDS(2) 
				UNITY_FOG_COORDS(3)
			};

			float _NormalStrength;
			float _SampleSize;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _StateTex;

			v2f vert(appdata_base v)
			{
				float4 state = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y, 0, 0));
				float4 state_l = tex2Dlod(_StateTex, float4(v.texcoord.x + _SampleSize, v.texcoord.y, 0, 0));
				float4 state_r = tex2Dlod(_StateTex, float4(v.texcoord.x - _SampleSize, v.texcoord.y, 0, 0));
				float4 state_t = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y + _SampleSize, 0, 0));
				float4 state_b = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y - _SampleSize, 0, 0));

				v.normal.x = -_NormalStrength * (state_l.r - state_r.r);
				v.normal.y = 1;
				v.normal.z = _NormalStrength * (state_b.r - state_t.r);
				v.normal = normalize(v.normal);

				v.vertex.y += state.r;

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
				o.uvState = v.texcoord;
				half3 worldNormal = UnityObjectToWorldNormal(v.normal);
				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				o.diff = nl * _LightColor0.rgb;
				o.ambient = ShadeSH9(half4(worldNormal,1));
				
				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}			

			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
				// compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
				fixed shadow = SHADOW_ATTENUATION(i);
				// darken light's illumination with shadow, keep ambient intact
				fixed3 lighting = i.diff * shadow + i.ambient;
				col.rgb *= lighting;

				UNITY_APPLY_FOG(i.fogCoord, col);


				return col;
			}
			ENDCG
		}

		Pass
		{
			Tags {"LightMode" = "ShadowCaster"}			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#include "UnityCG.cginc"

			struct v2f {
				V2F_SHADOW_CASTER;
			};

			sampler2D _StateTex;

			v2f vert(appdata_base v)
			{
				float4 state = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y, 0, 0));
				v.vertex.y += state.r;

				v2f o;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				SHADOW_CASTER_FRAGMENT(i)
			}
			ENDCG
		}
	}	
}