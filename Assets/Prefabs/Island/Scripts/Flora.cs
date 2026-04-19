using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using Triangulation;

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
        public float Density = 0.2f;
        public float BaseSize = 1.0f;
        public float MinSize = 1.0f;
        public float MaxSize = 1.0f;
        public GameObject Mesh;
    }


    /*
     * Generate a flora within a certain area
     * Takes in a list of gameobject along with respective density
     * And spread procedurally the vegetation depending on this density
     * Note that biotope only handle the vegetation part of the biome
     */
    [Serializable]
    public class Flora
    {

        // The parent Game Object in which the vegetals with be spawn under
        public GameObject Parent;

        // The margin between the area edges and the actuall flora
        public float DistanceFromEdge = 1.0f;
        // TODO ??
        public int AreaVertices = 30;
        // Enable debug draw boundboxes
        public bool DebugMode = false;
        public int Fertility = 1000; // Total points

        [SerializeField]
        public List<FloraElement> Elements = new List<FloraElement>();

        private Mesh _area;

        public void Init(Mesh area)
        {
            _area = area;

            if (DistanceFromEdge > 0)
            {
                _area = ShrinkArea(DistanceFromEdge);
            }
        }

        public FloraElement[] Generate(FloraType type = FloraType.Chaos)
        {

            SpreadPoints spread = new SpreadPoints(_area, Fertility);
            List<Vector3> points = spread.Points.ToList();
            List<int> availableIndex = Enumerable.Range(0, points.Count - 1).ToList();
            List<FloraElement> enumItems = Elements.ToList();

            float sumDensity = 0.0f;
            enumItems.ForEach(item => sumDensity += item.Density);

            //adding points randomly in function of density
            foreach (FloraElement item in enumItems)
            {
                float densityToNumber = Mathf.FloorToInt(item.Density * points.Count / sumDensity);
                List<Vector3> positions = new List<Vector3>();

                for (int n = 0; n < densityToNumber; n++)
                {
                    int randomIndex = UnityEngine.Random.Range(0, availableIndex.Count - 1);
                    Vector3 position = points[randomIndex];

                    //Add to item array
                    positions.Add(position);

                    //Remove selected indexes from available Index
                    availableIndex.RemoveAt(randomIndex);

                    // Spawn with random Y rotation for organic look
                    Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);

                    GameObject instance = GameObject.Instantiate(item.Mesh, position, rotation);

                    // Randomize the Size
                    float minScale = 0.8f;
                    float maxScale = 1.2f;
                    float randomScale = UnityEngine.Random.Range(minScale, maxScale);
                    instance.transform.localScale = Vector3.one * randomScale;

                    if (Parent != null) instance.transform.SetParent(Parent.transform);
                }
            }


            //Debug
            if (DebugMode)
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


            return enumItems.ToArray();
        }

        private Mesh ShrinkArea(float value)
        {

            //Downsample and simplify shape
            Mesh shrinkMesh = MeshUtils.DownSample(_area, AreaVertices);
            shrinkMesh = MeshUtils.Shrink(shrinkMesh, value);

            //Lost triangulation while downsampling
            Triangulator triangulator = new Triangulator(MeshUtils.ToVector2(shrinkMesh.vertices));

            return triangulator.Mesh;
        }
    }

}
