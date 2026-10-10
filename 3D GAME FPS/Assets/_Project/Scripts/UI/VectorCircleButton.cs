using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Project.UI
{
    /// <summary>
    /// Circular mobile action button with solid dark navy background,
    /// gradient neon ring border, and tactile press scaling.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class VectorCircleButton : MaskableGraphic, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Colors")]
        public Color backgroundColor = new Color(0.04f, 0.08f, 0.18f, 0.88f); // Dark Navy
        public Color borderGradientTop = new Color(0.0f, 0.95f, 1.0f, 1.0f);  // Neon start
        public Color borderGradientBottom = new Color(0.0f, 0.4f, 0.9f, 1.0f); // Neon end
        public Color pressedOverlay = new Color(1f, 1f, 1f, 0.15f);

        [Header("Border Settings")]
        [Range(2f, 12f)] public float borderThickness = 4.5f;
        [Range(16, 96)] public int segments = 48;

        public bool isPressed { get; private set; }

        public void OnPointerDown(PointerEventData eventData)
        {
            isPressed = true;
            rectTransform.localScale = Vector3.one * 0.92f;
            SetVerticesDirty();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
            rectTransform.localScale = Vector3.one;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            float radius = Mathf.Min(r.width, r.height) * 0.5f;
            if (radius <= 0f) return;

            Vector2 center = r.center;
            float innerR = Mathf.Max(0f, radius - borderThickness);
            float angleStep = (Mathf.PI * 2f) / segments;

            Color currentBg = isPressed ? Color.Lerp(backgroundColor, Color.white, 0.15f) : backgroundColor;

            // 1. Draw Dark Navy Background Disc
            int centerIdx = vh.currentVertCount;
            vh.AddVert(new Vector3(center.x, center.y, 0), currentBg, Vector2.zero);

            for (int i = 0; i <= segments; i++)
            {
                float a = i * angleStep;
                Vector2 p = center + new Vector2(Mathf.Cos(a) * innerR, Mathf.Sin(a) * innerR);
                vh.AddVert(new Vector3(p.x, p.y, 0), currentBg, Vector2.zero);
            }

            for (int i = 1; i <= segments; i++)
            {
                vh.AddTriangle(centerIdx, centerIdx + i, centerIdx + i + 1);
            }

            // 2. Draw Gradient Neon Border Ring
            for (int i = 0; i < segments; i++)
            {
                float a0 = i * angleStep;
                float a1 = (i + 1) * angleStep;

                float t0 = (Mathf.Sin(a0) + 1f) * 0.5f;
                float t1 = (Mathf.Sin(a1) + 1f) * 0.5f;

                Color c0 = Color.Lerp(borderGradientBottom, borderGradientTop, t0);
                Color c1 = Color.Lerp(borderGradientBottom, borderGradientTop, t1);

                if (isPressed)
                {
                    c0 = Color.Lerp(c0, Color.white, 0.3f);
                    c1 = Color.Lerp(c1, Color.white, 0.3f);
                }

                Vector2 o0 = center + new Vector2(Mathf.Cos(a0) * radius, Mathf.Sin(a0) * radius);
                Vector2 o1 = center + new Vector2(Mathf.Cos(a1) * radius, Mathf.Sin(a1) * radius);
                Vector2 i0 = center + new Vector2(Mathf.Cos(a0) * innerR, Mathf.Sin(a0) * innerR);
                Vector2 i1 = center + new Vector2(Mathf.Cos(a1) * innerR, Mathf.Sin(a1) * innerR);

                int baseIdx = vh.currentVertCount;
                vh.AddVert(new Vector3(o0.x, o0.y, 0), c0, Vector2.zero);
                vh.AddVert(new Vector3(o1.x, o1.y, 0), c1, Vector2.zero);
                vh.AddVert(new Vector3(i1.x, i1.y, 0), c1, Vector2.zero);
                vh.AddVert(new Vector3(i0.x, i0.y, 0), c0, Vector2.zero);

                vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
                vh.AddTriangle(baseIdx + 2, baseIdx + 3, baseIdx);
            }
        }
    }
}
