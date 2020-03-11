Shader "Plot/Surface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Channel("Channel", int) = 0
		_HeightScale("Height scale", float) = 1
		_ColorRamp("Color ramp", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
			Cull Off

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

            sampler2D _MainTex;
			sampler2D _ColorRamp;
            float4 _MainTex_ST;
			int _Channel;
			float _MinValue;
			float _MaxValue;
			float _HeightScale;

            v2f vert (appdata v)
            {
				uint channel = clamp(_Channel, 0, 4);
				float value = tex2Dlod(_MainTex, float4(v.uv.x, v.uv.y, 0, 0))[channel];

				float k = (value - _MinValue) / (_MaxValue - _MinValue);
				v.vertex.y = k * _HeightScale;

                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);							
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {                
				uint channel = clamp(_Channel, 0, 4);
                float value = tex2D(_MainTex, i.uv)[channel];
				float k = (value - _MinValue) / (_MaxValue - _MinValue);
                float4 cmap = tex2D(_ColorRamp, float2(clamp(k, 0, 1), 0.5));

				return cmap;

				/*

				float ld = .1;
				float lt = .001;
				float distanceChange = fwidth(k) * 0.5;
				float majorLineDistance = abs(frac(k / ld + 0.5) - 0.5) * ld;
				float majorLines = smoothstep(lt - distanceChange, lt + distanceChange, majorLineDistance);
				return cmap * majorLines;
				*/
            }
            ENDCG
        }
    }
}
