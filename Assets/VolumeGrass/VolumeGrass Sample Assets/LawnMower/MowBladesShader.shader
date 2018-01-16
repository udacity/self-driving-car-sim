Shader "MowShader" {
	Properties {
		mow_depth ("Mow Depth", Color) = (1,1,1,1)
	}
    SubShader {
    	Pass {
			ZWrite On
			ZTest Less
    		Color [mow_depth]
    	}
    }
}