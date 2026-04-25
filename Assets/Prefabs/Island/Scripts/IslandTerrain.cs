using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace Island
{
    public class IslandTerrain : MonoBehaviour
    {

        public class ChunkCollider
        {
            public Chunk Chunk { get; set; }
            public GameObject Collider { get; set; }
        }

        //Mesh
        public List<ChunkCollider> ChunkColliders = new List<ChunkCollider>();
        private List<GameObject> Parts = new List<GameObject>();
        private const string CHUNK_COLLIDER_NAME = "ChunkCollider";
        private Mesh Mesh;

        [Header("Island Configuration")]
        public int Size = 1;
        public Material GroundMaterial;
        public Material RockMaterial;

        public void Init()
        {

            //Inheritance from Draggable
            Draggable draggable = gameObject.GetComponent<Draggable>();
            draggable.HitNameFilter = CHUNK_COLLIDER_NAME;

            Chunk baseChunk = new Chunk();
            baseChunk.Radius = Size;
            baseChunk.Generate();

            //UpdateBounds();

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

        public void UpdateMesh()
        {

            //Generate Base Chunk
            if (ChunkColliders.Count == 0)
            {
                Chunk baseChunk = new Chunk();
                baseChunk.Radius = Size;
                baseChunk.Generate();

                //Mesh chunkMesh = baseChunk.Mesh;
                //Add to chunk cache list
                //ChunkColliders.Add(new ChunkCollider() { Chunk = baseChunk });
                //return chunkMesh;
            }

            //Assign Solo Chunk
            if (ChunkColliders.Count == 1)
            {
                //return ChunkColliders[0].Chunk.Mesh;
            }

            //Merge chunks
            foreach (ChunkCollider chunkCollider in ChunkColliders)
            {

            }

            // return Mesh;
        }

        private void UpdateBounds()
        {

            //Purge


            //Add Collider as Children as Unity can by default only handle 1 collider / gameobjects
            foreach (ChunkCollider chunkCollider in ChunkColliders)
            {
                GameObject collider = new GameObject(CHUNK_COLLIDER_NAME);
                collider.transform.parent = this.gameObject.transform;
                collider.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(Vector3.zero));
                BoxCollider boxCollider = collider.AddComponent<BoxCollider>();
                boxCollider.center = chunkCollider.Chunk.Bounds.center;
                boxCollider.size = chunkCollider.Chunk.Bounds.size;

                chunkCollider.Collider = collider;
            }
        }

        public GameObject GetGround()
        {
            return Parts[(int)Chunk.Part.Ground];
        }



    }
}
