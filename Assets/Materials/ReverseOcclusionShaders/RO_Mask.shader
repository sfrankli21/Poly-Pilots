Shader "Custom/RO/Mask"
{
    Properties
    {
        _Stencil ("Reveal Layers", Float) = 1
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
    }

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
                Ref [_Stencil]
                Comp Always
                Pass Replace
                Fail Keep
                ZFail Keep
                WriteMask [_StencilWriteMask]
            }
        }
    }
}