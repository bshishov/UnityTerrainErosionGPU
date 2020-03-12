Shader "Hidden/Copy"
{
    SubShader
    {
        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            sampler2D _HTex;
			sampler2D _PrevTex;			
			float _Scale;
			float _Bias;			
			int _SrcChannel;
			int _TgtChannel;

            fixed4 frag (v2f_img i) : SV_Target
            {
                float colSrc = UnpackHeightmap(tex2D(_HTex, i.uv));
                float4 colPrev = tex2D(_PrevTex, i.uv);
                
                colPrev.r = colSrc * _Scale + _Bias;				
                return colPrev;
            }
            ENDCG
        }
    }
}
