using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Island
{
    public class GroundTextureBaker : MonoBehaviour
    {
        [Header("Texture Settings")]
        public int Resolution = 1024;

        [Header("Colors")]
        public Color sandColorA = new Color(0.93f, 0.84f, 0.65f);
        public Color sandColorB = new Color(0.85f, 0.75f, 0.55f);
        public Color grassColorA = new Color(0.35f, 0.55f, 0.2f);
        public Color grassColorB = new Color(0.25f, 0.45f, 0.15f);

        [Header("Transition Settings")]
        [Range(0, 1)] public float transitionBlur = 0.1f;
        [Range(0, 1)] public float transitionNoiseAmount = 0.2f;
        public float transitionNoiseScale = 15f;
        public float grassBeginDistance = 0.5f;
        public float grassEndDistance = 0.2f;

        public Material PostProcessMat;
        public Material WhiteMat;

        private Camera bakeCamera;
        private RenderTexture islandMask;
        private int LAYER_ID = 31;

        public class ShrinkPass
        {
            public float Value;
            public RenderTexture Texture;
        }

        public void Init()
        {
            SetupBakeCamera();
        }

        private void SetupBakeCamera()
        {
            GameObject camGo = new GameObject("IslandBakeCamera");
            camGo.transform.SetParent(transform);
            // Positioned high above to look down
            camGo.transform.localPosition = new Vector3(0, 100, 0);
            camGo.transform.rotation = Quaternion.Euler(90, 0, 0);

            bakeCamera = camGo.AddComponent<Camera>();
            bakeCamera.orthographic = true;
            bakeCamera.clearFlags = CameraClearFlags.Color;
            bakeCamera.backgroundColor = Color.black;
            bakeCamera.enabled = false; // We trigger it manually
            bakeCamera.cullingMask = 1 << LAYER_ID; // Only render a specific "Baking" layer (Layer 31)
        }

        private void FitCameraToMesh(GameObject obj, float padding = 0.1f)
        {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;

            // Get the bounds in local space and scale them by the object's scale
            Bounds bounds = mf.sharedMesh.bounds;
            Vector3 size = Vector3.Scale(bounds.size, obj.transform.lossyScale);

            // Since we are looking top-down (X and Z planes)
            float width = size.x;
            float depth = size.z;

            // The camera size needs to be half of the largest dimension 
            // to fit the object perfectly in a square texture.
            float maxDim = Mathf.Max(width, depth);

            bakeCamera.orthographicSize = (maxDim / 2f) * (1.0f + padding);

            // Center the camera over the mesh bounds
            Vector3 center = obj.transform.TransformPoint(bounds.center);
            bakeCamera.transform.position = new Vector3(center.x, center.y + 100f, center.z);
        }

        public Texture2D Bake(GameObject groundObj)
        {

            FitCameraToMesh(groundObj, 0.0f);

            int downRes = Resolution / 8;
            RenderTexture rtBase = RenderTexture.GetTemporary(Resolution, Resolution, 24);
            RenderTexture rtGrassBegin = RenderTexture.GetTemporary(downRes, downRes, 24); // Divide by 2 so get blurry result
            RenderTexture rtGrassEnd = RenderTexture.GetTemporary(downRes, downRes, 24);
            RenderTexture finalRT = RenderTexture.GetTemporary(Resolution, Resolution, 0);

            //rtBase.filterMode = FilterMode.Bilinear;
            //finalRT.filterMode = FilterMode.Bilinear;

            // Store original state
            int originalLayer = groundObj.layer;
            Vector3 originalPos = groundObj.transform.position;
            Material originalMat = groundObj.GetComponent<Renderer>().sharedMaterial;
            Mesh originalMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;

            groundObj.layer = LAYER_ID;
            //groundObj.transform.position = bakeCamera.transform.position + Vector3.down * 10;

            {
                // Step 1: Print Outer Layer (Sand/Full Shape)
                bakeCamera.targetTexture = rtBase;
                // Use an unlit white material temporarily to get a clean mask
                groundObj.GetComponent<Renderer>().sharedMaterial = WhiteMat;
                bakeCamera.Render();
            }


            // Step 2: Shrink and Print Inner Layer
            {
                List<ShrinkPass> shrinkPasses = new List<ShrinkPass>(){
            new ShrinkPass(){Value = grassBeginDistance, Texture = rtGrassBegin},
            new ShrinkPass(){Value = grassEndDistance, Texture = rtGrassEnd}
        };
                foreach (var pass in shrinkPasses)
                {
                    Mesh shrunkMesh = Utils.MeshUtils.Shrink(originalMesh, pass.Value);
                    groundObj.GetComponent<MeshFilter>().sharedMesh = shrunkMesh;
                    pass.Texture.filterMode = FilterMode.Bilinear;
                    bakeCamera.targetTexture = pass.Texture;
                    bakeCamera.Render();
                }
            }

            {
                // Step 3: Post Process (Combine, Noise, Blur)
                PostProcessMat.SetTexture("_SandMask", rtBase);
                PostProcessMat.SetTexture("_GrassBegin", rtGrassBegin);
                PostProcessMat.SetTexture("_GrassEnd", rtGrassEnd);
                PostProcessMat.SetColor("_SandA", sandColorA);
                PostProcessMat.SetColor("_SandB", sandColorB);
                PostProcessMat.SetColor("_GrassA", grassColorA);
                PostProcessMat.SetColor("_GrassB", grassColorB);
                PostProcessMat.SetFloat("_Blur", transitionBlur);
                PostProcessMat.SetFloat("_NoiseAmt", transitionNoiseAmount);
                PostProcessMat.SetFloat("_NoiseScale", transitionNoiseScale);
                Graphics.Blit(rtBase, finalRT, PostProcessMat);
            }

            // Convert RT to Texture2D
            Texture2D output = new Texture2D(Resolution, Resolution, TextureFormat.RGB24, false);
            RenderTexture.active = finalRT;
            output.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
            output.Apply();


            // Cleanup
            groundObj.layer = originalLayer;
            //groundObj.transform.position = originalPos;
            groundObj.GetComponent<MeshFilter>().sharedMesh = originalMesh;
            groundObj.GetComponent<Renderer>().sharedMaterial = originalMat;
            RenderTexture.ReleaseTemporary(rtBase);
            RenderTexture.ReleaseTemporary(rtGrassBegin);
            RenderTexture.ReleaseTemporary(rtGrassEnd);
            RenderTexture.ReleaseTemporary(finalRT);

            return output;
        }

    }

}
