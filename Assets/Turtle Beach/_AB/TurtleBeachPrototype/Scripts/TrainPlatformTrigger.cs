using UnityEngine;
using Fusion.XR.Shared.Rig;
using UnityEngine.AI;

namespace AB.TurtleBeach
{
    public class TrainPlatformTrigger : MonoBehaviour
    {
        [Tooltip("The actual moving transform with 1,1,1 scale that the player should be parented to.")]
        public Transform parentTarget;

        private void Awake()
        {
            // Ensure a trigger BoxCollider is present
            var boxCol = GetComponent<BoxCollider>();
            if (boxCol == null)
            {
                boxCol = gameObject.AddComponent<BoxCollider>();
            }
            boxCol.isTrigger = true;

            // CRITICAL for Unity trigger-to-trigger collision matrix with kinematic players:
            // We must have an active kinematic Rigidbody on the trigger platform.
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null)
            {
                Transform target = parentTarget != null ? parentTarget : transform;

                // Temporarily disable NavMeshAgent so the player moves with the train
                var agent = rig.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.enabled = false;
                }

                rig.transform.SetParent(target);
                Debug.Log($"[TrainPlatformTrigger] Parented player {rig.name} to {target.name} and disabled NavMeshAgent.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null)
            {
                Transform target = parentTarget != null ? parentTarget : transform;
                if (rig.transform.parent == target)
                {
                    rig.transform.SetParent(null);

                    // Re-enable NavMeshAgent upon leaving the train
                    var agent = rig.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.enabled = true;
                    }

                    Debug.Log($"[TrainPlatformTrigger] Unparented player {rig.name} from {target.name} and restored NavMeshAgent.");
                }
            }
        }

        private void OnDisable()
        {
            // Safety cleanup if the trigger is disabled/destroyed
            Transform target = parentTarget != null ? parentTarget : transform;
            var rigs = FindObjectsOfType<HardwareRig>();
            foreach (var rig in rigs)
            {
                if (rig != null && rig.transform.parent == target)
                {
                    rig.transform.SetParent(null);
                    var agent = rig.GetComponent<NavMeshAgent>();
                    if (agent != null)
                    {
                        agent.enabled = true;
                    }
                }
            }
        }
    }
}