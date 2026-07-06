using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion.XR.Shared.Rig;

namespace AB.TurtleBeach
{
    public class WebXREggQuestContainer : MonoBehaviour
    {
        public static WebXREggQuestContainer Instance { get; private set; }

        [Header("Quest Configuration")]
        public string questName = "Egg Collector";
        
        [Header("State Tracking")]
        public int totalEggs = 0;
        public int collectedEggs = 0;
        public bool isCompleted = false;

        [Header("Completion Effects")]
        public GameObject completionVFX;

        private List<WebXREggPiece> eggs = new List<WebXREggPiece>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            // Find all WebXREggPiece components in children
            InitializeEggs();
        }

        public void InitializeEggs()
        {
            if (eggs == null) eggs = new List<WebXREggPiece>();
            eggs.Clear();
            foreach (Transform child in transform)
            {
                // Egg might be nested or on the trigger child
                var piece = child.GetComponentInChildren<WebXREggPiece>(true);
                if (piece != null)
                {
                    eggs.Add(piece);
                }
            }
            totalEggs = eggs.Count;
            Debug.Log($"[WebXREggQuestContainer] Initialized Egg Quest: {totalEggs} eggs found.");
        }

        public void OnEggCollected(WebXREggPiece egg)
        {
            collectedEggs++;
            Debug.Log($"[WebXREggQuestContainer] Egg collected! Progress: {collectedEggs}/{totalEggs}");

            // Show a visual notification or text over player's camera if we want
            if (collectedEggs >= totalEggs && !isCompleted)
            {
                CompleteQuest();
            }
        }

        private void CompleteQuest()
        {
            Debug.Log("[WebXREggQuestContainer] CONGRATULATIONS! All eggs collected!");
            isCompleted = true;

            if (completionVFX != null)
            {
                completionVFX.SetActive(true);
            }

            // Find active player and play completion effect
            var rig = FindLocalRig();
            if (rig != null && completionVFX != null)
            {
                var vfx = Instantiate(completionVFX, rig.transform.position + Vector3.up * 1f, Quaternion.identity);
                vfx.transform.SetParent(rig.transform);
                Destroy(vfx, 6f);
            }
        }

        private HardwareRig FindLocalRig()
        {
            var rigs = FindObjectsOfType<HardwareRig>();
            foreach (var r in rigs)
            {
                if (r.gameObject.activeInHierarchy)
                {
                    return r;
                }
            }
            return null;
        }
    }
}
