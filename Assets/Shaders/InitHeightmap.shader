Shader "Custom/InitHeightmap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Scale("Scale", float) = 1
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

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
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;
			float2 _MainTex_TexelSize;
			float _Scale;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
				//float4 col = float4(1, 0, 0, 1);
				//col.r = i.uv.x + i.uv.y;				
                
				// Hardness
				col.a = saturate(0.2 + col.r * 0.8);
				col.rg *= _Scale;
				col.r += 20;				
				
                return col;
            }
            ENDCG
        }
    }
}
