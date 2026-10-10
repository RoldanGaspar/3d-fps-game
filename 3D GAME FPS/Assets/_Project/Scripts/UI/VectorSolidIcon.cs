using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Renders bold, solid-fill vector icons (Arrows, Cross, Bullet, Reload, Reticle).
    /// Highly visible on mobile screens.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class VectorSolidIcon : MaskableGraphic
    {
        public enum IconType
        {
            SolidArrowUp,
            SolidArrowDown,
            SolidMedicalCross,
            SolidReload,
            SolidFireTarget,
            SolidCameraView
        }

        public IconType iconType = IconType.SolidArrowUp;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            Vector2 c = r.center;
            float s = Mathf.Min(r.width, r.height) * 0.5f;
            if (s <= 0f) return;

            switch (iconType)
            {
                case IconType.SolidArrowUp:
                    DrawSolidArrow(vh, c, s, true);
                    break;
                case IconType.SolidArrowDown:
                    DrawSolidArrow(vh, c, s, false);
                    break;
                case IconType.SolidMedicalCross:
                    DrawSolidCross(vh, c, s);
                    break;
                case IconType.SolidReload:
                    DrawSolidReload(vh, c, s);
                    break;
                case IconType.SolidFireTarget:
                    DrawSolidFireTarget(vh, c, s);
                    break;
                case IconType.SolidCameraView:
                    DrawSolidCameraView(vh, c, s);
                    break;
            }
        }

        private void DrawSolidArrow(VertexHelper vh, Vector2 c, float s, bool up)
        {
            float dir = up ? 1f : -1f;
            float headHalfW = s * 0.75f;
            float headY = c.y + s * 0.75f * dir;
            float notchY = c.y - s * 0.05f * dir;
            float stemHalfW = s * 0.32f;
            float stemBottomY = c.y - s * 0.7f * dir;

            int b0 = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x, headY, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x - headHalfW, notchY, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x + headHalfW, notchY, 0), color, Vector2.zero);
            vh.AddTriangle(b0, b0 + 1, b0 + 2);

            int b1 = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x - stemHalfW, stemBottomY, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x - stemHalfW, notchY, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x + stemHalfW, notchY, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x + stemHalfW, stemBottomY, 0), color, Vector2.zero);
            vh.AddTriangle(b1, b1 + 1, b1 + 2);
            vh.AddTriangle(b1 + 2, b1 + 3, b1);
        }

        private void DrawSolidCross(VertexHelper vh, Vector2 c, float s)
        {
            float halfThick = s * 0.32f;
            float length = s * 0.85f;

            int b0 = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x - length, c.y - halfThick, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x - length, c.y + halfThick, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x + length, c.y + halfThick, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x + length, c.y - halfThick, 0), color, Vector2.zero);
            vh.AddTriangle(b0, b0 + 1, b0 + 2);
            vh.AddTriangle(b0 + 2, b0 + 3, b0);

            int b1 = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x - halfThick, c.y - length, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x - halfThick, c.y + length, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x + halfThick, c.y + length, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(c.x + halfThick, c.y - length, 0), color, Vector2.zero);
            vh.AddTriangle(b1, b1 + 1, b1 + 2);
            vh.AddTriangle(b1 + 2, b1 + 3, b1);
        }

        private void DrawSolidReload(VertexHelper vh, Vector2 c, float s)
        {
            int segments = 24;
            float outerR = s * 0.75f;
            float innerR = s * 0.45f;
            float step = (Mathf.PI * 0.72f) / segments;

            DrawArc(vh, c, outerR, innerR, 0.2f, step, segments);
            DrawArc(vh, c, outerR, innerR, Mathf.PI + 0.2f, step, segments);

            Vector2 tip1 = c + new Vector2(Mathf.Cos(0.2f) * (innerR + outerR) * 0.5f, Mathf.Sin(0.2f) * (innerR + outerR) * 0.5f);
            DrawTriangleArrowhead(vh, tip1, new Vector2(0.5f, 1f).normalized, s * 0.35f);

            Vector2 tip2 = c + new Vector2(Mathf.Cos(Mathf.PI + 0.2f) * (innerR + outerR) * 0.5f, Mathf.Sin(Mathf.PI + 0.2f) * (innerR + outerR) * 0.5f);
            DrawTriangleArrowhead(vh, tip2, new Vector2(-0.5f, -1f).normalized, s * 0.35f);
        }

        private void DrawArc(VertexHelper vh, Vector2 c, float oR, float iR, float startAngle, float step, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a0 = startAngle + i * step;
                float a1 = a0 + step;

                Vector2 o0 = c + new Vector2(Mathf.Cos(a0) * oR, Mathf.Sin(a0) * oR);
                Vector2 o1 = c + new Vector2(Mathf.Cos(a1) * oR, Mathf.Sin(a1) * oR);
                Vector2 i0 = c + new Vector2(Mathf.Cos(a0) * iR, Mathf.Sin(a0) * iR);
                Vector2 i1 = c + new Vector2(Mathf.Cos(a1) * iR, Mathf.Sin(a1) * iR);

                int idx = vh.currentVertCount;
                vh.AddVert(new Vector3(o0.x, o0.y, 0), color, Vector2.zero);
                vh.AddVert(new Vector3(o1.x, o1.y, 0), color, Vector2.zero);
                vh.AddVert(new Vector3(i1.x, i1.y, 0), color, Vector2.zero);
                vh.AddVert(new Vector3(i0.x, i0.y, 0), color, Vector2.zero);

                vh.AddTriangle(idx, idx + 1, idx + 2);
                vh.AddTriangle(idx + 2, idx + 3, idx);
            }
        }

        private void DrawTriangleArrowhead(VertexHelper vh, Vector2 tip, Vector2 dir, float size)
        {
            Vector2 norm = new Vector2(-dir.y, dir.x);
            Vector2 back = tip - dir * size;
            Vector2 left = back + norm * (size * 0.65f);
            Vector2 right = back - norm * (size * 0.65f);

            int idx = vh.currentVertCount;
            vh.AddVert(new Vector3(tip.x, tip.y, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(left.x, left.y, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(right.x, right.y, 0), color, Vector2.zero);
            vh.AddTriangle(idx, idx + 1, idx + 2);
        }

        private void DrawSolidFireTarget(VertexHelper vh, Vector2 c, float s)
        {
            float dotR = s * 0.28f;
            int segs = 18;
            float step = (Mathf.PI * 2f) / segs;
            int centerIdx = vh.currentVertCount;
            vh.AddVert(new Vector3(c.x, c.y, 0), color, Vector2.zero);
            for (int i = 0; i <= segs; i++)
            {
                float a = i * step;
                vh.AddVert(new Vector3(c.x + Mathf.Cos(a) * dotR, c.y + Mathf.Sin(a) * dotR, 0), color, Vector2.zero);
            }
            for (int i = 1; i <= segs; i++)
            {
                vh.AddTriangle(centerIdx, centerIdx + i, centerIdx + i + 1);
            }

            float tickW = s * 0.16f;
            float startD = s * 0.42f;
            float endD = s * 0.9f;

            AddQuad(vh, new Vector2(c.x - tickW, c.y + startD), new Vector2(c.x + tickW, c.y + endD));
            AddQuad(vh, new Vector2(c.x - tickW, c.y - endD), new Vector2(c.x + tickW, c.y - startD));
            AddQuad(vh, new Vector2(c.x + startD, c.y - tickW), new Vector2(c.x + endD, c.y + tickW));
            AddQuad(vh, new Vector2(c.x - endD, c.y - tickW), new Vector2(c.x - startD, c.y + tickW));
        }

        private void DrawSolidCameraView(VertexHelper vh, Vector2 c, float s)
        {
            // Main body
            AddQuad(vh, new Vector2(c.x - s * 0.75f, c.y - s * 0.5f), new Vector2(c.x + s * 0.75f, c.y + s * 0.45f));
            // Viewfinder bump
            AddQuad(vh, new Vector2(c.x - s * 0.35f, c.y + s * 0.45f), new Vector2(c.x + s * 0.35f, c.y + s * 0.7f));
        }

        private void AddQuad(VertexHelper vh, Vector2 min, Vector2 max)
        {
            int idx = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y, 0), color, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y, 0), color, Vector2.zero);
            vh.AddTriangle(idx, idx + 1, idx + 2);
            vh.AddTriangle(idx + 2, idx + 3, idx);
        }
    }
}
