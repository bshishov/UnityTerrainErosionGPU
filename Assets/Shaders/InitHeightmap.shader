Shader "Custom/InitHeightMap"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		[NoScaleOffset] _Hardness("Texture", 2D) = "white" {}
		_SeaLevel("SeaLevel", float) = 0
		_Scale("Scale", float) = 1
		_Bias("Bias", float) = 0
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
			sampler2D _Hardness;
			float2 _MainTex_TexelSize;
			float _Scale;
			float _Bias;
			float _SeaLevel;

            fixed4 frag (v2f i) : SV_Target
            {
                float4 col = tex2D(_MainTex, i.uv);
                
				// Terrain height				
				col.r = max(0, col.r * _Scale + _Bias);

				// Water height
				col.g = max(0, _SeaLevel - col.r);

				// Suspended sediment
				col.b = 0;
				
				// Hardness
				half h = tex2D(_Hardness, i.uv);
				col.a = saturate(0.2 + col.r * 0.8 * h);
				
                return col;
            }
            ENDCG
        }
    }
}
