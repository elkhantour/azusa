using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Island
{
    public class GroundTextureBaker : MonoBehaviour
    {
        [Header("Texture Settings")]
        public int Resolution = 1024;
        public int ResolutionSmall = 128;

        [Header("Colors")]
        public Color sandColorA = new Color(0.93f, 0.84f, 0.65f);
        public Color sandColorB = new Color(0.85f, 0.75f, 0.55f);
        public Color grassColorA = new Color(0.35f, 0.55f, 0.2f);
        public Color grassColorB = new Color(0.25f, 0.45f, 0.15f);
        public Color townColorA = new Color(0.72f, 0.38f, 0.24f);
        public Color townColorB = new Color(0.58f, 0.28f, 0.16f);
        public Texture2D grassPattern;
        public float grassPatternSize = 10.0f;

        [Header("Transition Settings")]
        [Range(0, 1)]
        public float transitionBlur = 0.1f;
        [Range(0, 1)]
        public float transitionNoiseAmount = 0.2f;
        public float transitionNoiseScale = 15f;
        public float grassBeginDistance = 0.5f;
        public float grassEndDistance = 0.2f;

        public Material PostProcessMat;
        public Material WhiteMat;

        private Camera bakeCamera;
        private RenderTexture islandMask;
        private int LAYER_ID = 31;

        public class Tex
        {
            public RenderTexture texture;
            public string reference;
            public int resolution;
            public int format;
        }

        public enum TmpTexType
        {
            SandMask,
            GrassMaskBegin,
            GrassMaskEnd,
            TownMaskBegin,
            TownMaskEnd,
            FinalTexture,
            COUNT,
        }

        private List<Tex> _tmpTex = null;

        public class ShrinkPass
        {
            public float Value;
            public RenderTexture Texture;
        }

        public void Init()
        {
            SetupTmpTextures();
            SetupBakeCamera();
        }

        private void SetupTmpTextures()
        {
            ;

            _tmpTex = new()
            {
                new()
                {
                    reference = "_SandMask",
                    resolution = Resolution,
                    format = 24
                },
                new()
                {
                    reference = "_GrassBegin",
                    resolution = ResolutionSmall,
                    format = 24
                },
                new()
                {
                    reference = "_GrassEnd",
                    resolution = ResolutionSmall,
                    format = 24
                },
                new()
                {
                    reference = "_TownBegin",
                    resolution = ResolutionSmall,
                    format = 24
                },
                new()
                {
                    reference = "_TownEnd",
                    resolution = Resolution,
                    format = 24
                },
                new()
                {
                    reference = "_FinalTexture",
                    resolution = Resolution,
                    format = 0
                },
            };

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
            bakeCamera.cullingMask =
                1 << LAYER_ID; // Only render a specific "Baking" layer (Layer 31)
        }

        private void FitCameraToMesh(GameObject obj, float padding = 0.1f)
        {
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null)
                return;

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
            bakeCamera.transform.position =
                new Vector3(center.x, center.y + 100f, center.z);
        }

        private RenderTexture TmpTex(TmpTexType type)
        {
            return _tmpTex[(int)type].texture;
        }

        private void MapShaderParameters()
        {

            _tmpTex.ForEach(t => PostProcessMat.SetTexture(t.reference, t.texture));
            PostProcessMat.SetTexture("_GrassPattern", grassPattern);

            PostProcessMat.SetColor("_SandA", sandColorA);
            PostProcessMat.SetColor("_SandB", sandColorB);
            PostProcessMat.SetColor("_GrassA", grassColorA);
            PostProcessMat.SetColor("_GrassB", grassColorB);
            PostProcessMat.SetColor("_TownA", townColorA);
            PostProcessMat.SetColor("_TownB", townColorB);

            PostProcessMat.SetFloat("_GrassPatternSize", grassPatternSize);
            PostProcessMat.SetFloat("_Blur", transitionBlur);
            PostProcessMat.SetFloat("_NoiseAmt", transitionNoiseAmount);
            PostProcessMat.SetFloat("_NoiseScale", transitionNoiseScale);
        }

        private Texture2D BakeGround(GameObject groundObj)
        {
            return Texture2D.whiteTexture;
        }

        private Texture2D BakeTowns(GameObject groundObj)
        {
            return Texture2D.whiteTexture;
        }

        public Texture2D Bake(GameObject groundObj)
        {

            FitCameraToMesh(groundObj, 0.0f);

            _tmpTex.ForEach(t => t.texture = RenderTexture.GetTemporary(t.resolution, t.resolution, t.format));

            // Store original state
            int originalLayer = groundObj.layer;
            Vector3 originalPos = groundObj.transform.position;
            Material originalMat = groundObj.GetComponent<Renderer>().sharedMaterial;
            Mesh originalMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;

            groundObj.layer = LAYER_ID;
            // groundObj.transform.position = bakeCamera.transform.position +
            // Vector3.down * 10;

            {
                // Step 1: Print Outer Layer (Sand/Full Shape)
                bakeCamera.targetTexture = TmpTex(TmpTexType.SandMask);
                // Use an unlit white material temporarily to get a clean mask
                groundObj.GetComponent<Renderer>().sharedMaterial = WhiteMat;
                bakeCamera.Render();
            }

            // Step 2: Shrink and Print Inner Layer
            {

                List<ShrinkPass> shrinkPasses = new List<ShrinkPass>()
        {
        new ShrinkPass()
        {
        Value = grassBeginDistance,
        Texture = TmpTex(TmpTexType.GrassMaskBegin)
        },
        new ShrinkPass()
        {
        Value = grassEndDistance,
        Texture = TmpTex(TmpTexType.GrassMaskEnd)
        }
    };
                Mesh shrunkMesh = Utils.MeshUtils.Clone(originalMesh);
                foreach (var pass in shrinkPasses)
                {
                    Utils.MeshUtils.Shrink(shrunkMesh, pass.Value);
                    groundObj.GetComponent<MeshFilter>().sharedMesh = shrunkMesh;
                    pass.Texture.filterMode = FilterMode.Bilinear;
                    bakeCamera.targetTexture = pass.Texture;
                    bakeCamera.Render();
                }
            }

            MapShaderParameters();
            Graphics.Blit(TmpTex(TmpTexType.SandMask), TmpTex(TmpTexType.FinalTexture), PostProcessMat);

            // Convert RT to Texture2D
            Texture2D output =
                new Texture2D(Resolution, Resolution, TextureFormat.RGB24, false);
            RenderTexture.active = TmpTex(TmpTexType.FinalTexture);
            output.ReadPixels(new Rect(0, 0, Resolution, Resolution), 0, 0);
            output.Apply();



            {
                // Cleanup
                groundObj.layer = originalLayer;
                // groundObj.transform.position = originalPos;
                groundObj.GetComponent<MeshFilter>().sharedMesh = originalMesh;
                groundObj.GetComponent<Renderer>().sharedMaterial = originalMat;
                _tmpTex.ForEach(t => RenderTexture.ReleaseTemporary(t.texture));
            }

            return output;
        }
    }

}
