using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Procedural segmented angled bar with dark background track and solid neon blocks.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class VectorSegmentedBar : MaskableGraphic
    {
        [Range(1, 40)] public int totalSegments = 10;
        [Range(0f, 1f)] public float fillPercent = 1f;
        public float spacing = 4.5f;
        public float slantAngle = 14f;
        public Color emptyColor = new Color(0.04f, 0.08f, 0.18f, 0.6f); // Dark Navy empty block
        public Color fillGradientEnd = Color.clear;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            if (r.width <= 0f || r.height <= 0f) return;

            float totalSpacing = (totalSegments - 1) * spacing;
            float segWidth = (r.width - totalSpacing) / totalSegments;
            float h = r.height;
            float slantOffset = Mathf.Tan(slantAngle * Mathf.Deg2Rad) * h;

            int activeCount = Mathf.CeilToInt(totalSegments * Mathf.Clamp01(fillPercent));
            bool hasGrad = fillGradientEnd.a > 0.01f;

            for (int i = 0; i < totalSegments; i++)
            {
                float x0 = r.xMin + i * (segWidth + spacing);
                float x1 = x0 + segWidth;
                float t = (float)i / Mathf.Max(1, totalSegments - 1);

                Color segColor = (i < activeCount) ? (hasGrad ? Color.Lerp(color, fillGradientEnd, t) : color) : emptyColor;

                Vector3 b0 = new Vector3(x0, r.yMin, 0);
                Vector3 t0 = new Vector3(x0 + slantOffset, r.yMax, 0);
                Vector3 t1 = new Vector3(x1 + slantOffset, r.yMax, 0);
                Vector3 b1 = new Vector3(x1, r.yMin, 0);

                int baseIdx = vh.currentVertCount;
                vh.AddVert(b0, segColor, Vector2.zero);
                vh.AddVert(t0, segColor, Vector2.zero);
                vh.AddVert(t1, segColor, Vector2.zero);
                vh.AddVert(b1, segColor, Vector2.zero);

                vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
                vh.AddTriangle(baseIdx + 2, baseIdx + 3, baseIdx);
            }
        }
    }
}
