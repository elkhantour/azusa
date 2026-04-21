using UnityEngine;
using System.Collections.Generic;

/*
 Islands are a gathering of chunks
 This script is used as the main orchestrator for the Island environment.
 It handles the nomads town generation, the flaura and fauna spawning. 
 */
namespace Island
{

    public class Island : MonoBehaviour
    {

        [Header("Environment")]
        public IslandTerrain Terrain;
        public Flora Flora;

        [Header("Habitants")]
        public List<NomadTownGenerator> NomadTowns = new List<NomadTownGenerator>();


        private void Start()
        {

            Terrain.Init();

            foreach (var town in NomadTowns)
            {
                town.Init();
            }


            Mesh ground = Terrain.GetGround();

            if (ground != null)
            {
                GenerateVegetation(ground);
            }
        }


        private void GenerateVegetation(Mesh ground)
        {

            //Generate Vegetation

            //Vegetation Config
            if (ground)
            {
                Flora.Init(ground);
                Flora.Parent = gameObject;
                Flora.Generate();
            }

        }


    }
}

