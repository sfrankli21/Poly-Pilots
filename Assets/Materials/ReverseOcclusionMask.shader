Shader "Custom/ReverseOcclusionMask"
{
    SubShader
    {
        Tags
        {
            "Queue"="Geometry+10"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Pass
        {
            ColorMask 0
            ZWrite Off
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
            }
        }
    }
}