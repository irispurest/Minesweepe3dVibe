using UnityEngine;

namespace BalkanMines
{
    public class Bobbing : MonoBehaviour
    {
        public float Amplitude = 0.15f;
        public float Speed = 1.2f;

        private Vector3 _base;
        private float _phase;

        private void Start()
        {
            _base = transform.localPosition;
            _phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            var p = _base;
            p.y += Mathf.Sin(Time.unscaledTime * Speed + _phase) * Amplitude;
            transform.localPosition = p;
        }
    }
}
