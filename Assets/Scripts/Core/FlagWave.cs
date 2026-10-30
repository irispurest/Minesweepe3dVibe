using UnityEngine;

namespace BalkanMines
{
    /// <summary>Vertex-animated cloth used for every flag in the game.</summary>
    public class FlagWave : MonoBehaviour
    {
        public float Amplitude = 0.12f;
        public float Speed = 2.4f;

        private Mesh _mesh;
        private Vector3[] _base;
        private Vector3[] _verts;
        private float _phase;
        private float _time;

        private void Awake()
        {
            var mf = GetComponent<MeshFilter>();
            if (mf == null) return;
            _mesh = mf.mesh;
            _base = _mesh.vertices;
            _verts = new Vector3[_base.Length];
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            if (_mesh == null) return;
            _time += Time.unscaledDeltaTime * Speed;
            for (int i = 0; i < _base.Length; i++)
            {
                Vector3 v = _base[i];
                float x = Mathf.Clamp01(v.x);
                float y = v.y;
                float wave = Mathf.Sin(x * 5.2f - _time + _phase) * Amplitude * x;
                wave += Mathf.Sin(x * 2.1f + y * 3.0f - _time * 0.7f + _phase) * Amplitude * 0.35f * x;
                v.z += wave;
                v.y += Mathf.Sin(x * 3.0f - _time * 0.8f + _phase) * 0.015f * x;
                _verts[i] = v;
            }
            _mesh.vertices = _verts;
            _mesh.RecalculateNormals();
        }
    }
}
