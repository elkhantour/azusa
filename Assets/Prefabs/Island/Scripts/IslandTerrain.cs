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
        private const string CHUNK_COLLIDER_NAME = "ChunkCollider";
        private Mesh Mesh;

        [Header("Island Configuration")]
        public int Size = 1;
        public Material GroundMaterial;
        public Material RockMaterial;

        public void Init()
        {
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<MeshFilter>();

            //Add Materials
            gameObject.GetComponent<MeshRenderer>().materials = new Material[] {
        GroundMaterial,
    RockMaterial,
    RockMaterial,
    RockMaterial
        };

            //Inheritance from Draggable
            Draggable draggable = gameObject.GetComponent<Draggable>();
            draggable.HitNameFilter = CHUNK_COLLIDER_NAME;

            gameObject.GetComponent<MeshFilter>().mesh = UpdateMesh();
            UpdateBounds();

        }

        public Mesh UpdateMesh()
        {

            //Generate Base Chunk
            if (ChunkColliders.Count == 0)
            {
                Chunk baseChunk = new Chunk();
                baseChunk.radius = Size;
                Mesh chunkMesh = baseChunk.Mesh;

                //Add to chunk cache list
                ChunkColliders.Add(new ChunkCollider() { Chunk = baseChunk });
                return chunkMesh;
            }

            //Assign Solo Chunk
            if (ChunkColliders.Count == 1)
            {
                return ChunkColliders[0].Chunk.Mesh;
            }

            //Merge chunks
            foreach (ChunkCollider chunkCollider in ChunkColliders)
            {

            }

            return Mesh;
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

        public Mesh GetGround()
        {
            return ChunkColliders[0].Chunk.Circles.Where(c => c.name == "ground").First().Mesh;
        }



    }
}
