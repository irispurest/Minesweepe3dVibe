using UnityEngine;
using UnityEngine.InputSystem;

namespace BalkanMines
{
    /// <summary>First-person explorer. Jump on a tile to open it; click LMB to plant a flag.</summary>
    public class PlayerController : MonoBehaviour
    {
        public float WalkSpeed = 4.6f;
        public float SprintSpeed = 6.5f;
        public float JumpSpeed = 6.2f;
        public float Gravity = -20f;
        public float MouseSensitivity = 0.14f;
        public float EyeHeight = 1.62f;

        public bool ControlEnabled { get; private set; }
        public LevelManager Level;
        public GameBootstrap Game;

        private Camera _cam;
        private CharacterController _cc;
        private float _yaw;
        private float _pitch = -21f;
        private Vector3 _velocity;
        private bool _wasGrounded;
        private float _airTime;
        private float _lastLanding = -1f;
        private TileView _hovered;
        private TileView _hoverCandidate;
        private float _hoverCandidateTime;
        private float _bob;
        private float _bobKick;
        private float _lookLockTimer;

        public void Init(GameBootstrap game, LevelManager level, Camera cam, float yaw)
        {
            Game = game;
            Level = level;
            _cam = cam;
            _cc = GetComponent<CharacterController>();
            _yaw = yaw;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
            _pitch = -21f;
            _cam.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            _cam.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            SetControl(false);
        }

        public void SnapCamera()
        {
            if (_cam == null) return;
            _cam.transform.localPosition = new Vector3(0f, EyeHeight, 0f);
            _cam.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        public void SetControl(bool enabled)
        {
            ControlEnabled = enabled;
            if (enabled)
            {
                SnapCamera();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                _lookLockTimer = 0.12f;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (_hovered != null) _hovered.SetHover(false);
                _hovered = null;
                if (Game != null) Game.UI.SetPrompt(false);
            }
        }

        private void Update()
        {
            if (!ControlEnabled) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb == null || mouse == null) return;

            if (_lookLockTimer > 0f) _lookLockTimer -= Time.unscaledDeltaTime;
            else
            {
                Vector2 delta = mouse.delta.ReadValue();
                _yaw += delta.x * MouseSensitivity;
                _pitch -= delta.y * MouseSensitivity;
                _pitch = Mathf.Clamp(_pitch, -82f, 82f);
            }

            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            float mx = 0f, mz = 0f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) mx -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) mx += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) mz -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) mz += 1f;
            Vector3 input = new Vector3(mx, 0f, mz);
            if (input.sqrMagnitude > 1f) input.Normalize();
            bool sprint = kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
            float speed = sprint ? SprintSpeed : WalkSpeed;

            bool grounded = _cc.isGrounded;
            if (grounded && _velocity.y < 0f) _velocity.y = -2f;
            if (grounded && kb.spaceKey.wasPressedThisFrame)
            {
                _velocity.y = JumpSpeed;
                _bobKick = 0.10f;
                Game.Audio.PlayJump();
            }

            _velocity.y += Gravity * Time.deltaTime;
            Vector3 worldMove = transform.right * input.x + transform.forward * input.z;
            _cc.Move((worldMove * speed + Vector3.up * _velocity.y) * Time.deltaTime);

            bool groundedNow = _cc.isGrounded;
            if (!groundedNow) _airTime += Time.deltaTime;
            if (!_wasGrounded && groundedNow)
            {
                if (_airTime > 0.06f) TryLand();
                _airTime = 0f;
                if (Mathf.Abs(_velocity.y) > 7f) _bobKick = 0.16f;
            }
            _wasGrounded = groundedNow;

            Game.Audio.TickFootsteps(input.sqrMagnitude > 0.05f, groundedNow, sprint ? 1.35f : 1f);

            // Camera bob and landing kick.
            _bob += Time.deltaTime * (input.sqrMagnitude > 0.05f && groundedNow ? (sprint ? 13f : 9f) : 2f);
            _bobKick = Mathf.Lerp(_bobKick, 0f, Time.deltaTime * 7f);
            float bobY = Mathf.Sin(_bob) * (input.sqrMagnitude > 0.05f ? 0.028f : 0.008f);
            _cam.transform.localPosition = new Vector3(0f, EyeHeight + bobY - _bobKick, 0f);
            _cam.transform.localRotation = Quaternion.Euler(_pitch + Mathf.Sin(_bob * 0.5f) * 0.6f, 0f, 0f);

            UpdateHover();
            if (mouse.leftButton.wasPressedThisFrame && _hovered != null)
            {
                Level.ToggleFlag(_hovered);
            }
        }

        private void UpdateHover()
        {
            TileView found = null;
            Ray ray = new Ray(_cam.transform.position, _cam.transform.forward);
            if (Physics.Raycast(ray, out var hit, 9.0f, ~0, QueryTriggerInteraction.Ignore))
            {
                found = hit.collider.GetComponent<TileView>();
                // Aiming into the gap between mounds used to clear the hover every
                // other frame, which made the LMB prompt and pile highlight flicker.
                // Snap to the nearest tile for ground/fence hits instead.
                if (found == null) found = Level != null ? Level.TileAtWorld(hit.point) : null;
            }

            // Hysteresis: only switch highlight after the new target is stable for a
            // moment. Walking over mound edges alternates between tile/gap targets,
            // and this keeps both the prompt and the tile tint from strobing.
            if (found != _hoverCandidate)
            {
                _hoverCandidate = found;
                _hoverCandidateTime = 0f;
            }
            else
            {
                _hoverCandidateTime += Time.unscaledDeltaTime;
            }

            if (_hoverCandidate != _hovered && _hoverCandidateTime >= 0.12f)
            {
                if (_hovered != null) _hovered.SetHover(false);
                _hovered = _hoverCandidate;
                if (_hovered != null) _hovered.SetHover(true);
            }
            if (Game != null) Game.UI.SetPrompt(_hovered != null && !_hovered.IsRevealed);
        }

        private void TryLand()
        {
            if (Time.time - _lastLanding < 0.18f) return;
            Ray ray = new Ray(transform.position + Vector3.up * 0.55f, Vector3.down);
            if (!Physics.Raycast(ray, out var hit, 1.75f, ~0, QueryTriggerInteraction.Ignore)) return;
            var tile = hit.collider.GetComponent<TileView>();
            if (tile == null) return;

            _lastLanding = Time.time;
            bool wasRevealed = tile.IsRevealed;
            // Only new dirt produces the heavy landing dip. Hopping on an already
            // cleared tile should not replay the dig-down camera shake.
            if (!wasRevealed) _bobKick = 0.20f;
            bool bounce = Level.HandleLanding(tile);
            if (bounce)
            {
                _velocity.y = 5.4f;
                _bobKick = 0.24f;
                _cc.Move(Vector3.up * 0.06f);
            }
        }
    }
}
