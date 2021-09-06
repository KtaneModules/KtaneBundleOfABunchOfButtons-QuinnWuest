Shader "KT/Custom/Masks/Mask 75" {
    Properties {
    }

    SubShader {
        Tags { "RenderType"="Transparent" }
        ColorMask 0
        Stencil {
            ref 75
            Comp Always
            Pass replace
        }
        LOD 150

        ZWrite Off

		pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			struct appdata_t {
				float4 vertex : POSITION;
			};

			struct v2f {
				float4 p : SV_POSITION;
			};

			v2f vert (appdata_t v)
			{
				v2f o;
				o.p = UnityObjectToClipPos(v.vertex);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				return fixed4(0, 0, 0, 0);
			}
			ENDCG
		}
    }
}