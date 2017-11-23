// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "ootii/Projector/Simple Unlit" 
{
	Properties 
	{
		_Color ("Main Color", Color) = (1,1,1,1)
		_Alpha ("Alpha",Range(0.0,1.0)) = 1.0
	    _Attenuation("Falloff", Range(0.0, 1.0)) = 1.0
		_Projection ("Projector", 2D) = "white" {}
	}
	
	Subshader 
	{
		Tags {"Queue"="Transparent"}
		
		Pass 
		{
			ZWrite Off
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, -1
	
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#include "UnityCG.cginc"
			
			sampler2D _Projection;
			fixed4 _Color;
			float _Alpha;
			float _Attenuation;

			float4x4 unity_Projector;
			float4x4 unity_ProjectorClip;
			
			struct v2f 
			{
				float4 uvShadow : TEXCOORD0;
				float4 uvFalloff : TEXCOORD1;
				UNITY_FOG_COORDS(2)
				float4 pos : SV_POSITION;
			};
			
			v2f vert (float4 vertex : POSITION)
			{
				v2f o;
				o.pos = UnityObjectToClipPos (vertex);
				o.uvShadow = mul (unity_Projector, vertex);
				o.uvFalloff = mul (unity_ProjectorClip, vertex);
				UNITY_TRANSFER_FOG(o,o.pos);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 lColor = tex2Dproj (_Projection, UNITY_PROJ_COORD(i.uvShadow));
				lColor.rgb *= _Color.rgb;
				lColor.a *= _Alpha * _Color.a;

				UNITY_APPLY_FOG_COLOR(i.fogCoord, lColor, fixed4(0,0,0,0));

				float lDepth = i.uvShadow.z;
				return lColor * clamp(1.0 - abs(lDepth) + _Attenuation, 0.0, 1.0);

				return lColor;
			}
			ENDCG
		}
	}
}
