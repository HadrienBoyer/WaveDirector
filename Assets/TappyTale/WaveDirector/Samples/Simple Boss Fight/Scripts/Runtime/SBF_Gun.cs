using UnityEngine;

namespace TappyTale.Samples.SimpleBossFight
{
    public sealed class SBF_Gun : MonoBehaviour
    {
        [SerializeField] private Camera cam;
        [SerializeField] private float damage = 25f;
        [SerializeField] private float range = 40f;
        [SerializeField] private float fireCooldown = 0.15f;

        private float lastFireTime;

        private void Awake()
        {
            if (cam == null) cam = Camera.main;
        }

        private void Update()
        {
            if (cam == null) return;
            if (!Input.GetMouseButton(0)) return;
            if (Time.time < lastFireTime + fireCooldown) return;

            lastFireTime = Time.time;

            var ray = new Ray(cam.transform.position, cam.transform.forward);
            if (!Physics.Raycast(ray, out var hit, range)) return;

            var hp = hit.collider.GetComponentInParent<SBF_Health>();
            if (hp != null) hp.Damage(damage);
        }
    }
}
