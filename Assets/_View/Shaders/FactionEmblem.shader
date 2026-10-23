Shader "Cursed Deck/UI/Faction Emblem"
{
 Properties
 {
  [PerRendererData] _MainTex ("Sprite Texture",2D)="white" {}
  _Color ("Tint",Color)=(1,1,1,1)
  _FactionTint ("Faction fill",Color)=(1,1,1,1)
  _Mode ("1 neutral fill, 2 blue fill",Float)=1
  _StencilComp ("Stencil Comparison",Float)=8
  _Stencil ("Stencil ID",Float)=0
  _StencilOp ("Stencil Operation",Float)=0
  _StencilWriteMask ("Stencil Write Mask",Float)=255
  _StencilReadMask ("Stencil Read Mask",Float)=255
  _ColorMask ("Color Mask",Float)=15
  [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip",Float)=0
 }
 SubShader
 {
  Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
  Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
  Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
  Blend SrcAlpha OneMinusSrcAlpha
  ColorMask [_ColorMask]
  Pass
  {
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #pragma target 2.0
   #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
   #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
   #include "UnityCG.cginc"
   #include "UnityUI.cginc"
   struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
   struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 world:TEXCOORD1;UNITY_VERTEX_OUTPUT_STEREO};
   sampler2D _MainTex;float4 _Color,_TextureSampleAdd,_FactionTint;float _Mode;float4 _ClipRect;
   v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.world=v.vertex;o.vertex=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.color=v.color*_Color;return o;}
   float4 frag(v2f i):SV_Target
   {
    float4 c=tex2D(_MainTex,i.uv)+_TextureSampleAdd;
    // Preserve saturated gold filigree and ruby gems. Recolor only neutral stone or blue gemstone fill.
    float highest=max(c.r,max(c.g,c.b)),lowest=min(c.r,min(c.g,c.b));
    float neutral=1-step(.29,highest-lowest);
    float blue=step(c.r*1.2,c.b)*step(c.g*1.05,c.b);
    float mask=_Mode<1.5?neutral:blue;
    float light=saturate(dot(c.rgb,float3(.299,.587,.114))*1.3);
    c.rgb=lerp(c.rgb,_FactionTint.rgb*light,mask);c*=i.color;
    #ifdef UNITY_UI_CLIP_RECT
    c.a*=UnityGet2DClipping(i.world.xy,_ClipRect);
    #endif
    #ifdef UNITY_UI_ALPHACLIP
    clip(c.a-.001);
    #endif
    return c;
   }
   ENDCG
  }
 }
}
