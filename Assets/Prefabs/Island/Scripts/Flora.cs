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
        public string Name;
        public GameObject Mesh;
        public float Density = 0.2f;
        public float BaseSize = 1.0f;
        public float MinSize = 1.0f;
        public float MaxSize = 1.0f;
        [Tooltip("The Minimum distance allowed between two entities")]
        public float Radius = 0.0f;
    }


    public class FloraMask
    {
        public float Radius;
        public Vector3 Position;
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

        [SerializeField]
        /**
         * To avoid clamping, We need to create a hierarchy for our flora elements. 
         */
        public List<FloraElement> BigElements = new List<FloraElement>();
        public List<FloraElement> MediumElements = new List<FloraElement>();
        public List<FloraElement> SmallElements = new List<FloraElement>();

        private GameObject _area;

        public void Init(GameObject area)
        {
            _area = area;
        }

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

        private bool IsInsideMask(Vector3 position, List<FloraMask> masks, float exclusionBuffer = 2.0f)
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

        public FloraElement[] Generate(List<FloraMask> masks = null)
        {

            if (Fertility == 0)
                return new FloraElement[0];


            // Generate random points (aka spread points)
            PointDistributor spread = new PointDistributor(_area, DistanceFromEdge);
            List<Vector3> points = spread.GetRandomPoints(Fertility);

            List<int> availableIndex = Enumerable.Range(0, points.Count - 1).ToList();
            List<FloraElement> enumItems = SmallElements.ToList();

            float sumDensity = 0.0f;
            enumItems.ForEach(item => sumDensity += item.Density);

            // Adding points randomly in function of density
            foreach (FloraElement item in enumItems)
            {
                float densityToNumber = Mathf.FloorToInt(item.Density * points.Count / sumDensity);

                for (int n = 0; n < densityToNumber; n++)
                {
                    if (availableIndex.Count == 0) break;

                    // Pick a random point in the available index list
                    int randomIndexLocation = UnityEngine.Random.Range(0, availableIndex.Count);
                    int pointIndex = availableIndex[randomIndexLocation];
                    Vector3 position = points[pointIndex];

                    // If it's in a mask, skip this point but DON'T increment 'n' 
                    // so we try to find a different valid spot for this item.
                    if (IsInsideMask(position, masks))
                    {
                        availableIndex.RemoveAt(randomIndexLocation);
                        n--; // Retry this "count" with a new random index
                        continue;
                    }

                    // Remove index so it's not picked again
                    availableIndex.RemoveAt(randomIndexLocation);

                    // Standard instantiation logic...
                    Quaternion rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0);
                    GameObject instance = GameObject.Instantiate(item.Mesh, position, rotation);

                    float randomScale = UnityEngine.Random.Range(item.MinSize, item.MaxSize);
                    instance.transform.localScale = Vector3.one * item.BaseSize * randomScale;

                    if (Parent != null) instance.transform.SetParent(Parent.transform);
                }
            }


            //Debug
            if (DebugMode)
            {
                DrawDebug(enumItems);
            }


            return enumItems.ToArray();
        }



    }

}
