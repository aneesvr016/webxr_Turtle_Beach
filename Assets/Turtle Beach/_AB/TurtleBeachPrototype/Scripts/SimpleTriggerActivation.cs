using UnityEngine;
using Fusion.XR.Shared.Rig;

namespace AB.TurtleBeach
{
    [RequireComponent(typeof(Collider))]
    public class SimpleTriggerActivation : MonoBehaviour
    {
        [Header("Target Activation Settings")]
        public GameObject[] targetsToActivate;
        public GameObject[] targetsToDeactivate;
        
        [Header("Trigger Settings")]
        public bool triggerOnce = true;
        
        private bool hasTriggered = false;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered && triggerOnce) return;

            // Check if player (HardwareRig) entered the trigger
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null)
            {
                TriggerAction();
            }
        }

        private void TriggerAction()
        {
            hasTriggered = true;
            Debug.Log($"[SimpleTriggerActivation] Triggered action on {gameObject.name}!");

            if (targetsToActivate != null)
            {
                foreach (var target in targetsToActivate)
                {
                    if (target != null)
                    {
                        target.SetActive(true);
                    }
                }
            }

            if (targetsToDeactivate != null)
            {
                foreach (var target in targetsToDeactivate)
                {
                    if (target != null)
                    {
                        target.SetActive(false);
                    }
                }
            }
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
        }
    }
}