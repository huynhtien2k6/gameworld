using UnityEngine;

namespace GameWorld.UnityClient
{
    [DisallowMultipleComponent]
    public sealed class GameWorldCameraController : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 6.5f, -8f);
        [SerializeField] private float followSpeed = 7f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float zoomSpeed = 15f;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 15f;

        private float pitch = 32f;
        private float yaw;
        private float distance = 8f;

        private void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }

            distance = Mathf.Clamp(offset.magnitude, minDistance, maxDistance);
            yaw = transform.eulerAngles.y;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
            }

            if (Input.GetMouseButton(1))
            {
                yaw += Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                pitch -= Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;
                pitch = Mathf.Clamp(pitch, 10f, 70f);
            }

            var desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
            var desiredPosition = target.position + desiredRotation * new Vector3(0f, 0f, -distance);
            desiredPosition += Vector3.up * 2.5f;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, followSpeed * Time.deltaTime);
        }

        public void BindTarget(Transform playerTransform)
        {
            target = playerTransform;
        }
    }
}
