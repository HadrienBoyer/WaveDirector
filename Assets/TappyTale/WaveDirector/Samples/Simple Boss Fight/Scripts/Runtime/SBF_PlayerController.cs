using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    [DisallowMultipleComponent]
    public sealed class SBF_PlayerController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private CharacterController characterController;

        private float yaw;
        private float pitch;

        private void Reset() => characterController = GetComponent<CharacterController>();

        private void Awake()
        {
            if (characterController == null) characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            yaw += Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -75f, 75f);

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (Camera.main != null) Camera.main.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 dir = (transform.right * h + transform.forward * v);
            if (dir.sqrMagnitude > 1f) dir.Normalize();

            Vector3 velocity = dir * moveSpeed;
            velocity.y = -2f;

            characterController.Move(velocity * Time.deltaTime);
        }
    }
}
