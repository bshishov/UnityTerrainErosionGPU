Shader "Custom/WaterBoundary" {
	Properties{		
		_Color("Color", Color) = (0, 0, 1, 0.8)
		[NoScaleOffset] _StateTex("State", 2D) = "black" {}		
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
			float3 _WorldScale;
			fixed4 _Color;

			#define WATER_HEIGHT(s) (s.g)
			#define TERRAIN_HEIGHT(s) (s.r)
			#define FULL_HEIGHT(s) (TERRAIN_HEIGHT(s) + WATER_HEIGHT(s))

			v2f vert(appdata_base v)
			{
			    float4 worldPos = mul(unity_ObjectToWorld, v.vertex);			
			    
			    // Notice that state is sample at uv = worldPos / _WorldScale
				float4 state = tex2Dlod(_StateTex, float4(worldPos.x / _WorldScale.x, worldPos.z / _WorldScale.z, 0, 0));
				
				// if mesh uv.y is 0 - then y of vertex = terrain, if 1 then y of vertex = water
				v.vertex.y = TERRAIN_HEIGHT(state) + v.texcoord.y * WATER_HEIGHT(state);

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
			    // return simple Color
			    // TODO: Adapt water shader 
			    return _Color;
			}
			ENDCG
		}
	}

	Fallback "Diffuse"		
}