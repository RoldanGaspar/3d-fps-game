using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Renders a UI panel with dark navy blue background and gradient neon border.
    /// Supports optional chamfered (cut) corners for a sci-fi tactical look.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class VectorPanel : MaskableGraphic
    {
        [Header("Colors")]
        public Color backgroundColor = new Color(0.04f, 0.07f, 0.16f, 0.88f); // Dark Navy Blue
        public Color borderGradientStart = new Color(0.0f, 0.95f, 1.0f, 1.0f); // Neon Cyan
        public Color borderGradientEnd = new Color(0.0f, 0.5f, 1.0f, 1.0f);   // Neon Blue

        [Header("Border Settings")]
        [Range(1f, 12f)] public float borderWidth = 3f;
        public float cornerCutSize = 12f;
        public bool cutTopRight = true;
        public bool cutBottomLeft = true;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (r.width <= 0 || r.height <= 0) return;

            float w = r.width;
            float h = r.height;
            float cut = Mathf.Min(cornerCutSize, Mathf.Min(w, h) * 0.3f);
            float b = borderWidth;

            // Define 8 outline vertices for outer edge (with cut corners)
            Vector2[] outer = new Vector2[8];
            outer[0] = new Vector2(r.xMin, r.yMin + (cutBottomLeft ? cut : 0));
            outer[1] = new Vector2(r.xMin + (cutBottomLeft ? cut : 0), r.yMin);
            outer[2] = new Vector2(r.xMax, r.yMin);
            outer[3] = new Vector2(r.xMax, r.yMax - (cutTopRight ? cut : 0));
            outer[4] = new Vector2(r.xMax - (cutTopRight ? cut : 0), r.yMax);
            outer[5] = new Vector2(r.xMin, r.yMax);
            outer[6] = new Vector2(r.xMin, r.yMax);
            outer[7] = new Vector2(r.xMin, r.yMin);

            // Compute inner vertices inset by borderWidth
            Vector2[] inner = new Vector2[6];
            inner[0] = new Vector2(r.xMin + b, r.yMin + b + (cutBottomLeft ? cut * 0.7f : 0));
            inner[1] = new Vector2(r.xMin + b + (cutBottomLeft ? cut * 0.7f : 0), r.yMin + b);
            inner[2] = new Vector2(r.xMax - b, r.yMin + b);
            inner[3] = new Vector2(r.xMax - b, r.yMax - b - (cutTopRight ? cut * 0.7f : 0));
            inner[4] = new Vector2(r.xMax - b - (cutTopRight ? cut * 0.7f : 0), r.yMax - b);
            inner[5] = new Vector2(r.xMin + b, r.yMax - b);

            // 1. Draw Background Fill (Dark Navy Blue)
            int bgCenter = vh.currentVertCount;
            vh.AddVert(new Vector3(r.center.x, r.center.y, 0), backgroundColor, Vector2.zero);

            for (int i = 0; i < inner.Length; i++)
            {
                vh.AddVert(new Vector3(inner[i].x, inner[i].y, 0), backgroundColor, Vector2.zero);
            }

            for (int i = 0; i < inner.Length; i++)
            {
                int next = (i + 1) % inner.Length;
                vh.AddTriangle(bgCenter, bgCenter + 1 + i, bgCenter + 1 + next);
            }

            // 2. Draw Gradient Neon Border
            Vector2[] outer6 = new Vector2[6];
            outer6[0] = new Vector2(r.xMin, r.yMin + (cutBottomLeft ? cut : 0));
            outer6[1] = new Vector2(r.xMin + (cutBottomLeft ? cut : 0), r.yMin);
            outer6[2] = new Vector2(r.xMax, r.yMin);
            outer6[3] = new Vector2(r.xMax, r.yMax - (cutTopRight ? cut : 0));
            outer6[4] = new Vector2(r.xMax - (cutTopRight ? cut : 0), r.yMax);
            outer6[5] = new Vector2(r.xMin, r.yMax);

            for (int i = 0; i < 6; i++)
            {
                int next = (i + 1) % 6;
                float t0 = (float)i / 6f;
                float t1 = (float)(i + 1) / 6f;
                Color c0 = Color.Lerp(borderGradientStart, borderGradientEnd, t0);
                Color c1 = Color.Lerp(borderGradientStart, borderGradientEnd, t1);

                int baseIdx = vh.currentVertCount;
                vh.AddVert(new Vector3(outer6[i].x, outer6[i].y, 0), c0, Vector2.zero);
                vh.AddVert(new Vector3(outer6[next].x, outer6[next].y, 0), c1, Vector2.zero);
                vh.AddVert(new Vector3(inner[next].x, inner[next].y, 0), c1, Vector2.zero);
                vh.AddVert(new Vector3(inner[i].x, inner[i].y, 0), c0, Vector2.zero);

                vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
                vh.AddTriangle(baseIdx + 2, baseIdx + 3, baseIdx);
            }
        }
    }
}
