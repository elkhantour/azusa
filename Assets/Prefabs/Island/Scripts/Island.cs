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
        public Flora Flora;

        [Header("Habitants")]
        [SerializeField]
        private List<NomadTownGenerator> NomadTowns = new List<NomadTownGenerator>();
        private List<RadialMask> nomadTownMask;

        [SerializeField] private Material groundMaterial;
        [SerializeField] private Material rootMaterial;
        [SerializeField] private GroundTextureBaker GroundTextureBaker;
	
        [System.NonSerialized] public GameObject Parent;
        [System.NonSerialized] public GameObject Ground;
        [System.NonSerialized] public GameObject Root;

	
        private void GenerateNomadTowns()
        {
            nomadTownMask = new List<RadialMask>();
            // Generate Towns
            foreach (var town in NomadTowns)
            {
                town.Init();

                // Create flora masks for each town, so the plant don't spawn inside the radius
                nomadTownMask.Add(new RadialMask()
                {
                    Radius = town.GetOuterRadius(),
                    Position = town.GetPosition(),
                });
            }

        }

        private GameObject CreateGameObject(string name, Material material = null, GameObject parent = null)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<MeshFilter>();
            MeshRenderer rd = go.AddComponent<MeshRenderer>();

            if (material)
            {
                rd.material = material;
            }

            if (parent)
            {
                go.transform.SetParent(parent.transform);
            }

            return go;
        }

        private void GenerateFlora(List<RadialMask> townMask)
        {
            if (Ground != null)
            {
                // Generate Flora
                Flora.Init(Ground);
                Flora.Parent = Parent;
                Flora.Generate(nomadTownMask);
            }
        }

        public void Init()
        {
            Parent = gameObject;
            Ground = CreateGameObject("Ground", groundMaterial, Parent);
            Root = CreateGameObject("Root", rootMaterial, Parent);
        }

        public void BakeTexture()
        {
            GroundTextureBaker.Init();
            Texture2D texture = GroundTextureBaker.Bake(Ground, nomadTownMask);
            MeshRenderer groundRenderer = Ground.GetComponent<MeshRenderer>();
            groundRenderer.material.SetTexture("_BaseMap", texture);
        }

        public void GenerateFlora()
        {
            GenerateFlora(nomadTownMask);
        }

        public Mesh GetGroundMesh()
        {
            return Ground.GetComponent<MeshFilter>().mesh;
        }

        public Mesh GetRootMesh()
        {
            return Root.GetComponent<MeshFilter>().mesh;
        }

        public void SetGroundMesh(Mesh mesh)
        {
            Ground.GetComponent<MeshFilter>().mesh = mesh;
        }

        public void SetRootMesh(Mesh mesh)
        {
            Root.GetComponent<MeshFilter>().mesh = mesh;
        }


    }
}

