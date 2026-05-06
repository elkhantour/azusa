using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace Island
{
    public class IslandTerrain : MonoBehaviour
    {

        //Mesh
        private List<GameObject> Parts = new List<GameObject>();
        private Mesh Mesh;

        [Header("Island Configuration")]
        public int Size = 1;
        public Material GroundMaterial;
        public Material RockMaterial;
        public GroundTextureBaker GroundTextureBaker;

        public void Init() { }

	// DELETEME ?
        public void GenerateDebugChunk()
        {
            Chunk baseChunk = new Chunk();
            baseChunk.Radius = Size;
            baseChunk.Generate();

            // Create Gameobject and filter for each island parts
            // TODO: investigate why c# doesn't like [(int)ChunkPart.Ground] = ...
            Material[] materialsMap = new Material[(int)Chunk.Part.COUNT] { GroundMaterial, RockMaterial };

            for (int i = 0; i < (int)Chunk.Part.COUNT; i++)
            {
                string name = ((Chunk.Part)i).ToString();
                GameObject part = new GameObject(name);
                part.transform.SetParent(this.transform, false);

                MeshFilter mf = part.AddComponent<MeshFilter>();
                MeshRenderer mr = part.AddComponent<MeshRenderer>();

                mf.mesh = baseChunk.GetPartMesh((Chunk.Part)i);
                mr.material = materialsMap[i];

                part.AddComponent<MeshCollider>();

                // cache the gameobjects for future retrieval
                Parts.Add(part);
            }
        }

        public void BakeTexture(List<RadialMask> townMasks = null)
        {
            GroundTextureBaker.Init();
            GameObject ground = GetGround();
            Texture2D texture = GroundTextureBaker.Bake(ground, townMasks);
            MeshRenderer groundRenderer = ground.GetComponent<MeshRenderer>();
            groundRenderer.material.SetTexture("_BaseMap", texture);
        }

        public GameObject GetGround()
        {
            return Parts[(int)Chunk.Part.Ground];
        }



    }
}
