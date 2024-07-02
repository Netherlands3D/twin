Shader "DepthMask Projector"
{	
	Properties
	{
		_MainTex ("Cookie", 2D) = "gray" { TexGen ObjectLinear }
		_Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
	}
	
	SubShader
	{
		Tags {"Queue"="Geometry-100" }

		Lighting Off

		ColorMask A
		
        Pass 
        {
        	ZWrite On
			ZTest Less
		
        	Alphatest Greater [_Cutoff]
			AlphaToMask True
			
			Offset -1, -1
				
			SetTexture [_MainTex] 
			{
				Combine texture
				Matrix [_Projector]
			} 
		}   
	}
}
