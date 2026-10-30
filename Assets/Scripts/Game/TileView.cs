using System.Collections;
using UnityEngine;

namespace BalkanMines
{
    /// <summary>One potential bomb spot. The tile itself only holds the earth block;
    /// flags, numbers and mines are spawned in world space by the level.</summary>
    public class TileView : MonoBehaviour
    {
        public int X;
        public int Z;
        public bool IsMine;
        public bool IsRevealed;
        public bool IsFlagged;
        public int Adjacent;
        public LevelManager Level;

        public GameObject FlagObject;
        public GameObject NumberObject;
        public GameObject MineObject;

        private MeshRenderer _renderer;
        private Collider _collider;
        private Material _hiddenMat;
        private Material _revealedMat;
        private Color _hiddenColor;
        private Color _revealedColor;
        private bool _hovered;
        private bool _animating;
        private Vector3 _fullScale;
        private Vector3 _flatScale;

        public float TopY => transform.position.y + 0.58f * transform.localScale.y;

        public void Setup(LevelManager level, int x, int z, LevelDefinition def, Texture2D soilTex)
        {
            Level = level;
            X = x;
            Z = z;
            _renderer = GetComponent<MeshRenderer>();
            _collider = GetComponent<Collider>();
            _fullScale = Vector3.one;
            _flatScale = new Vector3(1.05f, 0.28f, 1.05f);

            var baseMat = LP.TexturedMat(Color.white, soilTex, 0.02f, new Vector2(1f, 1f));
            _hiddenMat = new Material(baseMat);
            float variation = Random.Range(-0.06f, 0.08f);
            Color soil = Color.Lerp(new Color(0.55f, 0.42f, 0.27f), new Color(0.78f, 0.62f, 0.42f), Random.value);
            soil = Color.Lerp(soil, def.GrassAlt, 0.12f);
            _hiddenColor = new Color(
                Mathf.Clamp01(soil.r + variation),
                Mathf.Clamp01(soil.g + variation * 0.8f),
                Mathf.Clamp01(soil.b + variation * 0.5f));
            LP.SetColor(_hiddenMat, _hiddenColor);

            _revealedColor = Color.Lerp(new Color(0.58f, 0.49f, 0.36f), new Color(0.74f, 0.66f, 0.50f), Random.Range(0f, 1f));
            _revealedMat = new Material(LP.Mat(_revealedColor, 0.03f));
            LP.SetColor(_revealedMat, _revealedColor);

            _renderer.sharedMaterial = _hiddenMat;
            transform.localScale = _fullScale;
        }

        public void SetHover(bool hovered)
        {
            if (_hovered == hovered) return;
            _hovered = hovered;
            if (!IsRevealed && _hiddenMat != null)
                LP.SetColor(_hiddenMat, hovered ? Color.Lerp(_hiddenColor, Color.white, 0.22f) : _hiddenColor);
        }

        public IEnumerator RevealRoutine(bool isMine)
        {
            if (IsRevealed || _animating) yield break;
            _animating = true;
            IsRevealed = true;
            SetHover(false);
            if (_collider != null) _collider.enabled = true;

            Vector3 startScale = transform.localScale;
            Vector3 startPos = transform.localPosition;
            Vector3 endScale = _flatScale;
            Vector3 endPos = startPos;
            Color startColor = LP.GetColor(_hiddenMat, _hiddenColor);
            Color endColor = isMine ? new Color(0.22f, 0.17f, 0.14f) : _revealedColor;
            float dur = 0.28f;
            for (float t = 0f; t < 1f; t += Time.deltaTime / dur)
            {
                float e = Mathf.SmoothStep(0f, 1f, t);
                transform.localScale = Vector3.Lerp(startScale, endScale, e);
                transform.localPosition = Vector3.Lerp(startPos, endPos, e);
                LP.SetColor(_hiddenMat, Color.Lerp(startColor, endColor, e));
                yield return null;
            }
            transform.localScale = endScale;
            transform.localPosition = endPos;
            var finalMat = new Material(_revealedMat);
            LP.SetColor(finalMat, endColor);
            _renderer.sharedMaterial = finalMat;
            _animating = false;
        }

        public void ShowNumber(int n, Color color)
        {
            if (n <= 0 || NumberObject != null) return;
            // Numbers are painted flat on the cleared ground. No offset copies: from low
            // viewing angles a shadow copy would slide over the digit itself.
            var go = new GameObject("Number " + n);
            go.transform.SetParent(Level.transform, false);
            go.transform.localPosition = new Vector3(transform.position.x, 0.188f, transform.position.z);
            go.transform.localRotation = Quaternion.Euler(-90f, 180f, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.font = LP.Font;
            tm.text = n.ToString();
            tm.fontSize = 72;
            tm.characterSize = 0.098f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.fontStyle = FontStyle.Bold;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = LP.WorldTextMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            NumberObject = go;
        }

        public void ShowMine()
        {
            if (MineObject != null) return;
            var g = new GameObject("Mina");
            g.transform.SetParent(Level.transform, false);
            g.transform.localPosition = new Vector3(transform.position.x, 0.45f, transform.position.z);
            var body = LP.MeshObj("Mine Body", g.transform, LP.IcoSphere(0), LP.Mat(new Color(0.10f, 0.10f, 0.11f), 0.35f, 0.3f),
                Vector3.zero, Vector3.one * 0.58f);
            body.name = "Mine Body";
            var ring = LP.MeshObj("Mine Ring", g.transform, LP.Cylinder(10, 0.32f, 0.32f, 0.06f),
                LP.Mat(new Color(0.75f, 0.10f, 0.10f)), new Vector3(0f, 0.02f, 0f), Vector3.one);
            ring.name = "Mine Ring";
            var star = LP.MeshObj("Mine Star", g.transform, LP.StarMesh(0.16f, 0.07f, 0.04f),
                LP.Mat(new Color(0.86f, 0.16f, 0.12f)), new Vector3(0f, 0.42f, 0f), Vector3.one);
            star.AddComponent<Spinner>().DegreesPerSecond = 80f;
            var n = LP.MeshObj("Mine Spike", g.transform, LP.Cone(4, 0.08f, 0.22f),
                LP.Mat(new Color(0.2f, 0.2f, 0.2f)), new Vector3(0f, 0.25f, 0f), Vector3.one);
            n.name = "Mine Fuse";
            MineObject = g;
        }

        public void SetFlagVisual(bool on)
        {
            if (on && FlagObject == null)
            {
                var g = new GameObject("Zastava");
                g.transform.SetParent(Level.transform, false);
                g.transform.localPosition = new Vector3(transform.position.x, TopY, transform.position.z);
                var poleMat = LP.Mat(new Color(0.55f, 0.38f, 0.20f));
                LP.MeshObj("Flag Pole", g.transform, LP.Cylinder(5, 0.025f, 0.035f, 1.25f), poleMat,
                    new Vector3(0f, 0.62f, 0f), Vector3.one);
                var cloth = new GameObject("Flag Cloth");
                cloth.transform.SetParent(g.transform, false);
                cloth.transform.localPosition = new Vector3(0.02f, 1.02f, 0f);
                cloth.transform.localScale = new Vector3(0.44f, 0.28f, 1f);
                cloth.AddComponent<MeshFilter>().sharedMesh = LP.ClothMesh(5, 3);
                var mr = cloth.AddComponent<MeshRenderer>();
                mr.sharedMaterial = LP.Mat(new Color(0.78f, 0.09f, 0.11f));
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                var wave = cloth.AddComponent<FlagWave>();
                wave.Amplitude = 0.11f;
                wave.Speed = 3.2f;
                FlagObject = g;
                Level.StartCoroutine(PopFlag(g.transform));
                Level.PlayFlagSound();
            }
            else if (!on && FlagObject != null)
            {
                Object.Destroy(FlagObject);
                FlagObject = null;
            }
            IsFlagged = on;
        }

        private IEnumerator PopFlag(Transform t)
        {
            if (t == null) yield break;
            Vector3 target = t.localScale;
            t.localScale = Vector3.one * 0.01f;
            for (float f = 0f; f < 1f; f += Time.deltaTime / 0.18f)
            {
                if (t == null) yield break;
                float e = 1f - Mathf.Pow(1f - f, 3f);
                t.localScale = Vector3.Lerp(Vector3.one * 0.01f, target, e);
                yield return null;
            }
            if (t != null) t.localScale = target;
        }

        public void SetWrongFlag()
        {
            if (FlagObject != null)
            {
                foreach (var r in FlagObject.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = LP.Mat(new Color(0.25f, 0.24f, 0.22f));
            }
        }
    }
}
