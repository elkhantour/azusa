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


            List<FloraMask> floraMask = new List<FloraMask>();

            // Generate Towns
            foreach (var town in NomadTowns)
            {
                town.Init();

		// Create flora masks for each town, so the plant don't spawn inside the radius
                floraMask.Add(new FloraMask()
                {
                    Radius = town.GetOuterRadius(),
                    Position = town.GetPosition(),
                });
            }


            // Generate Flora
            Mesh ground = Terrain.GetGround();
            if (ground != null)
            {
                Flora.Init(ground);
                Flora.Parent = gameObject;
                Flora.Generate(floraMask);
            }
        }


    }
}

