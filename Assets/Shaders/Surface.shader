Shader "Custom/Terrain" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_NormalMap("Normals", 2D) = "bump" {}
		[NoScaleOffset] _StateTex("State", 2D) = "black" {}
		[NoScaleOffset] _NormalStrength("NormalStrength", Range(0.1, 100)) = 0.5
		_BumpScale("_BumpScale", Range(0.1, 100)) = 1
	}
	SubShader 
	{
		Tags { "Queue"="Geometry"  "RenderType"="Opaque"  "IgnoreProjector"="True"}

		Pass
		{
			Tags {"LightMode"="ForwardBase" }

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "Lighting.cginc"

			// compile shader into multiple variants, with and without shadows
			// (we don't care about any lightmaps yet, so skip these variants)
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight multi_compile_fog			
			#include "AutoLight.cginc"

			struct v2f
			{
				float2 uv : TEXCOORD0;				
				float2 uvState : TEXCOORD1;								
				float4 pos : SV_POSITION;
				SHADOW_COORDS(2) 
				UNITY_FOG_COORDS(3)
			};

			float _NormalStrength;			
			sampler2D _MainTex;
			float4 _MainTex_ST;

			sampler2D _NormalMap;
			float _BumpScale;

			sampler2D _StateTex;
			float2 _StateTex_TexelSize;

			#define WATER_HEIGHT(s) (s.g)
			#define TERRAIN_HEIGHT(s) (s.r)
			#define FULL_HEIGHT(s) (TERRAIN_HEIGHT(s) + WATER_HEIGHT(s))

			v2f vert(appdata_base v)
			{
				float4 state = tex2Dlod(_StateTex, float4(v.texcoord.x, v.texcoord.y, 0, 0));
				v.vertex.y += TERRAIN_HEIGHT(state);

				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = v.texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
				o.uvState = v.texcoord;				
				
				TRANSFER_SHADOW(o)
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}			

			fixed4 frag(v2f i) : SV_Target
			{				
				float2 du = float2(_StateTex_TexelSize.x * 0.5, 0);
				float2 dv = float2(0, _StateTex_TexelSize.y * 0.5);
				float4 state_l = tex2D(_StateTex, i.uvState + du);
				float4 state_r = tex2D(_StateTex, i.uvState - du);
				float4 state_t = tex2D(_StateTex, i.uvState + dv);
				float4 state_b = tex2D(_StateTex, i.uvState - dv);

				half dhdu = _NormalStrength * 0.5 * (TERRAIN_HEIGHT(state_r) - TERRAIN_HEIGHT(state_l));
				half dhdv = _NormalStrength * 0.5 * (TERRAIN_HEIGHT(state_b) - TERRAIN_HEIGHT(state_t));

				float3 normal = float3(dhdu, 1, dhdv);
				float3 bump = UnpackScaleNormal(tex2D(_NormalMap, i.uv), _BumpScale);				
				
				float3 worldNormal = UnityObjectToWorldNormal(normalize(normal + bump));

				half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
				half diff = nl * _LightColor0.rgb;
				half ambient = ShadeSH9(half4(worldNormal, 1));

				fixed4 col = tex2D(_MainTex, i.uv);				
				
				// compute shadow attenuation (1.0 = fully lit, 0.0 = fully shadowed)
				fixed shadow = SHADOW_ATTENUATION(i);
				// darken light's illumination with shadow, keep ambient intact
				fixed3 lighting = diff * shadow + ambient;
				col.rgb *= lighting;

				UNITY_APPLY_FOG(i.fogCoord, col);


				return col;
			}
			ENDCG
		}

		Pass
		{
			Tags {"LightMode" = "ShadowCaster"}		
			LOD 300

			ZWrite On 
			ZTest Less
			Offset 1, 1
			Cull Off
			//ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_shadowcaster
			#pragma fragmentoption ARB_precision_hint_fastest
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