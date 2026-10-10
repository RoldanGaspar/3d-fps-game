using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Procedural hit marker: four diagonal ticks around the crosshair centre.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class VectorHitMarker : MaskableGraphic
    {
        public float gap = 9f;
        public float length = 12f;
        public float thickness = 3f;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Vector2 c = rectTransform.rect.center;
            float half = thickness * 0.5f;

            // Four diagonal ticks (NE, NW, SW, SE)
            AddTick(vh, c, new Vector2(1f, 1f).normalized, half);
            AddTick(vh, c, new Vector2(-1f, 1f).normalized, half);
            AddTick(vh, c, new Vector2(-1f, -1f).normalized, half);
            AddTick(vh, c, new Vector2(1f, -1f).normalized, half);
        }

        private void AddTick(VertexHelper vh, Vector2 center, Vector2 dir, float half)
        {
            Vector2 perp = new Vector2(-dir.y, dir.x) * half;
            Vector2 a = center + dir * gap;
            Vector2 b = center + dir * (gap + length);

            int i = vh.currentVertCount;
            vh.AddVert(a - perp, color, Vector2.zero);
            vh.AddVert(a + perp, color, Vector2.zero);
            vh.AddVert(b + perp, color, Vector2.zero);
            vh.AddVert(b - perp, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i + 2, i + 3, i);
        }
    }
}
