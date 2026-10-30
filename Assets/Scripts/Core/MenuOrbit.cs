using UnityEngine;

namespace BalkanMines
{
    /// <summary>Slow orbit used by the main menu camera.</summary>
    public class MenuOrbit : MonoBehaviour
    {
        public Vector3 Target = new Vector3(0, 4f, 0);
        public float Radius = 17f;
        public float Height = 6.5f;
        public float Speed = 4.5f;
        public float LookHeight = 2.6f;

        private float _angle;

        private void Start()
        {
            _angle = 215f * Mathf.Deg2Rad;
            UpdateCamera(0f);
        }

        private void Update()
        {
            _angle += Time.unscaledDeltaTime * (Mathf.PI * 2f / Speed);
            UpdateCamera(Time.unscaledTime);
        }

        private void UpdateCamera(float t)
        {
            float r = Radius + Mathf.Sin(t * 0.23f) * 1.2f;
            var pos = Target + new Vector3(Mathf.Cos(_angle) * r, Height + Mathf.Sin(t * 0.5f) * 0.5f, Mathf.Sin(_angle) * r);
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(Target + Vector3.up * LookHeight - pos, Vector3.up);
        }
    }
}
