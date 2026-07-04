Shader "Custom/InvisibleWallURP"
{
    SubShader
    {
        // Tells Unity to draw this right before it draws normal objects
        Tags { "RenderType"="Opaque" "Queue"="Geometry-1" "RenderPipeline"="UniversalPipeline" }
        
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            
            // Turn ON depth writing (blocks the boxes behind it)
            ZWrite On
            
            // Turn OFF all color rendering (makes it completely invisible)
            ColorMask 0
            
            // Turn OFF backface culling (makes it work on both sides of the plane)
            Cull Off
        }
    }
}