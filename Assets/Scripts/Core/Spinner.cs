using UnityEngine;

namespace BalkanMines
{
    public class Spinner : MonoBehaviour
    {
        public Vector3 Axis = Vector3.up;
        public float DegreesPerSecond = 18f;

        private void Update()
        {
            transform.Rotate(Axis, DegreesPerSecond * Time.unscaledDeltaTime, Space.Self);
        }
    }
}
