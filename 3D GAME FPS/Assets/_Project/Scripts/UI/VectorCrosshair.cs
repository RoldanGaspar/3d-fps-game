using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Procedural vector dynamic crosshair.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class VectorCrosshair : MaskableGraphic
    {
        public float gap = 12f;
        public float length = 18f;
        public float thickness = 2.8f;
        public bool showCenterDot = true;
        public float dotSize = 3.5f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 c = rectTransform.rect.center;
            float halfThick = thickness * 0.5f;

            AddQuad(vh, new Vector2(c.x - halfThick, c.y + gap), new Vector2(c.x + halfThick, c.y + gap + length), color);
            AddQuad(vh, new Vector2(c.x - halfThick, c.y - gap - length), new Vector2(c.x + halfThick, c.y - gap), color);
            AddQuad(vh, new Vector2(c.x + gap, c.y - halfThick), new Vector2(c.x + gap + length, c.y + halfThick), color);
            AddQuad(vh, new Vector2(c.x - gap - length, c.y - halfThick), new Vector2(c.x - gap, c.y + halfThick), color);

            if (showCenterDot)
            {
                float halfDot = dotSize * 0.5f;
                AddQuad(vh, new Vector2(c.x - halfDot, c.y - halfDot), new Vector2(c.x + halfDot, c.y + halfDot), color);
            }
        }

        private void AddQuad(VertexHelper vh, Vector2 min, Vector2 max, Color c)
        {
            int baseIdx = vh.currentVertCount;
            vh.AddVert(new Vector3(min.x, min.y, 0), c, Vector2.zero);
            vh.AddVert(new Vector3(min.x, max.y, 0), c, Vector2.zero);
            vh.AddVert(new Vector3(max.x, max.y, 0), c, Vector2.zero);
            vh.AddVert(new Vector3(max.x, min.y, 0), c, Vector2.zero);

            vh.AddTriangle(baseIdx, baseIdx + 1, baseIdx + 2);
            vh.AddTriangle(baseIdx + 2, baseIdx + 3, baseIdx);
        }
    }
}
