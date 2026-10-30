using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BalkanMines
{
    /// <summary>
    /// Procedural low-poly art toolbox: meshes, materials, textures and primitives.
    /// Everything in the game is generated at runtime from these helpers.
    /// </summary>
    public static class LP
    {
        private static readonly Dictionary<string, Material> MatCache = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Texture2D> TexCache = new Dictionary<string, Texture2D>();
        private static Font _font;
        private static Material _worldTextMat;

        public static Font Font
        {
            get
            {
                if (_font == null)
                {
                    // Prefer a characterful OS font, fall back to the built-in legacy font.
                    string[] candidates =
                    {
                        "Bahnschrift SemiBold Condensed", "Bahnschrift SemiCondensed", "Bahnschrift",
                        "Impact", "Arial Black", "Georgia"
                    };
                    string[] installed = null;
                    try { installed = Font.GetOSInstalledFontNames(); } catch { }
                    if (installed != null)
                    {
                        foreach (var name in candidates)
                        {
                            if (System.Array.IndexOf(installed, name) < 0) continue;
                            var f = Font.CreateDynamicFontFromOSFont(name, 48);
                            if (f != null) { _font = f; break; }
                        }
                    }
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 24);
                }
                return _font;
            }
        }

        /// <summary>True when the project renders through URP (no Built-in pass support).</summary>
        public static readonly bool UseUrp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;

        /// <summary>
        /// TextMesh material that obeys scene depth. The built-in font material uses
        /// GUI/Text shader (ZTest Always), which made every world label draw on top of
        /// fences, houses and terrain. This is a transparent URP Unlit material with
        /// normal Z-testing, so text is occluded like any other object.
        /// </summary>
        public static Material WorldTextMaterial
        {
            get
            {
                if (_worldTextMat != null) return _worldTextMat;
                if (Font != null)
                    Font.RequestCharactersInTexture("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 •/—\n", 72, FontStyle.Bold);
                var tex = Font != null && Font.material != null ? Font.material.mainTexture : null;

                Shader shader = Resources.Load<Shader>("WorldText");
                if (shader == null) shader = Shader.Find("BalkanMines/WorldText");
                if (shader != null)
                {
                    var m = new Material(shader) { name = "WorldText" };
                    if (tex != null) m.mainTexture = tex;
                    m.SetColor("_Color", Color.white);
                    m.renderQueue = (int)RenderQueue.Transparent;
                    _worldTextMat = m;
                    return m;
                }

                // Fallback (should not happen): built-in font material keeps the text
                // visible even though it ignores scene depth.
                _worldTextMat = Font != null ? new Material(Font.material) : null;
                return _worldTextMat;
            }
        }

        private static Shader FindShader(bool lit, bool textured)
        {
            if (UseUrp)
            {
                // Resources materials guarantee that the URP shader and its passes
                // (depth, shadow caster) are actually packed into a player build.
                var baseMat = Resources.Load<Material>(lit ? "LitBase" : "UnlitBase");
                if (baseMat != null && baseMat.shader != null)
                {
                    LogShaderOnce(lit, baseMat.shader);
                    return baseMat.shader;
                }
                string name = lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit";
                Shader s = Shader.Find(name);
                if (s != null)
                {
                    LogShaderOnce(lit, s);
                    return s;
                }
                LogMissingOnce(lit, name);
            }
            string fallback = lit ? "Standard" : (textured ? "Unlit/Texture" : "Unlit/Color");
            Shader sh = Shader.Find(fallback);
            if (sh == null) sh = Shader.Find(lit ? "Universal Render Pipeline/Lit" : "Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh != null) LogShaderOnce(lit, sh);
            return sh;
        }

        private static readonly HashSet<string> LoggedShaders = new HashSet<string>();

        private static void LogShaderOnce(bool lit, Shader shader)
        {
            string key = (lit ? "lit:" : "unlit:") + shader.name;
            if (LoggedShaders.Add(key))
                Debug.Log("BalkanMines runtime shader " + key);
        }

        private static void LogMissingOnce(bool lit, string name)
        {
            string key = "missing:" + name;
            if (LoggedShaders.Add(key))
                Debug.LogError("BalkanMines could not find shader " + name + " in this build");
        }

        private static void ApplyColor(Material m, Color color)
        {
            // 3D props are opaque; guard against colours whose alpha was scaled.
            if (m.HasProperty("_Surface")) color.a = 1f;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.color = color;
        }

        /// <summary>Sets a colour on any shader variant (SRP or Built-in).</summary>
        public static void SetColor(Material m, Color color)
        {
            if (m == null) return;
            ApplyColor(m, color);
        }

        public static Color GetColor(Material m, Color fallback)
        {
            if (m == null) return fallback;
            if (m.HasProperty("_BaseColor")) return m.GetColor("_BaseColor");
            if (m.HasProperty("_Color")) return m.GetColor("_Color");
            return fallback;
        }

        private static void ApplyTexture(Material m, Texture2D tex)
        {
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            // Sprite/unlit shaders still expose the legacy alias.
            if (!m.HasProperty("_BaseMap") && !m.HasProperty("_MainTex")) m.mainTexture = tex;
        }

        public static Material Mat(Color color, float smoothness = 0.08f, float metallic = 0f)
        {
            string key = "S|" + ColorUtility.ToHtmlStringRGBA(color) + "|" + smoothness.ToString("F2") + "|" + metallic.ToString("F2");
            if (MatCache.TryGetValue(key, out var cached) && cached != null) return cached;
            Shader shader = FindShader(true, false);
            var m = new Material(shader);
            ApplyColor(m, color);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            m.enableInstancing = true;
            MatCache[key] = m;
            return m;
        }

        public static Material TexturedMat(Color tint, Texture2D tex, float smoothness = 0.06f, Vector2? tiling = null)
        {
            string key = "T|" + ColorUtility.ToHtmlStringRGBA(tint) + "|" + tex.name + "|" + smoothness +
                         "|" + (tiling.HasValue ? tiling.Value.x.ToString("F2") + "x" + tiling.Value.y.ToString("F2") : "1x1");
            if (MatCache.TryGetValue(key, out var cached) && cached != null) return cached;
            Shader shader = FindShader(true, true);
            var m = new Material(shader);
            ApplyColor(m, tint);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            ApplyTexture(m, tex);
            if (tiling.HasValue)
            {
                if (m.HasProperty("_BaseMap")) m.SetTextureScale("_BaseMap", tiling.Value);
                if (m.HasProperty("_MainTex")) m.SetTextureScale("_MainTex", tiling.Value);
                m.mainTextureScale = tiling.Value;
            }
            m.enableInstancing = true;
            MatCache[key] = m;
            return m;
        }

        public static Material Unlit(Color color, Texture2D tex = null)
        {
            string key = "U|" + ColorUtility.ToHtmlStringRGBA(color) + "|" + (tex != null ? tex.name : "");
            if (MatCache.TryGetValue(key, out var cached) && cached != null) return cached;
            Shader shader = FindShader(false, tex != null);
            var m = new Material(shader);
            ApplyColor(m, color);
            if (tex != null) ApplyTexture(m, tex);
            MatCache[key] = m;
            return m;
        }

        public static Material SpriteMat(Texture2D tex, Color tint)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            var m = new Material(shader);
            m.mainTexture = tex;
            m.color = tint;
            return m;
        }

        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos,
            Vector3 scale, Material mat, bool keepCollider = false, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (rotation.HasValue) go.transform.localRotation = rotation.Value;
            var r = go.GetComponent<Renderer>();
            if (r != null && mat != null) r.sharedMaterial = mat;
            if (!keepCollider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }
            return go;
        }

        public static GameObject MeshObj(string name, Transform parent, Mesh mesh, Material mat,
            Vector3 localPos, Vector3 scale, Quaternion? rotation = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            if (rotation.HasValue) go.transform.localRotation = rotation.Value;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        public static TextMesh WorldText(Transform parent, string text, Vector3 localPos, float size,
            Color color, Quaternion? rotation = null, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject("Text_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            if (rotation.HasValue) go.transform.localRotation = rotation.Value;
            var tm = go.AddComponent<TextMesh>();
            tm.font = Font;
            tm.text = text;
            tm.anchor = anchor;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 72;
            tm.characterSize = 0.1f * size;
            tm.color = color;
            tm.richText = true;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = WorldTextMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return tm;
        }

        public static void StripColliders(GameObject root)
        {
            foreach (var c in root.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        }

        // ------------------------------------------------------------------
        //  Textures
        // ------------------------------------------------------------------

        public static Texture2D NoiseTexture(string key, int size, Color a, Color b, float density, int seed)
        {
            if (TexCache.TryGetValue(key, out var existing) && existing != null) return existing;
            var rng = new System.Random(seed);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, true, false);
            tex.name = key;
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = (float)rng.NextDouble();
                    float blot = Mathf.PerlinNoise(x * 0.09f + seed, y * 0.09f + seed) * 0.6f + 0.4f;
                    Color c = Color.Lerp(a, b, Mathf.Clamp01(n * density + blot * 0.45f));
                    px[y * size + x] = c;
                }
            }
            tex.SetPixels(px);
            tex.Apply(true, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            TexCache[key] = tex;
            return tex;
        }

        public static Texture2D GradientTexture(string key, int w, int h, Color bottom, Color top, bool horizontal = false)
        {
            if (TexCache.TryGetValue(key, out var existing) && existing != null) return existing;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.name = key;
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float t = horizontal ? (float)x / (w - 1) : (float)y / (h - 1);
                    px[y * w + x] = Color.Lerp(bottom, top, t);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            TexCache[key] = tex;
            return tex;
        }

        public static Texture2D RadialTexture(string key, int size, Color center, Color edge, float power = 2f)
        {
            if (TexCache.TryGetValue(key, out var existing) && existing != null) return existing;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            tex.name = key;
            var px = new Color[size * size];
            Vector2 c = new Vector2(size / 2f, size / 2f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c) / (size * 0.5f);
                    float t = Mathf.Pow(Mathf.Clamp01(d), power);
                    px[y * size + x] = Color.Lerp(center, edge, t);
                }
            }
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            TexCache[key] = tex;
            return tex;
        }

        public static Texture2D FlagTexture(string key)
        {
            string cacheKey = "flag_" + key;
            if (TexCache.TryGetValue(cacheKey, out var existing) && existing != null) return existing;

            const int w = 320;
            const int h = 192;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false);
            tex.name = cacheKey;
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;

            Color red = new Color(0.78f, 0.10f, 0.13f);
            Color blue = new Color(0.06f, 0.22f, 0.58f);
            Color white = new Color(0.96f, 0.95f, 0.90f);
            Color gold = new Color(0.95f, 0.78f, 0.20f);

            for (int y = 0; y < h; y++)
            {
                Color row;
                if (key == "yugoslavia")
                {
                    row = y > h * 2f / 3f ? blue : y > h / 3f ? white : red;
                }
                else if (key == "serbia")
                {
                    row = y > h * 2f / 3f ? red : y > h / 3f ? blue : white;
                }
                else if (key == "croatia")
                {
                    row = y > h * 2f / 3f ? red : y > h / 3f ? white : blue;
                }
                else if (key == "bosnia")
                {
                    row = blue;
                }
                else
                {
                    row = new Color(0.55f, 0.10f, 0.10f);
                }
                for (int x = 0; x < w; x++) px[y * w + x] = row;
            }

            if (key == "croatia")
            {
                // Simple red/white checkerboard shield.
                int sx = w / 2 - 22, sy = h / 2 - 26;
                for (int y = sy; y < sy + 52; y++)
                {
                    for (int x = sx; x < sx + 44; x++)
                    {
                        int cx = (x - sx) / 11;
                        int cy = (y - sy) / 11;
                        Color c = ((cx + cy) % 2 == 0) ? white : red;
                        px[y * w + x] = c;
                    }
                }
            }
            else if (key == "bosnia")
            {
                // Yellow triangle + white stars.
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float tx = x / (float)w;
                        float ty = y / (float)h;
                        if (tx + ty > 1.05f) px[y * w + x] = gold;
                    }
                }
                for (int s = 0; s < 7; s++)
                {
                    DrawStar(px, w, h, 52 + s * 38, 20 + s * 24, 11, white, null);
                }
            }
            else if (key == "yugoslavia")
            {
                DrawStar(px, w, h, w / 2, h / 2, 46, red, gold);
            }
            else if (key == "serbia")
            {
                // Small golden shield with a cross.
                int cx = w / 2, cy = h / 2;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        float dx = Mathf.Abs(x - cx) / 30f;
                        float dy = Mathf.Abs(y - cy) / 36f;
                        if (dx + dy < 1f) px[y * w + x] = gold;
                    }
                }
                for (int y = cy - 22; y <= cy + 22; y++)
                for (int x = cx - 4; x <= cx + 4; x++)
                    if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = white;
                for (int y = cy - 4; y <= cy + 4; y++)
                for (int x = cx - 18; x <= cx + 18; x++)
                    if (x >= 0 && x < w && y >= 0 && y < h) px[y * w + x] = white;
            }

            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            TexCache[cacheKey] = tex;
            return tex;
        }

        public static Texture2D StarTexture(string key, int size, Color star, Color outline)
        {
            string cacheKey = "star_" + key + "_" + ColorUtility.ToHtmlStringRGBA(star) + "_" + ColorUtility.ToHtmlStringRGBA(outline);
            if (TexCache.TryGetValue(cacheKey, out var existing) && existing != null) return existing;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, false);
            tex.name = cacheKey;
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            DrawStar(px, size, size, size / 2, size / 2, size * 0.42f, star, outline);
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            TexCache[cacheKey] = tex;
            return tex;
        }

        private static void DrawStar(Color[] px, int w, int h, int cx, int cy, float radius, Color color, Color? outline)
        {
            // 5-pointed star polygon, drawn with an outline by testing radii.
            Vector2[] pts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float ang = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? radius : radius * 0.42f;
                pts[i] = new Vector2(cx + Mathf.Cos(ang) * r, cy + Mathf.Sin(ang) * r);
            }
            float outer = outline.HasValue ? Mathf.Max(2f, radius * 0.10f) : 0f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (!PointInPolygon(pts, new Vector2(x + 0.5f, y + 0.5f))) continue;
                    int idx = y * w + x;
                    if (outline.HasValue)
                    {
                        bool edge = false;
                        for (int k = 0; k < 10 && !edge; k++)
                        {
                            Vector2 a = pts[k];
                            Vector2 b = pts[(k + 1) % 10];
                            if (DistanceToSegment(new Vector2(x + 0.5f, y + 0.5f), a, b) < outer) edge = true;
                        }
                        px[idx] = edge ? outline.Value : color;
                    }
                    else px[idx] = color;
                }
            }
        }

        private static bool PointInPolygon(Vector2[] poly, Vector2 p)
        {
            bool inside = false;
            for (int i = 0, j = poly.Length - 1; i < poly.Length; j = i++)
            {
                if (((poly[i].y > p.y) != (poly[j].y > p.y)) &&
                    (p.x < (poly[j].x - poly[i].x) * (p.y - poly[i].y) / (poly[j].y - poly[i].y) + poly[i].x))
                    inside = !inside;
            }
            return inside;
        }

        private static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        public static Sprite MakeSprite(Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // ------------------------------------------------------------------
        //  Meshes
        // ------------------------------------------------------------------

        public static Mesh Cylinder(int sides, float radiusTop, float radiusBottom, float height, bool caps = true)
        {
            sides = Mathf.Max(3, sides);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            float half = height * 0.5f;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float s = Mathf.Sin(a), c = Mathf.Cos(a);
                verts.Add(new Vector3(c * radiusBottom, -half, s * radiusBottom));
                verts.Add(new Vector3(c * radiusTop, half, s * radiusTop));
                uvs.Add(new Vector2(i / (float)sides, 0));
                uvs.Add(new Vector2(i / (float)sides, 1));
            }
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                int b0 = i * 2, t0 = i * 2 + 1, b1 = n * 2, t1 = n * 2 + 1;
                tris.Add(b0); tris.Add(t0); tris.Add(t1);
                tris.Add(b0); tris.Add(t1); tris.Add(b1);
            }
            if (caps)
            {
                int centerBottom = verts.Count;
                verts.Add(new Vector3(0, -half, 0));
                uvs.Add(new Vector2(0.5f, 0.5f));
                int centerTop = verts.Count;
                verts.Add(new Vector3(0, half, 0));
                uvs.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i < sides; i++)
                {
                    int n = (i + 1) % sides;
                    // Caps must face outwards: bottom -Y, top +Y.
                    tris.Add(centerBottom); tris.Add(i * 2); tris.Add(n * 2);
                    tris.Add(centerTop); tris.Add(n * 2 + 1); tris.Add(i * 2 + 1);
                }
            }
            var mesh = new Mesh { name = "LP_Cylinder" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh Cone(int sides, float radius, float height)
        {
            sides = Mathf.Max(3, sides);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                verts.Add(new Vector3(Mathf.Cos(a) * radius, 0, Mathf.Sin(a) * radius));
                uvs.Add(new Vector2(i / (float)sides, 0));
            }
            int apex = verts.Count;
            verts.Add(new Vector3(0, height, 0));
            uvs.Add(new Vector2(0.5f, 1));
            int center = verts.Count;
            verts.Add(Vector3.zero);
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                tris.Add(i); tris.Add(apex); tris.Add(n);
                tris.Add(center); tris.Add(n); tris.Add(i);
            }
            var mesh = new Mesh { name = "LP_Cone" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh IcoSphere(int subdivisions)
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            Vector3[] baseVerts =
            {
                new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0),
                new Vector3(0, -1, t), new Vector3(0, 1, t), new Vector3(0, -1, -t), new Vector3(0, 1, -t),
                new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1)
            };
            int[] baseTris =
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
            };
            var verts = new List<Vector3>(baseVerts);
            for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
            var tris = new List<int>(baseTris);
            for (int s = 0; s < subdivisions; s++)
            {
                var midCache = new Dictionary<long, int>();
                var next = new List<int>();
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Midpoint(verts, a, b, midCache);
                    int bc = Midpoint(verts, b, c, midCache);
                    int ca = Midpoint(verts, c, a, midCache);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = next;
            }
            // Flat shaded: expand triangles.
            var fv = new List<Vector3>();
            var ft = new List<int>();
            for (int i = 0; i < tris.Count; i += 3)
            {
                int start = fv.Count;
                fv.Add(verts[tris[i]]);
                fv.Add(verts[tris[i + 1]]);
                fv.Add(verts[tris[i + 2]]);
                ft.Add(start); ft.Add(start + 1); ft.Add(start + 2);
            }
            var mesh = new Mesh { name = "LP_Ico" };
            mesh.SetVertices(fv);
            mesh.SetTriangles(ft, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int Midpoint(List<Vector3> verts, int a, int b, Dictionary<long, int> cache)
        {
            long key = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
            if (cache.TryGetValue(key, out int idx)) return idx;
            idx = verts.Count;
            verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
            cache[key] = idx;
            return idx;
        }

        public static Mesh Mountain(int seed, int sides, float radius, float height, float jag)
        {
            var rng = new System.Random(seed);
            sides = Mathf.Max(5, sides);
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            float apexX = (float)(rng.NextDouble() - 0.5) * radius * 0.22f;
            float apexZ = (float)(rng.NextDouble() - 0.5) * radius * 0.22f;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides;
                float r = radius * (0.86f + (float)rng.NextDouble() * jag);
                verts.Add(new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r));
                uvs.Add(new Vector2(i / (float)sides, 0));
            }
            int apex = verts.Count;
            verts.Add(new Vector3(apexX, height, apexZ));
            uvs.Add(new Vector2(0.5f, 1));
            int center = verts.Count;
            verts.Add(new Vector3(0, 0, 0));
            uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                // Sides: wound so normals face outwards (previous winding cancelled out).
                tris.Add(i); tris.Add(apex); tris.Add(n);
                // Base: faces down.
                tris.Add(center); tris.Add(i); tris.Add(n);
            }
            var mesh = new Mesh { name = "LP_Mountain" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Rounded, lumpy low-poly dirt pile used for hidden mine spots. Origin at the base.</summary>
        public static Mesh Mound(int seed, int sides, float radius, float height)
        {
            var rng = new System.Random(seed);
            sides = Mathf.Max(6, sides);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();

            var ring0 = new Vector3[sides];
            var ring1 = new Vector3[sides];
            var ring2 = new Vector3[sides];
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides + (float)(rng.NextDouble() - 0.5) * 0.10f;
                float j0 = 0.90f + (float)rng.NextDouble() * 0.16f;
                float j1 = 0.58f + (float)rng.NextDouble() * 0.14f;
                float j2 = 0.24f + (float)rng.NextDouble() * 0.12f;
                float y1 = height * (0.36f + (float)rng.NextDouble() * 0.10f);
                float y2 = height * (0.68f + (float)rng.NextDouble() * 0.10f);
                ring0[i] = new Vector3(Mathf.Cos(a) * radius * j0, 0f, Mathf.Sin(a) * radius * j0);
                ring1[i] = new Vector3(Mathf.Cos(a) * radius * j1, y1, Mathf.Sin(a) * radius * j1);
                ring2[i] = new Vector3(Mathf.Cos(a) * radius * j2, y2, Mathf.Sin(a) * radius * j2);
            }
            var apex = new Vector3((float)(rng.NextDouble() - 0.5) * radius * 0.12f,
                height * (0.94f + (float)rng.NextDouble() * 0.08f),
                (float)(rng.NextDouble() - 0.5) * radius * 0.12f);

            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i0 = verts.Count;
                verts.Add(a); verts.Add(b); verts.Add(c);
                // Project onto the XZ plane for the soil texture.
                uvs.Add(new Vector2(a.x / (radius * 2f) + 0.5f, a.z / (radius * 2f) + 0.5f));
                uvs.Add(new Vector2(b.x / (radius * 2f) + 0.5f, b.z / (radius * 2f) + 0.5f));
                uvs.Add(new Vector2(c.x / (radius * 2f) + 0.5f, c.z / (radius * 2f) + 0.5f));
                tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
            }

            for (int i = 0; i < sides; i++)
            {
                int n = (i + 1) % sides;
                // Outward-facing slopes.
                Tri(ring0[i], ring1[i], ring1[n]);
                Tri(ring0[i], ring1[n], ring0[n]);
                Tri(ring1[i], ring2[i], ring2[n]);
                Tri(ring1[i], ring2[n], ring1[n]);
                Tri(ring2[i], apex, ring2[n]);
            }

            var mesh = new Mesh { name = "LP_Mound" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh GableRoof(float width, float depth, float height)
        {
            float hw = width * 0.5f, hd = depth * 0.5f;
            Vector3[] v =
            {
                new Vector3(-hw, 0, -hd), new Vector3(hw, 0, -hd),
                new Vector3(-hw, 0, hd), new Vector3(hw, 0, hd),
                new Vector3(0, height, -hd), new Vector3(0, height, hd)
            };
            int[] t =
            {
                0,2,5, 0,5,4,       // left slope
                1,4,5, 1,5,3,       // right slope
                0,4,1,              // front gable
                2,3,5,              // back gable
                0,1,3, 0,3,2        // bottom
            };
            // Flat-shade the roof: shared corners would average the ridge and gable
            // normals, producing the smeared shading seen on the houses.
            var fv = new List<Vector3>(t.Length);
            var ft = new List<int>(t.Length);
            for (int i = 0; i < t.Length; i += 3)
            {
                int start = fv.Count;
                fv.Add(v[t[i]]); fv.Add(v[t[i + 1]]); fv.Add(v[t[i + 2]]);
                ft.Add(start); ft.Add(start + 1); ft.Add(start + 2);
            }
            var mesh = new Mesh { name = "LP_Roof" };
            mesh.SetVertices(fv);
            mesh.SetTriangles(ft, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh ClothMesh(int wSeg, int hSeg)
        {
            wSeg = Mathf.Max(2, wSeg);
            hSeg = Mathf.Max(2, hSeg);
            var verts = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>();
            int stride = wSeg + 1;
            int count = stride * (hSeg + 1);
            // Front and back use separate vertices so each side gets correct normals.
            for (int side = 0; side < 2; side++)
            {
                for (int y = 0; y <= hSeg; y++)
                {
                    for (int x = 0; x <= wSeg; x++)
                    {
                        float u = x / (float)wSeg;
                        float v = y / (float)hSeg;
                        verts.Add(new Vector3(u, v - 0.5f, 0));
                        uvs.Add(new Vector2(u, v));
                    }
                }
            }
            for (int y = 0; y < hSeg; y++)
            {
                for (int x = 0; x < wSeg; x++)
                {
                    int i0 = y * stride + x;
                    int i1 = i0 + 1;
                    int i2 = i0 + stride;
                    int i3 = i2 + 1;
                    tris.Add(i0); tris.Add(i2); tris.Add(i3);
                    tris.Add(i0); tris.Add(i3); tris.Add(i1);
                    // back faces (reversed winding)
                    tris.Add(count + i0); tris.Add(count + i3); tris.Add(count + i2);
                    tris.Add(count + i0); tris.Add(count + i1); tris.Add(count + i3);
                }
            }
            var mesh = new Mesh { name = "LP_Cloth" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh StarMesh(float outer, float inner, float depth)
        {
            var poly = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                float a = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? outer : inner;
                poly[i] = new Vector2(Mathf.Cos(a) * r, Mathf.Sin(a) * r);
            }
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            for (int i = 0; i < 10; i++)
            {
                verts.Add(new Vector3(poly[i].x, poly[i].y, -depth * 0.5f));
                uvs.Add(new Vector2(poly[i].x / outer * 0.5f + 0.5f, poly[i].y / outer * 0.5f + 0.5f));
            }
            for (int i = 0; i < 10; i++)
            {
                verts.Add(new Vector3(poly[i].x, poly[i].y, depth * 0.5f));
                uvs.Add(new Vector2(poly[i].x / outer * 0.5f + 0.5f, poly[i].y / outer * 0.5f + 0.5f));
            }
            int centerFront = verts.Count; verts.Add(new Vector3(0, 0, -depth * 0.5f)); uvs.Add(new Vector2(0.5f, 0.5f));
            int centerBack = verts.Count; verts.Add(new Vector3(0, 0, depth * 0.5f)); uvs.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i < 10; i++)
            {
                int n = (i + 1) % 10;
                tris.Add(centerFront); tris.Add(n); tris.Add(i);
                tris.Add(centerBack); tris.Add(i + 10); tris.Add(n + 10);
                tris.Add(i); tris.Add(n); tris.Add(n + 10);
                tris.Add(i); tris.Add(n + 10); tris.Add(i + 10);
            }
            var mesh = new Mesh { name = "LP_Star" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static Mesh RockMesh(int seed)
        {
            var mesh = IcoSphere(0);
            var verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 v = verts[i];
                // IcoSphere is flat-shaded, so the same corner appears several times.
                // Derive the lumpiness from the position (not a sequence) so duplicated
                // corners move together and triangles do not tear apart.
                float n1 = Mathf.PerlinNoise(seed * 0.137f + v.x * 2.7f + v.y * 1.3f,
                                             seed * 0.217f + v.z * 2.9f + v.y * 1.7f);
                float n2 = Mathf.PerlinNoise(seed * 0.071f + v.z * 3.1f,
                                             seed * 0.113f + v.x * 2.3f);
                float n = 0.78f + n1 * 0.30f + n2 * 0.12f;
                verts[i] = new Vector3(v.x * n, v.y * n * 0.62f, v.z * n);
            }
            mesh.vertices = verts;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
