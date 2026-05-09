using System;
using System.Collections.Generic;
using Island.Builder;
using UnityEngine;

namespace Utils
{
    public static class PoissonDisk
    {
        public static List<Vector2> Spawn(Rect bounds, List<Vector2> polygon, float radius, int k)
            => Spawn(bounds, Centroid(polygon), p => PointInPolygon(p, polygon), radius, k);

        public static List<Vector2> Spawn(Rect bounds, List<RadialMask> masks, float radius, int k)
            => Spawn(bounds, Centroid(masks), p => PointInCircles(p, masks), radius, k);

        // ── Core ─────────────────────────────────────────────────────────────

        private static List<Vector2> Spawn(Rect bounds, Vector2 seed, Func<Vector2, bool> contains, float radius, int k)
        {
            float       cellSize = radius / Mathf.Sqrt(2f);
            int         cols     = Mathf.CeilToInt(bounds.width  / cellSize);
            int         rows     = Mathf.CeilToInt(bounds.height / cellSize);
            Vector2?[,] grid     = new Vector2?[cols, rows];

            var result = new List<Vector2>();
            var active = new List<Vector2>();

            if (contains(seed))
                Place(seed);

            while (active.Count > 0)
            {
                int     idx    = UnityEngine.Random.Range(0, active.Count);
                Vector2 origin = active[idx];
                bool    found  = false;

                for (int attempt = 0; attempt < k; attempt++)
                {
                    float   angle     = UnityEngine.Random.value * Mathf.PI * 2f;
                    float   dist      = UnityEngine.Random.Range(radius, 2f * radius);
                    Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * dist;

                    if (!bounds.Contains(candidate)) continue;
                    if (!contains(candidate))        continue;
                    if (!GridClear(candidate))       continue;

                    Place(candidate);
                    found = true;
                    break;
                }

                if (!found) active.RemoveAt(idx);
            }

            return result;

            void Place(Vector2 p)
            {
                result.Add(p);
                active.Add(p);
                int gx = Mathf.Clamp((int)((p.x - bounds.xMin) / cellSize), 0, cols - 1);
                int gy = Mathf.Clamp((int)((p.y - bounds.yMin) / cellSize), 0, rows - 1);
                grid[gx, gy] = p;
            }

            bool GridClear(Vector2 p)
            {
                int gx = (int)((p.x - bounds.xMin) / cellSize);
                int gy = (int)((p.y - bounds.yMin) / cellSize);
                int x0 = Mathf.Max(gx - 2, 0), x1 = Mathf.Min(gx + 2, cols - 1);
                int y0 = Mathf.Max(gy - 2, 0), y1 = Mathf.Min(gy + 2, rows - 1);
                for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (grid[x, y] is Vector2 n && Vector2.Distance(p, n) < radius)
                        return false;
                return true;
            }
        }

        // ── Containment ───────────────────────────────────────────────────────

        private static bool PointInPolygon(Vector2 p, List<Vector2> poly)
        {
            bool inside = false;
            int  j      = poly.Count - 1;
            for (int i = 0; i < poly.Count; j = i++)
            {
                float xi = poly[i].x, yi = poly[i].y;
                float xj = poly[j].x, yj = poly[j].y;
                if (((yi > p.y) != (yj > p.y)) &&
                    (p.x < (xj - xi) * (p.y - yi) / (yj - yi) + xi))
                    inside = !inside;
            }
            return inside;
        }

        private static bool PointInCircles(Vector2 p, List<RadialMask> masks)
        {
            foreach (var mask in masks)
            {
                float dx = p.x - mask.Position.x;
                float dy = p.y - mask.Position.z;
                if (dx * dx + dy * dy <= mask.Radius * mask.Radius)
                    return true;
            }
            return false;
        }

        // ── Seed helpers ──────────────────────────────────────────────────────

        private static Vector2 Centroid(List<Vector2> pts)
        {
            Vector2 sum = Vector2.zero;
            foreach (var p in pts) sum += p;
            return sum / pts.Count;
        }

        private static Vector2 Centroid(List<RadialMask> masks)
        {
            Vector2 sum = Vector2.zero;
            foreach (var m in masks) sum += new Vector2(m.Position.x, m.Position.z);
            return sum / masks.Count;
        }
    }
}
