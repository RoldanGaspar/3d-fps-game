using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Procedural vector hollow ring with optional gradient.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class VectorRing : MaskableGraphic
    {
        [Range(4, 128)] public int segments = 64;
        [Min(0.5f)] public float thickness = 4f;
        public bool isFilled = false;
        public Color gradientSecondary = Color.clear;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect rect = rectTransform.rect;
            float outerR = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (outerR <= 0f) return;

            float innerR = isFilled ? 0f : Mathf.Max(0f, outerR - thickness);
            Vector2 center = rect.center;
            float angleStep = (Mathf.PI * 2f) / segments;
            bool hasGrad = gradientSecondary.a > 0.01f;

            for (int i = 0; i < segments; i++)
            {
                float a0 = i * angleStep;
                float a1 = (i + 1) * angleStep;

                Color c0 = color;
                Color c1 = color;
                if (hasGrad)
                {
                    float t0 = (Mathf.Sin(a0) + 1f) * 0.5f;
                    float t1 = (Mathf.Sin(a1) + 1f) * 0.5f;
                    c0 = Color.Lerp(color, gradientSecondary, t0);
                    c1 = Color.Lerp(color, gradientSecondary, t1);
                }

                Vector2 o0 = center + new Vector2(Mathf.Cos(a0) * outerR, Mathf.Sin(a0) * outerR);
                Vector2 o1 = center + new Vector2(Mathf.Cos(a1) * outerR, Mathf.Sin(a1) * outerR);
                Vector2 i0 = center + new Vector2(Mathf.Cos(a0) * innerR, Mathf.Sin(a0) * innerR);
                Vector2 i1 = center + new Vector2(Mathf.Cos(a1) * innerR, Mathf.Sin(a1) * innerR);

                int baseIdx = vh.currentVertCount;
                vh.AddVert(new Vector3(o0.x, o0.y, 0f), c0, Vector2.zero);
                vh.AddVert(new Vector3(o1.x, o1.y, 0f), c1, Vector2.zero);
                vh.AddVert(new Vector3(i1.x, i1.y, 0f), c1, Vector2.zero);
                vh.AddVert(new Vector3(i0.x, i0.y, 0f), c0, Vector2.zero);

                vh.AddTriangle(baseIdx + 0, baseIdx + 1, baseIdx + 2);
                vh.AddTriangle(baseIdx + 2, baseIdx + 3, baseIdx + 0);
            }
        }
    }
}
