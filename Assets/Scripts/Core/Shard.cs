using UnityEngine;

namespace BalkanMines
{
    /// <summary>Low-poly particle shard: velocity, spin, gravity and lifetime.</summary>
    public class Shard : MonoBehaviour
    {
        public Vector3 Velocity;
        public Vector3 Spin;
        public float Gravity = 9f;
        public float Life = 1f;
        public float Grow = 0f;
        public bool Shrink = true;

        private float _t;
        private Vector3 _startScale;

        private void Start()
        {
            _startScale = transform.localScale;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            _t += dt;
            Velocity.y -= Gravity * dt;
            transform.position += Velocity * dt;
            transform.Rotate(Spin * dt, Space.Self);
            float k = Mathf.Clamp01(_t / Mathf.Max(0.01f, Life));
            if (Grow > 0f)
                transform.localScale = _startScale * (1f + Grow * k);
            else if (Shrink)
                transform.localScale = _startScale * (1f - k);
            if (_t >= Life) Destroy(gameObject);
        }
    }
}
