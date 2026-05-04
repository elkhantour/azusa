using UnityEngine;
using System.Collections.Generic;

namespace Island
{
    public class MarchingSquaresOutline
    {
        private float _gridSize;
        private float _isoValue = 0.5f;

        public MarchingSquaresOutline(float gridSize = 1.0f)
        {
            _gridSize = gridSize;
        }

        /// <summary>
        /// Converts a list of circles into a line-topology mesh.
        /// </summary>
        public Mesh GenerateOutline(List<RadialMask> circles, Rect bounds)
        {
            int width = Mathf.CeilToInt(bounds.width / _gridSize);
            int height = Mathf.CeilToInt(bounds.height / _gridSize);
            float[,] field = new float[width + 1, height + 1];

            // 1. Sample Scalar Field
            for (int x = 0; x <= width; x++)
            {
                for (int y = 0; y <= height; y++)
                {
                    Vector2 worldPos = new Vector2(bounds.xMin + x * _gridSize, bounds.yMin + y * _gridSize);
                    float maxDensity = 0;

                    foreach (var circle in circles)
                    {
                        float dist = Vector2.Distance(worldPos, new Vector2(circle.Position.x, circle.Position.z));
                        float d = 1.0f - Mathf.Clamp01((dist - (circle.Radius - 0.5f)) / 1.0f);
                        maxDensity = Mathf.Max(maxDensity, d);
                    }
                    field[x, y] = maxDensity;
                }
            }

            // 2. Marching Squares Logic
            List<Vector3> vertices = new List<Vector3>();
            List<int> indices = new List<int>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    ProcessCell(x, y, field, bounds, vertices, indices);
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = "MarchingOutline_Lines";
            mesh.SetVertices(vertices);
            // Using MeshTopology.Lines for simple vertex pairs
            mesh.SetIndices(indices.ToArray(), MeshTopology.Lines, 0);
            mesh.RecalculateBounds();

            return mesh;
        }

        private void ProcessCell(int x, int y, float[,] field, Rect bounds, List<Vector3> verts, List<int> indices)
        {
            float v0 = field[x, y];         // BL
            float v1 = field[x + 1, y];     // BR
            float v2 = field[x + 1, y + 1]; // TR
            float v3 = field[x, y + 1];     // TL

            int config = 0;
            if (v0 >= _isoValue) config |= 1;
            if (v1 >= _isoValue) config |= 2;
            if (v2 >= _isoValue) config |= 4;
            if (v3 >= _isoValue) config |= 8;

            if (config == 0 || config == 15) return;

            Vector3 pL = LerpEdge(x, y, x, y + 1, v0, v3, bounds);
            Vector3 pR = LerpEdge(x + 1, y, x + 1, y + 1, v1, v2, bounds);
            Vector3 pB = LerpEdge(x, y, x + 1, y, v0, v1, bounds);
            Vector3 pT = LerpEdge(x, y + 1, x + 1, y + 1, v3, v2, bounds);

            GenerateEdges(config, pL, pR, pB, pT, verts, indices);
        }

        private void GenerateEdges(int config, Vector3 pL, Vector3 pR, Vector3 pB, Vector3 pT, List<Vector3> verts, List<int> indices)
        {
            switch (config)
            {
                case 1: case 14: AddLine(pL, pB, verts, indices); break;
                case 2: case 13: AddLine(pB, pR, verts, indices); break;
                case 4: case 11: AddLine(pR, pT, verts, indices); break;
                case 8: case 7:  AddLine(pT, pL, verts, indices); break;
                case 3: case 12: AddLine(pL, pR, verts, indices); break;
                case 6: case 9:  AddLine(pB, pT, verts, indices); break;
                case 5: 
                    AddLine(pL, pT, verts, indices); 
                    AddLine(pB, pR, verts, indices); 
                    break;
                case 10: 
                    AddLine(pL, pB, verts, indices); 
                    AddLine(pT, pR, verts, indices); 
                    break;
            }
        }

        private void AddLine(Vector3 a, Vector3 b, List<Vector3> verts, List<int> indices)
        {
            int start = verts.Count;
            verts.Add(a);
            verts.Add(b);

            indices.Add(start);
            indices.Add(start + 1);
        }

        private Vector3 LerpEdge(int x1, int y1, int x2, int y2, float val1, float val2, Rect bounds)
        {
            float t = (_isoValue - val1) / (val2 - val1);
            float posX = Mathf.Lerp(x1, x2, t) * _gridSize + bounds.xMin;
            float posZ = Mathf.Lerp(y1, y2, t) * _gridSize + bounds.yMin;
            return new Vector3(posX, 0.2f, posZ); 
        }
    }
}
