using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Triangulation;
using Island.Builder;

namespace Island
{

    public enum FloraType
    {
        Chaos, //Jungle type, messes everywhere
        Centric,
        Plain, //++ grass &fields, Tree cluster
        Arid, //++ rock, --grass, --tree
    }


    [Serializable]
    public class FloraElement
    {
        public string Name;
        public GameObject Mesh;
        public float Density = 0.2f;
        public float BaseSize = 1.0f;
        public float MinSize = 1.0f;
        public float MaxSize = 1.0f;
        [Tooltip("The Minimum distance allowed between two entities")]
        public float Radius = 0.0f;
    }


    /*
     * Generate a flora within a certain area
     * Takes in a list of gameobject along with respective density
     * And spread procedurally the vegetation depending on this density
     * Note that biotope only handle the vegetation part of the biome
     */
    [Serializable]
    public class Flora : MonoBehaviour
    {

        // The parent Game Object in which the vegetals with be spawn under
        public GameObject Parent { get; set; }

        public FloraType type = FloraType.Chaos;
        // The margin between the area edges and the actuall flora
        public int DistanceFromEdge = 1;
        // TODO: explain usage
        public int AreaVertices = 30;
        // Enable debug draw boundboxes
        public bool DebugMode = false;
        public int Fertility = 1000; // Total points
        public float CellSize = 1.0f;
        public bool DebugCell = false;

        [SerializeField]

        // To avoid clamping, We need to create a hierarchy for our flora elements. 
        public List<FloraElement> BigElements = new List<FloraElement>();
        public List<FloraElement> MediumElements = new List<FloraElement>();
        public List<FloraElement> SmallElements = new List<FloraElement>();

        private GameObject _area;
        private GameObject _wrapper;

        private void DrawDebug(List<FloraElement> enumItems)
        {
            Color[] colors = new Color[] {
                Color.blue,
                Color.red,
                Color.white,
                Color.magenta,
                Color.yellow,
                Color.cyan
            };


            for (int i = 0; i < enumItems.Count; i++)
            {

                Debugger.Cube(new Cube()
                {
                    Position = enumItems[i].Mesh.transform.position,
                    Size = new Vector3(0.3f, 0.3f, 0.3f),
                    Color = colors[i % colors.Length]
                });

            }
        }

        private static bool IsInsideMask(Vector3 position, List<RadialMask> masks, float exclusionBuffer = 2.0f)
        {
            foreach (var mask in masks)
            {
                // Get the radius of the outer-most ring
                float distanceToMask = Vector3.Distance(position, mask.Position);

                if (distanceToMask < (mask.Radius + exclusionBuffer))
                {
                    return true;
                }
            }

            return false;

        }

        private void DrawElement(FloraElement element, Vector3 position)
        {
            GameObject instance = GameObject.Instantiate(element.Mesh, position, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0));
            instance.transform.localScale = Vector3.one * element.BaseSize * UnityEngine.Random.Range(element.MinSize, element.MaxSize);
            if (_wrapper != null) instance.transform.SetParent(_wrapper.transform);
        }

        private void DrawDebugCell(Vector3 position)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.position = new Vector3(position.x, 0.3f, position.z);
            quad.transform.rotation = Quaternion.Euler(90, 0, 0); // lay flat
            quad.transform.localScale = Vector3.one * CellSize;
        }

        public void Generate(GameObject area, GameObject parent = null, List<RadialMask> masks = null)
        {
            if (Fertility == 0) return;

            if (_wrapper) Clear();

            _area = area;
            _wrapper = new GameObject("Flora");
            if (parent) _wrapper.transform.SetParent(parent.transform);

            // We need the raw grid data to know which point belongs to which cell
            Dictionary<Vector2Int, Vector3> gridData = JitteredGrid.Spawn(_area, DistanceFromEdge, 1.0f, 0.4f, 0.5f, "Ground");
            List<Vector2Int> availableCoords = gridData.Keys.ToList();

            List<FloraElement>[] spawnOrder = new List<FloraElement>[3]{
            BigElements.ToList(),
            MediumElements.ToList(),
            SmallElements.ToList(),
        };

            float sumDensity = 0.0f;
            Array.ForEach(spawnOrder, list => list.ForEach(item => sumDensity += item.Density));

            foreach (List<FloraElement> list in spawnOrder)
            {
                foreach (FloraElement item in list)
                {
                    float densityToNumber = Mathf.FloorToInt(item.Density * gridData.Count / sumDensity);

                    for (int n = 0; n < densityToNumber; n++)
                    {
                        if (availableCoords.Count == 0) break;

                        int randomIndex = UnityEngine.Random.Range(0, availableCoords.Count);
                        Vector2Int coord = availableCoords[randomIndex];

                        // Mask check
                        if (masks != null && IsInsideMask(gridData[coord], masks))
                        {
                            availableCoords.RemoveAt(randomIndex);
                            n--; continue;
                        }

                        // Standard instantiation (adjust rotation and size)
                        DrawElement(item, gridData[coord]);

                        // Grid-based removal
                        int range = Mathf.CeilToInt(item.Radius / CellSize);

                        // Remove neighbors within the "X" pattern
                        availableCoords.RemoveAll(c =>
                            c.x >= coord.x - range && c.x <= coord.x + range &&
                            c.y >= coord.y - range && c.y <= coord.y + range
                        );

                        if (DebugCell)
                            DrawDebugCell(gridData[coord]);


                    }
                }
            }

        }

        public void Clear()
        {
            Destroy(_wrapper);
            _wrapper = null;
        }


    }

}
