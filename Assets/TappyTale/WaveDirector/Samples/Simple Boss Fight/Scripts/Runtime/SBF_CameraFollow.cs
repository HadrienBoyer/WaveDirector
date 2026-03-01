using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    public sealed class SBF_CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.6f, -3.2f);
        [SerializeField] private float positionLerp = 18f;

        public Transform Target { get => target; set => target = value; }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desired, 1f - Mathf.Exp(-positionLerp * Time.deltaTime));
        }
    }
}
