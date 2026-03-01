using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TappyTale.Samples.SimpleBossFight
{
    [DisallowMultipleComponent]
    public sealed class SBF_PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private bool lockAndHideCursor = true;

        private float yaw;
        private float pitch;

        private void Reset() => characterController = GetComponent<CharacterController>();

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            ApplyCursorState();
        }

        private void OnDisable()
        {
            ReleaseCursorState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            ApplyCursorState();
        }

        private void Update()
        {
            Vector2 look = ReadLookDelta();
            yaw += look.x * lookSensitivity;
            pitch -= look.y * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -75f, 75f);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (Camera.main != null && Camera.main.GetComponent<SBF_CameraFollow>() == null)
            {
                Camera.main.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Vector2 move = ReadMoveInput();
            float h = move.x;
            float v = move.y;

            Vector3 dir = (transform.right * h + transform.forward * v);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            Vector3 velocity = dir * moveSpeed;
            velocity.y = -2f;

            characterController.Move(velocity * Time.deltaTime);
        }

        private void ApplyCursorState()
        {
            if (!lockAndHideCursor) return;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ReleaseCursorState()
        {
            if (!lockAndHideCursor) return;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private static Vector2 ReadLookDelta()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.delta.ReadValue() * Time.deltaTime * 60f : Vector2.zero;
#else
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
        }

        private static Vector2 ReadMoveInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return Vector2.zero;
            float x = 0f;
            float y = 0f;
            if (Keyboard.current.aKey.isPressed) x -= 1f;
            if (Keyboard.current.dKey.isPressed) x += 1f;
            if (Keyboard.current.sKey.isPressed) y -= 1f;
            if (Keyboard.current.wKey.isPressed) y += 1f;
            return new Vector2(x, y);
#else
            return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif
        }
    }
}
