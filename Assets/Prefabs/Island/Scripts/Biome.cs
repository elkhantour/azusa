using UnityEngine;
using System;

namespace Island
{
    /**
       A Biome is what defines an Island atmosphere and overall environment.
       A Biome may be constitued of various characteristics that shape the overall island look such as:
       - Flora
       - Fauna
       - Ground
       - Outter belt
     */
    [System.Serializable]
    public class Biome : MonoBehaviour
    {
        //Materials
        public Material GroundMaterial;
        public Material RockMaterial;

        //Vegetation
        public Flora Flora;

        public void Init(Mesh area)
        {
	    Flora.Init(area);
        }


    }
}
