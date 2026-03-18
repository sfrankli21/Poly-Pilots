Shader "Custom/RO/GameObject"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,0,0,1)
        _Stencil ("Reveal Layer", Float) = 1
        _StencilReadMask ("Stencil Read Mask", Float) = 255
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
        }

        Pass
        {
            Cull Back
            ZWrite Off
            ZTest Always

            Stencil
            {
                Ref [_Stencil]
                Comp Equal
                Pass Keep
                Fail Keep
                ZFail Keep
                ReadMask [_StencilReadMask]
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _BaseColor;

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _BaseColor;
            }
            ENDHLSL
        }
    }
}