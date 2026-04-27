using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Island
{
    public class GroundTextureBaker : MonoBehaviour
    {

        [System.Serializable]
        public class GroundTextureArea
        {
            public string Name;
            public Texture2D Base = null;
            public float BaseScale = 5.0f;
            public Texture2D Pattern = null;
            public float PatternScale = 10.0f;
            public Color ColorA = Color.black;
            public Color ColorB = Color.black;
        }

        [Header("Texture Settings")]
        public int Resolution = 1024;
        public int ResolutionSmall = 128;

        [Header("Sand")]
        [SerializeField]
        public GroundTextureArea Sand = new GroundTextureArea()
        {
            Name = "Sand",
            ColorA = new Color(0.93f, 0.84f, 0.65f),
            ColorB = new Color(0.85f, 0.75f, 0.55f),
        };
        [Header("Grass")]
        [SerializeField]
        public GroundTextureArea Grass = new GroundTextureArea()
        {
            Name = "Grass",
            ColorA = new Color(0.93f, 0.84f, 0.65f),
            ColorB = new Color(0.25f, 0.45f, 0.15f),
        };

        [Header("Town")]
        [SerializeField]
        public GroundTextureArea Town = new GroundTextureArea()
        {
            Name = "Town",
            ColorA = new Color(0.72f, 0.38f, 0.24f),
            ColorB = new Color(0.58f, 0.28f, 0.16f),
        };

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
        private int GROUND_LAYER_ID = 30;
        private int TOWN_LAYER_ID = 31;

        public enum AreaType
        {
            Sand,
            Grass,
            Town,
            COUNT
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

        public class Tex
        {
            public RenderTexture texture;
            public string reference;
            public int resolution;
            public int format;
        }


        private List<Tex> _tmpTex = null;
        private List<GroundTextureArea> _areas = null;

        public class ShrinkPass
        {
            public float Value;
            public RenderTexture Texture;
        }

        public void Init()
        {
            SetupTmpTextures();
            SetupAreas();
            SetupBakeCamera();
        }

        private void SetupAreas()
        {
            _areas = new()
            {
                Sand,
                Grass,
                Town,
            };

            // Set default base and pattern to black if base or pattern textures are null
            _areas.ForEach(a =>
            {
                if (a.Base == null) a.Base = Texture2D.blackTexture;
                if (a.Pattern == null) a.Pattern = Texture2D.blackTexture;
            });

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
            // We trigger it manually
            bakeCamera.enabled = false;
            // Only render a specific "Baking" layer (Layer 31)
            bakeCamera.cullingMask = 1 << GROUND_LAYER_ID;
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

            // Sand Attributes
            {
                PostProcessMat.SetColor("_SandTintA", Sand.ColorA);
                PostProcessMat.SetColor("_SandTintB", Sand.ColorB);
                PostProcessMat.SetTexture("_SandPattern", Sand.Pattern);
                PostProcessMat.SetTexture("_SandBase", Sand.Base);
                PostProcessMat.SetFloat("_SandBaseScale", Sand.BaseScale);
                PostProcessMat.SetFloat("_SandPatternScale", Sand.PatternScale);
            }

            // Grass Attributes
            {
                PostProcessMat.SetColor("_GrassTintA", Grass.ColorA);
                PostProcessMat.SetColor("_GrassTintB", Grass.ColorB);
                PostProcessMat.SetTexture("_GrassPattern", Grass.Pattern);
                PostProcessMat.SetTexture("_GrassBase", Grass.Base);
                PostProcessMat.SetFloat("_GrassPatternScale", Grass.PatternScale);
                PostProcessMat.SetFloat("_GrassBaseScale", Grass.BaseScale);
            }

            // Town Attributes
            {
                PostProcessMat.SetColor("_TownTintA", Town.ColorA);
                PostProcessMat.SetColor("_TownTintB", Town.ColorB);
                PostProcessMat.SetTexture("_TownPattern", Town.Pattern);
                PostProcessMat.SetTexture("_TownBase", Town.Base);
                PostProcessMat.SetFloat("_TownPatternScale", Town.PatternScale);
                PostProcessMat.SetFloat("_TownBaseScale", Town.BaseScale);
            }

            // Global Attributes
            {
                PostProcessMat.SetFloat("_Blur", transitionBlur);
                PostProcessMat.SetFloat("_NoiseAmt", transitionNoiseAmount);
                PostProcessMat.SetFloat("_NoiseScale", transitionNoiseScale);
            }
        }

        private void RenderGround(GameObject groundObj)
        {

            {
                // Step 1: Print Outer Layer (Sand/Full Shape)
                bakeCamera.targetTexture = TmpTex(TmpTexType.SandMask);
                // Use an unlit white material temporarily to get a clean mask
                groundObj.GetComponent<Renderer>().sharedMaterial = WhiteMat;
                bakeCamera.Render();
            }

            // Step 2: Shrink and Print Inner Layer
            {
                Mesh originalMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;
                List<ShrinkPass> shrinkPasses = new List<ShrinkPass>() {
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

        }

        private void RenderTowns(List<RadialMask> townMask)
        {

            // Update camera culling mask so it only render the town meshes
            // and not the ground one.
            bakeCamera.cullingMask = 1 << TOWN_LAYER_ID;

            int townCircleSegments = 30;
            float shrink = 3.0f;
            float margin = 5.0f;
            List<GameObject> circles = new();

            foreach (var mask in townMask)
            {
                // Converts circular radial mask to actual distorted meshes
                Circle circle = new Circle()
                {
                    Name = "town_TEMP",
                    Segments = townCircleSegments,
                    Smooth = true,
                    SmoothThresholdAngle = 160,
                    Radius = mask.Radius + margin,
                    Position = mask.Position,
                    NoiseAmplitude = 2.0f,
                };

                circle.Spawn();
                GameObject circleGO = new GameObject();
                MeshRenderer mr = circleGO.AddComponent<MeshRenderer>();
                MeshFilter mf = circleGO.AddComponent<MeshFilter>();
                circleGO.transform.parent = gameObject.transform;
                circleGO.layer = TOWN_LAYER_ID;
                mf.mesh = circle.Mesh;
                mr.sharedMaterial = WhiteMat;
                circles.Add(circleGO);
            }

            // Render the initial mask in the begin texture
            bakeCamera.targetTexture = TmpTex(TmpTexType.TownMaskBegin);
            bakeCamera.Render();

            // Apply shrinking
            circles.ForEach(c => Utils.MeshUtils.Shrink(c.GetComponent<MeshFilter>().mesh, shrink));
            // Render the initial mask in the end texture
            bakeCamera.targetTexture = TmpTex(TmpTexType.TownMaskEnd);
            bakeCamera.Render();

            // clean up
            circles.ForEach(c => Destroy(c));
            bakeCamera.cullingMask = 1 << GROUND_LAYER_ID;
        }


        public Texture2D Bake(GameObject groundObj, List<RadialMask> townMask = null)
        {

            FitCameraToMesh(groundObj, 0.0f);

            _tmpTex.ForEach(t => t.texture = RenderTexture.GetTemporary(t.resolution, t.resolution, t.format));

            // Store original state
            int originalLayer = groundObj.layer;
            Vector3 originalPos = groundObj.transform.position;
            Material originalMat = groundObj.GetComponent<Renderer>().sharedMaterial;
            Mesh originalMesh = groundObj.GetComponent<MeshFilter>().sharedMesh;

            groundObj.layer = GROUND_LAYER_ID;

            RenderGround(groundObj);

            if (townMask != null)
                RenderTowns(townMask);

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
