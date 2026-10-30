using UnityEngine;

namespace BalkanMines
{
    /// <summary>Faces the camera while staying upright. Used for floating tile numbers.</summary>
    public class Billboard : MonoBehaviour
    {
        private Camera _cam;
        public float Bob = 0.04f;
        public float Speed = 1.5f;

        private Vector3 _basePos;
        private float _phase;

        private void Start()
        {
            _cam = Camera.main;
            _basePos = transform.localPosition;
            _phase = Random.Range(0f, 10f);
        }

        private void LateUpdate()
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return;
            transform.rotation = Quaternion.LookRotation(transform.position - _cam.transform.position, Vector3.up);
            if (Bob > 0f)
            {
                var p = _basePos;
                p.y += Mathf.Sin(Time.unscaledTime * Speed + _phase) * Bob;
                transform.localPosition = p;
            }
        }
    }
}
