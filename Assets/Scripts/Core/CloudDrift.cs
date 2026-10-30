using UnityEngine;

namespace BalkanMines
{
    public class CloudDrift : MonoBehaviour
    {
        public float Speed = 0.6f;
        public float Wrap = 150f;
        public float Phase;

        private void Update()
        {
            var p = transform.position;
            p.x += Speed * Time.deltaTime;
            if (p.x > Wrap) p.x = -Wrap;
            p.y += Mathf.Sin(Time.time * 0.25f + Phase) * 0.002f;
            transform.position = p;
        }
    }
}
