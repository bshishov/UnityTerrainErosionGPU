Shader "Plot/Grid"
{
    Properties
    {
		_Color("Color", Color) = (1, 1, 1, 0.25)
		_BackgroundColor("Background", Color) = (0, 0, 0, 0.1)		
		_LineDistance("Major Line Distance", Range(0, 2)) = .1
		_LineThickness("Major Line Thickness", Range(0, 0.1)) = 0.0016
		[IntRange]_SubLines("Lines between major lines", Range(1, 10)) = 10
		_SubLineThickness("Thickness of inbetween lines", Range(0, 0.05)) = 0.0003
    }
    SubShader
    {
        Tags { "Queue" = "Transparent"  "RenderType" = "Transparent" }
        LOD 100
		Blend SrcAlpha OneMinusSrcAlpha

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
			fixed4 _Color;
			fixed4 _BackgroundColor;
			
			float _LineDistance;
			float _LineThickness;
			float _SubLines;
			float _SubLineThickness;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;                
                return o;
            }

			float gridLines(float dist) {
				float distanceChange = fwidth(dist) * 0.5;
				float majorLineDistance = abs(frac(dist / _LineDistance + 0.5) - 0.5) * _LineDistance;
				float majorLines = smoothstep(_LineThickness - distanceChange, _LineThickness + distanceChange, majorLineDistance);

				float distanceBetweenSubLines = _LineDistance / _SubLines;
				float subLineDistance = abs(frac(dist / distanceBetweenSubLines + 0.5) - 0.5) * distanceBetweenSubLines;
				float subLines = smoothstep(_SubLineThickness - distanceChange, _SubLineThickness + distanceChange, subLineDistance);

				return saturate((1 - majorLines) + (1 - subLines));
			}

            fixed4 frag (v2f i) : SV_Target
            {
				float dist = i.uv.x;				
				
				float grid = saturate(gridLines(i.uv.x) + gridLines(i.uv.y));			
                
				return grid * _Color + (1 - grid) * _BackgroundColor;
            }
            ENDCG
        }
    }
}
