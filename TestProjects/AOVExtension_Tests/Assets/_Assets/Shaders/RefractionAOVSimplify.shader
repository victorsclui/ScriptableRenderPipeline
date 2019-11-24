Shader "Hidden/RefractionAOVSimplify"
{
    Properties
    {
        _MainTex ("Main Input", 2D) = "white" {}
		_TransmittanceMap("Transmittance Map", 2D) = "white" {}
    }
    SubShader
    {
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
			sampler2D _TransmittanceMap;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed4 col = tex2D(_MainTex, i.uv);
				fixed4 transmittance = tex2D(_TransmittanceMap, i.uv);

				// Black out refraction AOV when transmittance is super small
				col.xyz = col.xyz * step(0.001, transmittance.x + transmittance.y + transmittance.z);
				return col;
            }

            ENDCG
        }
    }
}
