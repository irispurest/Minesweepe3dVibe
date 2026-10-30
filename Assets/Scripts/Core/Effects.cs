using System.Collections;
using UnityEngine;

namespace BalkanMines
{
    /// <summary>Explosions, dust, fireworks, confetti and camera shake.</summary>
    public class Effects : MonoBehaviour
    {
        private GameBootstrap _game;
        private Camera _cam;
        private float _shakeTime;
        private float _shakeDur;
        private float _shakeAmp;
        private Vector3 _shakeBasePos;
        private Quaternion _shakeBaseRot;

        public void Init(GameBootstrap game)
        {
            _game = game;
            _cam = game.Cam;
        }

        public void Dust(Vector3 pos, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = LP.Prim(PrimitiveType.Cube, "Dust", transform, pos + Random.insideUnitSphere * 0.35f,
                    Vector3.one * Random.Range(0.05f, 0.16f), LP.Mat(color));
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var shard = go.AddComponent<Shard>();
                shard.Velocity = new Vector3(Random.Range(-1.3f, 1.3f), Random.Range(0.6f, 2.2f), Random.Range(-1.3f, 1.3f));
                shard.Gravity = 4f;
                shard.Life = Random.Range(0.35f, 0.7f);
                shard.Spin = Random.insideUnitSphere * 240f;
            }
        }

        public void Explosion(Vector3 pos)
        {
            Shake(0.45f, 1.0f);
            Color[] hot = { new Color(1f, 0.85f, 0.25f), new Color(1f, 0.45f, 0.08f), new Color(0.9f, 0.15f, 0.06f) };
            for (int i = 0; i < 34; i++)
            {
                var go = LP.Prim(PrimitiveType.Cube, "Explosion", transform, pos,
                    Vector3.one * Random.Range(0.10f, 0.34f), LP.Mat(hot[i % hot.Length]));
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var shard = go.AddComponent<Shard>();
                shard.Velocity = Random.insideUnitSphere * Random.Range(3f, 9f) + Vector3.up * 2.5f;
                shard.Gravity = 11f;
                shard.Life = Random.Range(0.5f, 1.2f);
                shard.Spin = Random.insideUnitSphere * 360f;
            }

            for (int i = 0; i < 12; i++)
            {
                var go = LP.Prim(PrimitiveType.Sphere, "Smoke", transform, pos + Random.insideUnitSphere * 0.8f,
                    Vector3.one * Random.Range(0.7f, 1.6f), LP.Mat(new Color(0.16f, 0.14f, 0.12f), 0.0f));
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var shard = go.AddComponent<Shard>();
                shard.Velocity = Random.insideUnitSphere * 2.2f + Vector3.up * Random.Range(0.8f, 2.5f);
                shard.Gravity = -0.8f;
                shard.Life = Random.Range(1.2f, 2.2f);
                shard.Grow = 1.7f;
                shard.Spin = Random.insideUnitSphere * 40f;
            }

            StartCoroutine(Flash(pos, new Color(1f, 0.75f, 0.25f, 0.95f), 9f, 0.65f));
        }

        public void Fireworks(Vector3 center, int bursts)
        {
            StartCoroutine(FireworksRoutine(center, bursts));
        }

        private IEnumerator FireworksRoutine(Vector3 center, int bursts)
        {
            Color[] palette =
            {
                new Color(1f, 0.85f, 0.2f), new Color(1f, 0.25f, 0.2f),
                new Color(0.3f, 0.7f, 1f), new Color(0.4f, 1f, 0.45f),
                new Color(0.9f, 0.4f, 1f), new Color(1f, 1f, 1f)
            };
            for (int b = 0; b < bursts; b++)
            {
                var p = center + new Vector3(Random.Range(-10f, 10f), Random.Range(9f, 19f), Random.Range(-10f, 10f));
                var color = palette[b % palette.Length];
                for (int i = 0; i < 30; i++)
                {
                    var go = LP.Prim(PrimitiveType.Cube, "Firework", transform, p,
                        Vector3.one * Random.Range(0.06f, 0.18f), LP.Unlit(color));
                    go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    var shard = go.AddComponent<Shard>();
                    shard.Velocity = Random.onUnitSphere * Random.Range(3.5f, 7.5f);
                    shard.Gravity = 4.5f;
                    shard.Life = Random.Range(1.1f, 1.9f);
                    shard.Spin = Random.insideUnitSphere * 360f;
                }
                StartCoroutine(Flash(p, new Color(color.r, color.g, color.b, 0.9f), 6f, 0.55f));
                yield return new WaitForSeconds(0.32f);
            }
        }

        public void Confetti(Vector3 center)
        {
            Color[] palette =
            {
                new Color(0.78f, 0.10f, 0.12f), new Color(0.95f, 0.95f, 0.90f),
                new Color(0.08f, 0.22f, 0.55f), new Color(0.95f, 0.78f, 0.20f)
            };
            for (int i = 0; i < 120; i++)
            {
                var p = center + new Vector3(Random.Range(-9f, 9f), Random.Range(8f, 16f), Random.Range(-9f, 9f));
                var go = LP.Prim(PrimitiveType.Cube, "Confetti", transform, p,
                    new Vector3(Random.Range(0.05f, 0.12f), Random.Range(0.10f, 0.22f), 0.02f), LP.Unlit(palette[i % palette.Length]));
                go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var shard = go.AddComponent<Shard>();
                shard.Velocity = new Vector3(Random.Range(-2f, 2f), Random.Range(-1f, 2f), Random.Range(-2f, 2f));
                shard.Gravity = 3.2f;
                shard.Life = Random.Range(4f, 7f);
                shard.Spin = Random.insideUnitSphere * 280f;
                shard.Grow = 1f;
                shard.Shrink = false;
            }
        }

        private IEnumerator Flash(Vector3 pos, Color color, float size, float dur)
        {
            var go = new GameObject("Flash");
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LP.MakeSprite(LP.RadialTexture("bang", 128, Color.white, new Color(1f, 1f, 1f, 0f), 2.4f));
            sr.color = color;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float e = t / dur;
                go.transform.localScale = Vector3.one * size * (0.6f + e * 0.9f);
                var c = color;
                c.a = Mathf.Lerp(color.a, 0f, e);
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        public void Shake(float amount, float duration)
        {
            if (_cam == null) return;
            _shakeAmp = amount;
            _shakeDur = duration;
            _shakeTime = duration;
            _shakeBasePos = _cam.transform.localPosition;
            _shakeBaseRot = _cam.transform.localRotation;
        }

        private void LateUpdate()
        {
            if (_cam == null || _shakeTime <= 0f) return;
            _shakeTime -= Time.deltaTime;
            float k = Mathf.Clamp01(_shakeTime / Mathf.Max(0.001f, _shakeDur));
            float amp = _shakeAmp * k;
            _cam.transform.localPosition = _shakeBasePos + Random.insideUnitSphere * amp * 0.22f;
            _cam.transform.localRotation = _shakeBaseRot * Quaternion.Euler(
                Random.Range(-amp, amp) * 3f, Random.Range(-amp, amp) * 3f, Random.Range(-amp, amp) * 4f);
            if (_shakeTime <= 0f)
            {
                _cam.transform.localPosition = _shakeBasePos;
                _cam.transform.localRotation = _shakeBaseRot;
            }
        }
    }
}
