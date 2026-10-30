using System.Collections.Generic;
using UnityEngine;

namespace BalkanMines
{
    /// <summary>Builds the low-poly Balkan world around the minefield.</summary>
    public static class WorldBuilder
    {
        public class Layout
        {
            public float GridHalf;
            public float FenceHalf;
            public Vector3 PlayerSpawn;
            public Vector3 Center;
            public Transform Root;
        }

        public const float TileSpacing = 2.15f;
        public const float TileSize = 1.72f;

        private static readonly Color Wood = new Color(0.36f, 0.22f, 0.12f);
        private static readonly Color WoodLight = new Color(0.52f, 0.34f, 0.18f);
        private static readonly Color Concrete = new Color(0.72f, 0.70f, 0.65f);
        private static readonly Color ConcreteDark = new Color(0.52f, 0.51f, 0.47f);
        private static readonly Color RoofRed = new Color(0.58f, 0.15f, 0.10f);
        private static readonly Color Plaster = new Color(0.90f, 0.86f, 0.74f);
        private static readonly Color Gold = new Color(0.92f, 0.72f, 0.18f);

        // ------------------------------------------------------------------
        //  Atmosphere
        // ------------------------------------------------------------------

        public static void ApplyAtmosphere(LevelDefinition def)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = def.FogColor;
            RenderSettings.fogDensity = def.FogDensity;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(def.FogColor, Color.white, 0.20f) * 0.82f;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.name = "Procedural Sky";
                sky.SetColor("_SkyTint", def.SkyTint);
                sky.SetColor("_GroundColor", def.Grass * 0.65f);
                sky.SetFloat("_AtmosphereThickness", def.HighMountains ? 1.25f : 0.85f);
                sky.SetFloat("_Exposure", 1.15f);
                sky.SetFloat("_SunSize", 0.035f);
                sky.SetFloat("_SunSizeConvergence", 4f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.sun = null;
        }

        private static GameObject BuildSky(Transform parent, LevelDefinition def)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(def.HighMountains ? 36f : 48f, -38f, 0f);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = def.HighMountains ? new Color(1f, 0.88f, 0.72f) : new Color(1f, 0.95f, 0.82f);
            sun.intensity = def.HighMountains ? 1.0f : 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;
            sun.shadowBias = 0.04f;
            RenderSettings.sun = sun;

            for (int i = 0; i < 8; i++)
            {
                var cloud = new GameObject("Cloud_" + i);
                cloud.transform.SetParent(parent, false);
                var group = cloud.transform;
                float angle = i * 47f * Mathf.Deg2Rad;
                float radius = 55f + (i % 3) * 22f;
                cloud.transform.position = new Vector3(Mathf.Cos(angle) * radius, 34f + (i % 4) * 5f, Mathf.Sin(angle) * radius);
                cloud.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                var drift = cloud.AddComponent<CloudDrift>();
                drift.Speed = 0.35f + (i % 4) * 0.12f;
                drift.Wrap = 130f;
                drift.Phase = i * 1.3f;
                var mat = LP.Unlit(new Color(0.97f, 0.97f, 0.95f));
                for (int k = 0; k < 4; k++)
                {
                    var puff = LP.Prim(PrimitiveType.Sphere, "Puff", group,
                        new Vector3((k - 1.5f) * 4.2f, Random.Range(-0.6f, 0.7f), Random.Range(-1.4f, 1.4f)),
                        new Vector3(7f + Random.Range(-1.5f, 2f), 2.2f, 4.2f), mat);
                    puff.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 40f), 0f);
                    var pr = puff.GetComponent<MeshRenderer>();
                    if (pr != null)
                    {
                        pr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        pr.receiveShadows = false;
                    }
                }
            }
            return sunGo;
        }

        private static void BuildGround(Transform parent, LevelDefinition def)
        {
            var grassTex = LP.NoiseTexture("grass_" + def.CountryCode, 512, def.Grass * 0.78f, def.GrassAlt, 0.8f, 11);
            grassTex.wrapMode = TextureWrapMode.Repeat;
            var grassMat = LP.TexturedMat(Color.white, grassTex, 0.02f, new Vector2(40f, 40f));
            var ground = LP.Prim(PrimitiveType.Plane, "Balkan Ground", parent, Vector3.zero,
                new Vector3(30f, 1f, 30f), grassMat, true);
            ground.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void BuildClearing(Transform parent, float half)
        {
            var gravel = LP.NoiseTexture("gravel", 256, new Color(0.52f, 0.48f, 0.39f), new Color(0.68f, 0.63f, 0.50f), 0.9f, 21);
            var mat = LP.TexturedMat(Color.white, gravel, 0.02f, new Vector2(half * 2.4f, half * 2.4f));
            var walk = LP.Prim(PrimitiveType.Plane, "Stone Walkway", parent, new Vector3(0f, 0.02f, 0f),
                new Vector3(half * 0.24f, 1f, half * 0.24f), mat);
            walk.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------
        //  Level world
        // ------------------------------------------------------------------

        public static Layout BuildLevel(Transform root, LevelDefinition def)
        {
            ApplyAtmosphere(def);
            var layout = new Layout { Root = root, Center = Vector3.zero };
            float gridHalf = def.Size * TileSpacing * 0.5f;
            layout.GridHalf = gridHalf;
            layout.FenceHalf = gridHalf + 3.5f;
            layout.PlayerSpawn = new Vector3(0f, 0.3f, -(layout.FenceHalf - 2.15f));

            var env = new GameObject("Environment").transform;
            env.SetParent(root, false);
            BuildGround(env, def);
            BuildSky(env, def);
            BuildClearing(env, layout.FenceHalf);
            BuildFence(root, layout.FenceHalf, def);
            BuildSurroundings(env, def, layout.FenceHalf);

            // The country flag towers over the north fence. The level title lives in the HUD
            // and intro banner, so no floating world-space label is needed here.
            var flag = CreateFlagPole(env, new Vector3(0f, 0f, layout.FenceHalf + 2.8f), 9.6f, def.CountryCode, 3.8f, 2.3f);
            flag.name = "Flag_" + def.Country;

            // Yugoslav memorial and flag.
            var monument = BuildMonument(env, new Vector3(-layout.FenceHalf - 7.5f, 0f, layout.FenceHalf + 1.5f), 0.85f);
            monument.name = "Spomenik";
            CreateFlagPole(env, new Vector3(-layout.FenceHalf - 2.8f, 0f, layout.FenceHalf + 2.6f), 7.2f, def.CountryCode, 2.9f, 1.8f);

            // A small gateway sign at the south, outside the fence.
            BuildGateSign(env, new Vector3(0f, 0f, -layout.FenceHalf - 2.6f), def);

            return layout;
        }

        private static void BuildGateSign(Transform parent, Vector3 pos, LevelDefinition def)
        {
            var g = new GameObject("Gate Sign");
            g.transform.SetParent(parent, false);
            g.transform.position = pos;
            var postMat = LP.Mat(Wood);
            LP.MeshObj("PostL", g.transform, LP.Cylinder(6, 0.10f, 0.12f, 2.6f), postMat, new Vector3(-1.6f, 1.3f, 0f), Vector3.one);
            LP.MeshObj("PostR", g.transform, LP.Cylinder(6, 0.10f, 0.12f, 2.6f), postMat, new Vector3(1.6f, 1.3f, 0f), Vector3.one);
            var board = LP.Prim(PrimitiveType.Cube, "Board", g.transform, new Vector3(0f, 2.15f, 0f),
                new Vector3(3.4f, 0.72f, 0.12f), LP.Mat(WoodLight));
            var text = LP.WorldText(g.transform, "BALKANSKE MINE", new Vector3(0f, 2.15f, -0.09f), 0.22f,
                new Color(0.98f, 0.93f, 0.76f), Quaternion.identity, TextAnchor.MiddleCenter);
            for (int i = 0; i < 7; i++)
            {
                var star = LP.MeshObj("Star", g.transform,
                    LP.StarMesh(0.17f, 0.075f, 0.03f), LP.Mat(new Color(0.78f, 0.10f, 0.12f)),
                    new Vector3(-1.35f + i * 0.45f, 2.52f, -0.09f), Vector3.one, Quaternion.identity);
                star.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }

        public static void BuildFence(Transform root, float half, LevelDefinition def)
        {
            var fence = new GameObject("Fence").transform;
            fence.SetParent(root, false);
            float h = 1.85f;
            var mat = LP.Mat(Wood);
            var matLight = LP.Mat(WoodLight);

            int perSide = Mathf.Max(6, Mathf.RoundToInt(half * 2f / 2.4f));
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i <= perSide; i++)
                {
                    float t = (i / (float)perSide) * 2f - 1f;
                    Vector3 p;
                    if (side == 0) p = new Vector3(t * half, h * 0.5f, -half);
                    else if (side == 1) p = new Vector3(t * half, h * 0.5f, half);
                    else if (side == 2) p = new Vector3(-half, h * 0.5f, t * half);
                    else p = new Vector3(half, h * 0.5f, t * half);
                    LP.MeshObj("Fence Post", fence, LP.Cylinder(6, 0.10f, 0.13f, h + 0.30f),
                        mat, p, Vector3.one);
                    var cap = LP.MeshObj("Post Cap", fence, LP.Cone(6, 0.16f, 0.22f),
                        matLight, p + Vector3.up * (h * 0.62f), Vector3.one);
                    cap.name = "Fence Cap";
                }
            }

            for (int side = 0; side < 4; side++)
            {
                for (int rail = 0; rail < 2; rail++)
                {
                    float y = rail == 0 ? 0.62f : 1.28f;
                    Vector3 pos, scale;
                    if (side == 0) { pos = new Vector3(0f, y, -half); scale = new Vector3(half * 2f, 0.10f, 0.10f); }
                    else if (side == 1) { pos = new Vector3(0f, y, half); scale = new Vector3(half * 2f, 0.10f, 0.10f); }
                    else if (side == 2) { pos = new Vector3(-half, y, 0f); scale = new Vector3(0.10f, 0.10f, half * 2f); }
                    else { pos = new Vector3(half, y, 0f); scale = new Vector3(0.10f, 0.10f, half * 2f); }
                    LP.Prim(PrimitiveType.Cube, "Fence Rail", fence, pos, scale, matLight);
                }
            }

            // Bunting along the top rail.
            BuildBunting(fence, half);

            // Invisible walls: the player can never leave.
            AddInvisibleWall(fence, new Vector3(0f, 1.6f, -half), new Vector3(half * 2f + 1f, 3.2f, 0.6f));
            AddInvisibleWall(fence, new Vector3(0f, 1.6f, half), new Vector3(half * 2f + 1f, 3.2f, 0.6f));
            AddInvisibleWall(fence, new Vector3(-half, 1.6f, 0f), new Vector3(0.6f, 3.2f, half * 2f + 1f));
            AddInvisibleWall(fence, new Vector3(half, 1.6f, 0f), new Vector3(0.6f, 3.2f, half * 2f + 1f));
        }

        private static void AddInvisibleWall(Transform parent, Vector3 pos, Vector3 size)
        {
            var go = new GameObject("Boundary");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var bc = go.AddComponent<BoxCollider>();
            bc.size = size;
        }

        private static void BuildBunting(Transform parent, float half)
        {
            Color[] cols = { new Color(0.78f, 0.10f, 0.12f), new Color(0.95f, 0.95f, 0.90f), new Color(0.08f, 0.22f, 0.55f) };
            Mesh tri = TriFlagMesh();
            var mats = new[] { LP.Mat(cols[0]), LP.Mat(cols[1]), LP.Mat(cols[2]) };
            int count = Mathf.Max(18, Mathf.RoundToInt(half * 3.1f));
            for (int side = 0; side < 4; side++)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = (i + 0.5f) / count * 2f - 1f;
                    Vector3 pos;
                    Quaternion rot;
                    if (side == 0) { pos = new Vector3(t * half, 1.62f, -half - 0.06f); rot = Quaternion.Euler(0f, 180f, 0f); }
                    else if (side == 1) { pos = new Vector3(t * half, 1.62f, half + 0.06f); rot = Quaternion.identity; }
                    else if (side == 2) { pos = new Vector3(-half - 0.06f, 1.62f, t * half); rot = Quaternion.Euler(0f, 90f, 0f); }
                    else { pos = new Vector3(half + 0.06f, 1.62f, t * half); rot = Quaternion.Euler(0f, -90f, 0f); }
                    var go = new GameObject("Bunting");
                    go.transform.SetParent(parent, false);
                    go.transform.localPosition = pos;
                    go.transform.localRotation = rot;
                    go.transform.localScale = new Vector3(0.28f, 0.42f, 0.028f);
                    go.AddComponent<MeshFilter>().sharedMesh = tri;
                    go.AddComponent<MeshRenderer>().sharedMaterial = mats[i % 3];
                }
            }
        }

        private static Mesh TriFlagMesh()
        {
            var m = new Mesh { name = "TriFlag" };
            m.vertices = new[]
            {
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f), new Vector3(0f, -0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f), new Vector3(0f, -0.5f, 0f), new Vector3(0.5f, 0.5f, 0f)
            };
            m.triangles = new[] { 0, 1, 2, 3, 4, 5 };
            m.uv = new[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 0), new Vector2(0, 1), new Vector2(0.5f, 0), new Vector2(1, 1) };
            m.RecalculateNormals();
            return m;
        }

        // ------------------------------------------------------------------
        //  Surroundings
        // ------------------------------------------------------------------

        private static void BuildSurroundings(Transform env, LevelDefinition def, float fenceHalf)
        {
            var rng = new System.Random(1000 + def.Size * 31 + def.Mines);
            BuildMountains(env, def, fenceHalf, rng);
            BuildTrees(env, def, fenceHalf, rng);
            BuildHouses(env, def, fenceHalf, rng);
            BuildRocksAndBushes(env, def, fenceHalf, rng);
            BuildHaystacks(env, def, fenceHalf, rng);

            if (def.Coastal) BuildSea(env, fenceHalf);
            if (def.HighMountains) BuildMountainVillage(env, def, fenceHalf, rng);

            // A few Yugoslav-era details.
            BuildCar(env, new Vector3(fenceHalf + 5.5f, 0f, -fenceHalf - 1.0f), 40f, new Color(0.72f, 0.12f, 0.10f));
            BuildCar(env, new Vector3(-fenceHalf - 4.0f, 0f, -fenceHalf + 4.5f), 205f, new Color(0.16f, 0.36f, 0.62f));
        }

        private static void BuildMountains(Transform parent, LevelDefinition def, float fenceHalf, System.Random rng)
        {
            int count = def.HighMountains ? 26 : 18;
            float baseRadius = def.HighMountains ? 78f : 92f;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f + (float)rng.NextDouble() * 0.12f;
                float radius = baseRadius + (float)rng.NextDouble() * 55f;
                float h = (def.HighMountains ? 26f : 17f) + (float)rng.NextDouble() * (def.HighMountains ? 34f : 22f);
                float r = h * (0.65f + (float)rng.NextDouble() * 0.35f);
                var mesh = LP.Mountain(100 + i, 7, r, h, 0.22f);
                var col = Color.Lerp(def.Grass * 0.55f, new Color(0.38f, 0.38f, 0.36f), 0.45f + (float)rng.NextDouble() * 0.35f);
                var m = LP.MeshObj("Mountain", parent, mesh, LP.Mat(col),
                    new Vector3(Mathf.Cos(angle) * radius, -0.4f, Mathf.Sin(angle) * radius), Vector3.one);
                m.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (def.Snow && h > 34f)
                {
                    var snowMesh = LP.Mountain(500 + i, 7, r * 0.38f, h * 0.35f, 0.18f);
                    var snow = LP.MeshObj("Snow Cap", m.transform, snowMesh, LP.Mat(new Color(0.92f, 0.94f, 0.95f)),
                        new Vector3(0f, h * 0.66f, 0f), Vector3.one);
                    snow.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        private static void BuildTrees(Transform parent, LevelDefinition def, float fenceHalf, System.Random rng)
        {
            int count = def.HighMountains ? 110 : 78;
            float min = fenceHalf + 6.5f;
            float max = fenceHalf + 46f;
            for (int i = 0; i < count; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = Mathf.Lerp(min, max, Mathf.Sqrt((float)rng.NextDouble()));
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                // Keep the country flag and monument sight lines clear.
                if (Mathf.Abs(pos.x) < 5f && pos.z > 0f) continue;
                if (def.Coastal && pos.x > fenceHalf + 6f) continue;
                float s = 0.75f + (float)rng.NextDouble() * 0.9f;
                if (def.HighMountains) BuildPine(parent, pos, s * 1.15f, rng);
                else if (def.Coastal) BuildCypress(parent, pos, s * 1.1f, rng);
                else if (i % 3 == 0) BuildPine(parent, pos, s, rng);
                else BuildDeciduous(parent, pos, s, rng);
            }
        }

        private static void BuildPine(Transform parent, Vector3 pos, float s, System.Random rng)
        {
            var g = new GameObject("Pine");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            var trunkMat = LP.Mat(new Color(0.34f, 0.22f, 0.13f));
            LP.MeshObj("Trunk", g.transform, LP.Cylinder(5, 0.16f, 0.22f, 2.4f), trunkMat, new Vector3(0f, 1.2f, 0f), Vector3.one);
            var green = Color.Lerp(new Color(0.12f, 0.30f, 0.15f), new Color(0.22f, 0.40f, 0.18f), (float)rng.NextDouble());
            var needle = LP.Mat(green, 0.02f);
            for (int k = 0; k < 3; k++)
            {
                float y = 1.55f + k * 1.05f;
                float r = (1.35f - k * 0.28f) * s;
                LP.MeshObj("Needles", g.transform, LP.Cone(7, r, 1.9f * s), needle, new Vector3(0f, y, 0f), Vector3.one);
            }
            g.transform.localScale = Vector3.one * s;
        }

        private static void BuildCypress(Transform parent, Vector3 pos, float s, System.Random rng)
        {
            var g = new GameObject("Cypress");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            var trunkMat = LP.Mat(new Color(0.32f, 0.22f, 0.14f));
            LP.MeshObj("Trunk", g.transform, LP.Cylinder(5, 0.12f, 0.18f, 1.6f), trunkMat, new Vector3(0f, 0.8f, 0f), Vector3.one);
            var green = new Color(0.11f, 0.27f, 0.15f);
            LP.MeshObj("Crown", g.transform, LP.Cone(7, 0.75f * s, 6.0f * s), LP.Mat(green, 0.02f), new Vector3(0f, 1.2f, 0f), Vector3.one);
            g.transform.localScale = Vector3.one * s;
        }

        private static void BuildDeciduous(Transform parent, Vector3 pos, float s, System.Random rng)
        {
            var g = new GameObject("Tree");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            var trunkMat = LP.Mat(new Color(0.37f, 0.24f, 0.13f));
            LP.MeshObj("Trunk", g.transform, LP.Cylinder(5, 0.15f, 0.25f, 2.8f), trunkMat, new Vector3(0f, 1.4f, 0f), Vector3.one);
            var green = Color.Lerp(new Color(0.24f, 0.42f, 0.16f), new Color(0.42f, 0.55f, 0.20f), (float)rng.NextDouble());
            var crownMat = LP.Mat(green, 0.04f);
            var ico = LP.IcoSphere(0);
            for (int k = 0; k < 3; k++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                var off = new Vector3(Mathf.Cos(a) * 0.5f, 2.9f + k * 0.55f, Mathf.Sin(a) * 0.5f);
                LP.MeshObj("Crown", g.transform, ico, crownMat, off,
                    new Vector3(1.6f - k * 0.18f, 1.1f, 1.5f));
            }
            g.transform.localScale = Vector3.one * s;
        }

        private static void BuildRocksAndBushes(Transform parent, LevelDefinition def, float fenceHalf, System.Random rng)
        {
            var rockMat = LP.Mat(new Color(0.48f, 0.47f, 0.43f));
            var bushMat = LP.Mat(new Color(0.23f, 0.38f, 0.18f));
            for (int i = 0; i < 46; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = fenceHalf + 2.5f + (float)rng.NextDouble() * 30f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                if (Mathf.Abs(pos.x) < fenceHalf + 5f && Mathf.Abs(pos.z) < fenceHalf + 5f) continue;
                if (i % 3 == 0)
                {
                    LP.MeshObj("Rock", parent, LP.RockMesh(300 + i), rockMat, pos,
                        Vector3.one * (0.4f + (float)rng.NextDouble() * 1.2f));
                }
                else
                {
                    var bush = new GameObject("Bush");
                    bush.transform.SetParent(parent, false);
                    bush.transform.localPosition = pos;
                    bush.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                    for (int k = 0; k < 3; k++)
                    {
                        LP.MeshObj("Leaf", bush.transform, LP.IcoSphere(0), bushMat,
                            new Vector3((float)rng.NextDouble() * 0.8f - 0.4f, 0.3f, (float)rng.NextDouble() * 0.8f - 0.4f),
                            new Vector3(0.7f, 0.5f, 0.7f));
                    }
                }
            }
        }

        private static void BuildHaystacks(Transform parent, LevelDefinition def, float fenceHalf, System.Random rng)
        {
            var straw = LP.Mat(new Color(0.80f, 0.66f, 0.28f));
            var strawDark = LP.Mat(new Color(0.62f, 0.49f, 0.19f));
            for (int i = 0; i < 5; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = fenceHalf + 7f + (float)rng.NextDouble() * 12f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var g = new GameObject("Haystack");
                g.transform.SetParent(parent, false);
                g.transform.localPosition = pos;
                g.transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
                LP.MeshObj("Base", g.transform, LP.Cylinder(7, 0.95f, 1.05f, 0.9f), strawDark, new Vector3(0f, 0.45f, 0f), Vector3.one);
                LP.MeshObj("Hay", g.transform, LP.Cone(8, 1.25f, 1.9f), straw, new Vector3(0f, 0.8f, 0f), Vector3.one);
                LP.MeshObj("Pole", g.transform, LP.Cylinder(4, 0.05f, 0.05f, 2.9f), strawDark, new Vector3(0f, 1.2f, 0f), Vector3.one);
            }

            var sunflower = LP.Mat(new Color(0.94f, 0.75f, 0.08f));
            var sunflowerCenter = LP.Mat(new Color(0.25f, 0.13f, 0.05f));
            var stem = LP.Mat(new Color(0.20f, 0.38f, 0.12f));
            for (int i = 0; i < 10; i++)
            {
                float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
                float radius = fenceHalf + 5.5f + (float)rng.NextDouble() * 9f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var g = new GameObject("Sunflower");
                g.transform.SetParent(parent, false);
                g.transform.localPosition = pos;
                LP.MeshObj("Stem", g.transform, LP.Cylinder(4, 0.035f, 0.05f, 1.5f), stem, new Vector3(0f, 0.75f, 0f), Vector3.one);
                LP.MeshObj("Head", g.transform, LP.Cylinder(10, 0.32f, 0.32f, 0.08f), sunflower,
                    new Vector3(0f, 1.5f, 0f), Vector3.one, Quaternion.Euler(90f, 0f, 0f));
                LP.MeshObj("Center", g.transform, LP.Cylinder(8, 0.15f, 0.15f, 0.10f), sunflowerCenter,
                    new Vector3(0f, 1.5f, -0.02f), Vector3.one, Quaternion.Euler(90f, 0f, 0f));
            }
        }

        private static void BuildSea(Transform parent, float fenceHalf)
        {
            var waterTex = LP.NoiseTexture("sea", 256, new Color(0.06f, 0.30f, 0.49f), new Color(0.12f, 0.48f, 0.60f), 0.8f, 41);
            var water = LP.TexturedMat(new Color(0.7f, 0.9f, 1f), waterTex, 0.8f, new Vector2(18f, 18f));
            // Keep the near edge beyond the beach — the old centre/scale put the
            // plane over half of the playing field.
            var plane = LP.Prim(PrimitiveType.Plane, "Jadransko More", parent,
                new Vector3(fenceHalf + 82f, 0.055f, 0f), new Vector3(12f, 1f, 24f), water);
            plane.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var sand = LP.Mat(new Color(0.82f, 0.75f, 0.55f));
            LP.Prim(PrimitiveType.Plane, "Beach", parent, new Vector3(fenceHalf + 17f, 0.045f, 0f),
                new Vector3(1.2f, 1f, 12f), sand);

            // A few white foam strips and rocks on the shore.
            for (int i = 0; i < 12; i++)
            {
                var foam = LP.Prim(PrimitiveType.Cube, "Foam", parent,
                    new Vector3(fenceHalf + 21f + (i % 3) * 0.8f, 0.07f, -24f + i * 4.4f),
                    new Vector3(0.35f + (i % 2) * 0.5f, 0.04f, 2.2f), LP.Mat(new Color(0.90f, 0.96f, 0.96f)));
                foam.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private static void BuildMountainVillage(Transform parent, LevelDefinition def, float fenceHalf, System.Random rng)
        {
            // A small wooden bridge and a chapel tucked into the mountain mist.
            var wood = LP.Mat(new Color(0.40f, 0.25f, 0.13f));
            var g = new GameObject("Bridge");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = new Vector3(-fenceHalf - 12f, 0f, -fenceHalf - 6f);
            g.transform.localRotation = Quaternion.Euler(0f, 25f, 0f);
            for (int i = 0; i < 7; i++)
            {
                LP.Prim(PrimitiveType.Cube, "Plank", g.transform, new Vector3(0f, 0.28f, -2.1f + i * 0.7f),
                    new Vector3(3.2f, 0.12f, 0.62f), wood);
            }
            LP.Prim(PrimitiveType.Cube, "RailL", g.transform, new Vector3(-1.45f, 0.75f, 0f),
                new Vector3(0.10f, 0.14f, 5f), wood);
            LP.Prim(PrimitiveType.Cube, "RailR", g.transform, new Vector3(1.45f, 0.75f, 0f),
                new Vector3(0.10f, 0.14f, 5f), wood);

            BuildChurch(parent, new Vector3(fenceHalf + 3f, 0f, -fenceHalf - 14f), 55f);
        }

        // ------------------------------------------------------------------
        //  Buildings and props
        // ------------------------------------------------------------------

        private static void BuildHouses(Transform parent, LevelDefinition def, float fenceHalf, System.Random rng)
        {
            int count = def.HighMountains ? 5 : 4;
            for (int i = 0; i < count; i++)
            {
                float angle = (60f + i * (360f / count) + (float)rng.NextDouble() * 20f) * Mathf.Deg2Rad;
                float radius = fenceHalf + 10f + (float)rng.NextDouble() * 9f;
                var pos = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                var rot = Quaternion.LookRotation(new Vector3(-pos.x, 0f, -pos.z), Vector3.up);
                var house = BuildHouse(parent, pos, rot, 0.9f + (float)rng.NextDouble() * 0.35f, rng);
                if (i == 0) BuildChurch(parent, pos + rot * new Vector3(10f, 0f, -2f), 20f);
            }
        }

        private static GameObject BuildHouse(Transform parent, Vector3 pos, Quaternion rot, float scale, System.Random rng)
        {
            var g = new GameObject("Balkan House");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = rot;
            g.transform.localScale = Vector3.one * scale;

            var plaster = LP.Mat(Plaster);
            var roof = LP.Mat(RoofRed);
            var wood = LP.Mat(Wood);
            var window = LP.Mat(new Color(0.12f, 0.18f, 0.24f), 0.4f);

            LP.Prim(PrimitiveType.Cube, "Base", g.transform, new Vector3(0f, 1.6f, 0f), new Vector3(5.2f, 3.2f, 4.4f), plaster);
            LP.MeshObj("Roof", g.transform, LP.GableRoof(6.4f, 5.4f, 2.1f), roof, new Vector3(0f, 3.2f, 0f), Vector3.one);
            LP.Prim(PrimitiveType.Cube, "Chimney", g.transform, new Vector3(1.7f, 4.4f, 1.1f),
                new Vector3(0.55f, 1.7f, 0.55f), LP.Mat(ConcreteDark));

            LP.Prim(PrimitiveType.Cube, "Door", g.transform, new Vector3(0f, 1.1f, 2.21f),
                new Vector3(0.9f, 2.0f, 0.10f), wood);
            LP.Prim(PrimitiveType.Cube, "WindowL", g.transform, new Vector3(-1.65f, 2.0f, 2.21f),
                new Vector3(0.9f, 0.9f, 0.06f), window);
            LP.Prim(PrimitiveType.Cube, "WindowR", g.transform, new Vector3(1.65f, 2.0f, 2.21f),
                new Vector3(0.9f, 0.9f, 0.06f), window);
            LP.Prim(PrimitiveType.Cube, "WindowSide", g.transform, new Vector3(2.61f, 2.0f, 0f),
                new Vector3(0.06f, 0.8f, 1.1f), window);

            // Fowler / flower box and a small bench.
            LP.Prim(PrimitiveType.Cube, "Planter", g.transform, new Vector3(0f, 0.55f, 2.55f),
                new Vector3(2.0f, 0.35f, 0.35f), LP.Mat(new Color(0.55f, 0.29f, 0.16f)));
            for (int i = 0; i < 5; i++)
            {
                LP.Prim(PrimitiveType.Sphere, "Flower", g.transform, new Vector3(-0.8f + i * 0.4f, 0.82f, 2.55f),
                    new Vector3(0.22f, 0.22f, 0.22f), LP.Mat(i % 2 == 0 ? new Color(0.85f, 0.12f, 0.18f) : new Color(0.95f, 0.75f, 0.15f)));
            }
            return g;
        }

        private static void BuildChurch(Transform parent, Vector3 pos, float yaw)
        {
            var g = new GameObject("Church");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var plaster = LP.Mat(new Color(0.86f, 0.83f, 0.73f));
            var roof = LP.Mat(new Color(0.32f, 0.30f, 0.28f));
            var gold = LP.Mat(Gold, 0.7f, 0.5f);
            LP.Prim(PrimitiveType.Cube, "Nave", g.transform, new Vector3(0f, 2.0f, 0f), new Vector3(4.2f, 4.0f, 7.5f), plaster);
            LP.MeshObj("Roof", g.transform, LP.GableRoof(4.8f, 8.2f, 1.8f), roof, new Vector3(0f, 4.0f, 0f), Vector3.one);
            var drum = LP.MeshObj("Drum", g.transform, LP.Cylinder(10, 1.15f, 1.15f, 1.2f), plaster, new Vector3(0f, 5.5f, 1.0f), Vector3.one);
            var dome = LP.Prim(PrimitiveType.Sphere, "Dome", g.transform, new Vector3(0f, 6.55f, 1.0f),
                new Vector3(2.6f, 2.0f, 2.6f), gold);
            dome.name = "Golden Dome";
            LP.MeshObj("CrossV", g.transform, LP.Cylinder(4, 0.07f, 0.07f, 1.2f), gold, new Vector3(0f, 8.2f, 1.0f), Vector3.one);
            LP.MeshObj("CrossH", g.transform, LP.Cylinder(4, 0.07f, 0.07f, 0.7f), gold,
                new Vector3(0f, 8.35f, 1.0f), Vector3.one, Quaternion.Euler(0f, 0f, 90f));
            for (int i = 0; i < 3; i++)
                LP.Prim(PrimitiveType.Cube, "Window", g.transform, new Vector3(0f, 2.2f, -3.78f + i * 2.8f),
                    new Vector3(0.8f, 1.6f, 0.06f), LP.Mat(new Color(0.12f, 0.18f, 0.26f), 0.4f));
        }

        private static void BuildCar(Transform parent, Vector3 pos, float yaw, Color color)
        {
            var g = new GameObject("Zastava 750");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var body = LP.Mat(color, 0.45f, 0.15f);
            var chrome = LP.Mat(new Color(0.8f, 0.82f, 0.83f), 0.8f, 0.8f);
            var glass = LP.Mat(new Color(0.18f, 0.27f, 0.31f), 0.7f);
            var tyre = LP.Mat(new Color(0.08f, 0.08f, 0.08f));
            LP.Prim(PrimitiveType.Cube, "Body", g.transform, new Vector3(0f, 0.62f, 0f), new Vector3(1.65f, 0.55f, 3.15f), body);
            LP.Prim(PrimitiveType.Cube, "Cabin", g.transform, new Vector3(0f, 1.05f, -0.15f), new Vector3(1.48f, 0.62f, 1.6f), body);
            LP.Prim(PrimitiveType.Cube, "Windshield", g.transform, new Vector3(0f, 1.1f, 0.66f), new Vector3(1.3f, 0.45f, 0.05f), glass);
            LP.Prim(PrimitiveType.Cube, "RearGlass", g.transform, new Vector3(0f, 1.1f, -0.96f), new Vector3(1.3f, 0.45f, 0.05f), glass);
            LP.Prim(PrimitiveType.Cube, "BumperF", g.transform, new Vector3(0f, 0.45f, 1.61f), new Vector3(1.7f, 0.12f, 0.12f), chrome);
            LP.Prim(PrimitiveType.Cube, "BumperR", g.transform, new Vector3(0f, 0.45f, -1.61f), new Vector3(1.7f, 0.12f, 0.12f), chrome);
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.93f : 0.93f;
                LP.MeshObj("WheelF", g.transform, LP.Cylinder(10, 0.34f, 0.34f, 0.22f), tyre,
                    new Vector3(x, 0.34f, 1.0f), Vector3.one, Quaternion.Euler(0f, 0f, 90f));
                LP.MeshObj("WheelR", g.transform, LP.Cylinder(10, 0.34f, 0.34f, 0.22f), tyre,
                    new Vector3(x, 0.34f, -1.0f), Vector3.one, Quaternion.Euler(0f, 0f, 90f));
            }
            LP.Prim(PrimitiveType.Cube, "Plate", g.transform, new Vector3(0f, 0.55f, 1.63f), new Vector3(0.55f, 0.16f, 0.04f), chrome);
        }

        // ------------------------------------------------------------------
        //  Monument & flags
        // ------------------------------------------------------------------

        public static GameObject BuildMonument(Transform parent, Vector3 pos, float scale)
        {
            var g = new GameObject("Spomenik");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;
            g.transform.localScale = Vector3.one * scale;

            var concrete = LP.Mat(Concrete);
            var dark = LP.Mat(ConcreteDark);
            var red = LP.Mat(new Color(0.78f, 0.10f, 0.12f));

            LP.MeshObj("Platform", g.transform, LP.Cylinder(8, 4.3f, 4.6f, 0.55f), dark,
                new Vector3(0f, 0.27f, 0f), Vector3.one);
            LP.MeshObj("Step", g.transform, LP.Cylinder(8, 3.4f, 3.6f, 0.45f), concrete,
                new Vector3(0f, 0.77f, 0f), Vector3.one);

            var slabA = LP.Prim(PrimitiveType.Cube, "SlabA", g.transform, new Vector3(-0.85f, 4.2f, 0.35f),
                new Vector3(1.1f, 6.9f, 0.95f), concrete, false, Quaternion.Euler(0f, 8f, -4f));
            var slabB = LP.Prim(PrimitiveType.Cube, "SlabB", g.transform, new Vector3(0.75f, 5.2f, -0.2f),
                new Vector3(1.0f, 8.9f, 0.9f), concrete, false, Quaternion.Euler(0f, -12f, 5f));
            var slabC = LP.Prim(PrimitiveType.Cube, "SlabC", g.transform, new Vector3(0.2f, 3.1f, 1.1f),
                new Vector3(0.9f, 4.8f, 0.8f), concrete, false, Quaternion.Euler(-6f, 22f, -2f));

            // Red star, the Yugoslav symbol.
            var star = LP.MeshObj("Red Star", g.transform, LP.StarMesh(0.62f, 0.27f, 0.16f), red,
                new Vector3(0.78f, 9.85f, -0.2f), Vector3.one, Quaternion.Euler(0f, 0f, 180f));
            star.AddComponent<Spinner>().DegreesPerSecond = 12f;
            star.GetComponent<Spinner>().Axis = Vector3.up;

            // Plaque and eternal flame.
            var plaque = new GameObject("Plaque");
            plaque.transform.SetParent(g.transform, false);
            plaque.transform.localPosition = new Vector3(0f, 1.35f, -2.2f);
            plaque.transform.localRotation = Quaternion.identity;
            LP.Prim(PrimitiveType.Cube, "PlaqueBack", plaque.transform, Vector3.zero,
                new Vector3(4.2f, 1.3f, 0.10f), dark);
            var text = LP.WorldText(plaque.transform, "BRATSTVO\nI JEDINSTVO", new Vector3(0f, 0f, -0.07f), 0.32f,
                new Color(0.95f, 0.88f, 0.65f), Quaternion.identity, TextAnchor.MiddleCenter);
            text.transform.localPosition = new Vector3(0f, 0f, -0.07f);
            var text2 = LP.WorldText(plaque.transform, "SFRJ  •  1945", new Vector3(0f, -0.42f, -0.07f), 0.15f,
                new Color(0.86f, 0.80f, 0.58f), Quaternion.identity, TextAnchor.MiddleCenter);
            text2.transform.localPosition = new Vector3(0f, -0.42f, -0.07f);

            var flameBase = LP.MeshObj("Flame Base", g.transform, LP.Cylinder(8, 0.65f, 0.85f, 0.25f), dark,
                new Vector3(0f, 1.05f, -3.3f), Vector3.one);
            var flame = LP.MeshObj("Eternal Flame", g.transform, LP.Cone(6, 0.28f, 0.9f),
                LP.Unlit(new Color(1f, 0.52f, 0.08f)), new Vector3(0f, 1.2f, -3.3f), Vector3.one);
            flame.AddComponent<Bobbing>().Amplitude = 0.08f;
            flame.AddComponent<Spinner>().DegreesPerSecond = 40f;
            var lightGo = new GameObject("Flame Light");
            lightGo.transform.SetParent(g.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2f, -3.3f);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.55f, 0.18f);
            light.range = 6f;
            light.intensity = 2.2f;

            return g;
        }

        public static GameObject CreateFlagPole(Transform parent, Vector3 pos, float height, string flagKey,
            float width, float heightOfFlag)
        {
            var g = new GameObject("Flag Pole");
            g.transform.SetParent(parent, false);
            g.transform.localPosition = pos;

            var pole = LP.Mat(new Color(0.78f, 0.77f, 0.72f), 0.5f, 0.4f);
            LP.MeshObj("Pole", g.transform, LP.Cylinder(8, 0.07f, 0.11f, height + 0.3f), pole,
                new Vector3(0f, height * 0.5f, 0f), Vector3.one);
            LP.Prim(PrimitiveType.Sphere, "Finial", g.transform, new Vector3(0f, height + 0.14f, 0f),
                new Vector3(0.32f, 0.32f, 0.32f), LP.Mat(Gold, 0.7f, 0.6f));

            var cloth = new GameObject("Flag Cloth");
            cloth.transform.SetParent(g.transform, false);
            cloth.transform.localPosition = new Vector3(0.07f, height - heightOfFlag * 0.5f - 0.15f, 0f);
            cloth.transform.localScale = new Vector3(width, heightOfFlag, 1f);
            var mf = cloth.AddComponent<MeshFilter>();
            mf.sharedMesh = LP.ClothMesh(10, 6);
            var mr = cloth.AddComponent<MeshRenderer>();
            mr.sharedMaterial = LP.TexturedMat(Color.white, LP.FlagTexture(flagKey));
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            var wave = cloth.AddComponent<FlagWave>();
            wave.Amplitude = 0.12f;
            wave.Speed = 2.1f;
            return g;
        }

        // ------------------------------------------------------------------
        //  Main menu
        // ------------------------------------------------------------------

        public static void BuildMenu(Transform root)
        {
            var def = LevelDefinition.All[0];
            ApplyAtmosphere(def);
            var env = new GameObject("Menu Environment").transform;
            env.SetParent(root, false);
            BuildGround(env, def);
            BuildSky(env, def);

            BuildMonument(env, Vector3.zero, 1.15f);
            CreateFlagPole(env, new Vector3(-5.4f, 0f, 1.2f), 8.6f, "yugoslavia", 3.4f, 2.1f);
            CreateFlagPole(env, new Vector3(5.3f, 0f, 2.2f), 7.2f, "serbia", 2.6f, 1.6f);

            var rng = new System.Random(77);
            BuildHouses(env, def, 12f, rng);
            BuildTrees(env, def, 12f, rng);
            BuildRocksAndBushes(env, def, 12f, rng);
            BuildMountains(env, def, 12f, rng);
            BuildHaystacks(env, def, 12f, rng);
            BuildCar(env, new Vector3(7.5f, 0f, -3.4f), 205f, new Color(0.72f, 0.12f, 0.10f));
            BuildGateSign(env, new Vector3(0f, 0f, -9f), def);
        }
    }
}
