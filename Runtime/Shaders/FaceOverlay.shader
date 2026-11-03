// The face's tints, drawn as a second pass over the skin (FaceOverlay): a blush on the
// cheeks and a soft spot on the nose (each its own channel) with hatch lines for a strong one,
// gloom over the upper face, tear streaks. Everything is placed in the face's own space (FaceFrame, in eye
// gaps) from the pixel's world position, so it needs no mask texture, works on any face mesh
// with eye bones, and the lines stay straight on the face. A 2x multiply blend: 0.5 leaves the
// skin as it is, below darkens, above lightens, so the skin's own lighting carries through.
Shader "CupkekGames/Character/FaceOverlay"
{
  Properties
  {
    [HideInInspector] _Skip ("Skip", Float) = 0
  }

  SubShader
  {
    Tags { "Queue" = "Transparent-20" "RenderType" = "Transparent" "IgnoreProjector" = "True" }

    Pass
    {
      Name "FaceOverlay"
      Tags { "LightMode" = "SRPDefaultUnlit" }
      ZWrite Off
      ZTest LEqual
      Cull Back
      Offset -1, -1
      Blend DstColor SrcColor

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_instancing
      #include "UnityCG.cginc"

      float _Skip;

      // The face: xyz the point between the eyes, w the gap between them; her axes.
      float4 _FaceOrigin;
      float4 _FaceRight;
      float4 _FaceUp;
      float4 _FaceForward;

      // Amounts, 0 to 1: x blush, y blush lines, z gloom, w tears.
      float4 _Amounts;

      float4 _BlushColor;
      float4 _LinesColor;
      float4 _GloomColor;
      float4 _TearsColor;

      // x cheek's distance out from the middle, y down from the eyes, zw the blush's half size.
      float4 _Cheek;
      // 0 a soft glow fading out, 1 a crisp painted oval.
      float _BlushEdge;
      // x line spacing, y line width, z slant in degrees, w the strokes' half length (of the blush's half height).
      float4 _Lines;
      // The nose spot: y down from the eyes, zw its half size (x unused); its amount, 0 to 1.
      float4 _Nose;
      float _NoseAmount;
      // x where gloom starts (up from the eyes), y where it is full.
      float4 _Gloom;
      // x the streak's distance out from the middle, y its top, z its bottom, w its width.
      float4 _Tear;
      // How far behind the eyes the face still counts, so the back of the head never tints.
      float _Depth;

      struct appdata
      {
        float4 vertex : POSITION;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f
      {
        float4 pos : SV_POSITION;
        float3 world : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      v2f vert(appdata v)
      {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.pos = UnityObjectToClipPos(v.vertex);
        o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
        return o;
      }

      // 1 inside a line of half width w at distance t, antialiased.
      float Line(float t, float w)
      {
        float aa = max(fwidth(t), 1e-4);
        return 1.0 - smoothstep(w - aa, w + aa, t);
      }

      float4 frag(v2f i) : SV_Target
      {
        if (_Skip > 0.5) discard;

        float3 d = (i.world - _FaceOrigin.xyz) / max(_FaceOrigin.w, 1e-5);
        float3 q = float3(dot(d, _FaceRight.xyz), dot(d, _FaceUp.xyz), dot(d, _FaceForward.xyz));
        float front = smoothstep(-_Depth, -_Depth * 0.6, q.z);

        // Blush: a soft ellipse on each cheek.
        float side = q.x >= 0.0 ? 1.0 : -1.0;
        float2 c = float2(q.x - side * _Cheek.x, q.y + _Cheek.y);
        float2 e = c / _Cheek.zw;
        float r2 = dot(e, e);
        float2 n = float2(q.x, q.y + _Nose.y) / _Nose.zw;
        float nose = exp(-dot(n, n) * 1.6) * front;
        float blush = lerp(exp(-r2 * 1.6), 1.0 - smoothstep(0.75, 1.0, r2), _BlushEdge) * front;

        // Hatch lines: straight strokes across each cheek, the same slant on both (///).
        float angle = radians(_Lines.z);
        float2 across = float2(cos(angle), sin(angle));
        float u = dot(c, across) / _Lines.x;
        float nearest = clamp(round(u), -1.0, 1.0);
        float t = abs(u - nearest) * _Lines.x;
        float2 alongDir = float2(-across.y, across.x);
        float along = dot(c, alongDir) / (_Cheek.w * _Lines.w);
        float reach = 1.0 - smoothstep(0.6, 1.0, abs(along));
        float lines = Line(t, _Lines.y * 0.5) * reach * step(abs(u), 1.5) * front;

        // Gloom: the upper face darkens from the eyes up.
        float gloom = smoothstep(_Gloom.x, _Gloom.y, q.y) * front;

        // Tears: a streak down from under each eye, fading as it runs.
        float dx = abs(abs(q.x) - _Tear.x);
        float run = saturate((_Tear.y - q.y) / max(_Tear.y - _Tear.z, 1e-4));
        float inside = step(q.y, _Tear.y) * step(_Tear.z, q.y);
        float tears = Line(dx, _Tear.w * 0.5 * (1.0 - run * 0.4)) * inside * (1.0 - run * run) * front;

        float3 f = 1.0;
        f = lerp(f, f * _GloomColor.rgb, saturate(gloom * _Amounts.z * _GloomColor.a));
        f = lerp(f, f * _BlushColor.rgb, saturate(max(blush * _Amounts.x, nose * _NoseAmount) * _BlushColor.a));
        f = lerp(f, f * _LinesColor.rgb, saturate(lines * _Amounts.y * _LinesColor.a));
        f = lerp(f, _TearsColor.rgb, saturate(tears * _Amounts.w * _TearsColor.a));
        return float4(f * 0.5, 1.0);
      }
      ENDCG
    }
  }
}
