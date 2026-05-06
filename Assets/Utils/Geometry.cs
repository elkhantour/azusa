using UnityEngine;

namespace Utils
{

    public class Point
    {
        public int Index { get; set; }
        public Vector3 Position { get; set; }
    }


    public class Edge
    {
        public Point Begin { get; set; }
        public Point End { get; set; }
    }

}
