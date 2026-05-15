using UnityEngine;

namespace MyTrafficSystem.Gameplay.FreeMode
{
    [DisallowMultipleComponent]
    public class FreeModeCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 thirdPersonOffset = new Vector3(0f, 3.1f, -6.2f);
        [SerializeField] private Vector3 firstPersonOffset = new Vector3(0f, 1.2f, 0.6f);
        [SerializeField] private float followSmooth = 8f;
        [SerializeField] private float rotateSmooth = 10f;
        [SerializeField] private KeyCode toggleViewKey = KeyCode.V;

        private bool firstPerson;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        private void LateUpdate()
        {
            if (target == null) return;

            if (Input.GetKeyDown(toggleViewKey))
            {
                firstPerson = !firstPerson;
            }

            Vector3 offset = firstPerson ? firstPersonOffset : thirdPersonOffset;
            Vector3 desiredPos = target.TransformPoint(offset);
            transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * Mathf.Max(1f, followSmooth));

            Quaternion desiredRot;
            if (firstPerson)
            {
                desiredRot = target.rotation;
            }
            else
            {
                Vector3 lookPoint = target.position + Vector3.up * 1.2f;
                desiredRot = Quaternion.LookRotation((lookPoint - transform.position).normalized, Vector3.up);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, Time.deltaTime * Mathf.Max(1f, rotateSmooth));
        }
    }
}
