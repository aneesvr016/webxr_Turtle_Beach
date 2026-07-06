using System.Collections;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;
using Fusion.XR.Shared.Rig;

namespace AB.TurtleBeach
{
    [RequireComponent(typeof(Collider))]
    public class WebXREggPiece : MonoBehaviour
    {
        public GameObject interactionCanvasPrefab;

        private WebXREggQuestContainer container;
        private GameObject interactionCanvas;
        private bool playerInside = false;
        private bool isCollected = false;

        // Auto-discovered variables
        private ParticleSystem particle;
        private GameObject eggVisual;
        private float respawnTime = 30f;

        private void Awake()
        {
            container = GetComponentInParent<WebXREggQuestContainer>();

            // Ensure collider is a trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // Auto-discover parameters from Visual Scripting variables
            DiscoverVariables();
        }

        private void Start()
        {
            SetupInteractionCanvas();
        }

        private void DiscoverVariables()
        {
            var vars = GetComponent<Variables>();
            if (vars != null)
            {
                try
                {
                    particle = vars.declarations.Get("Particle") as ParticleSystem;
                }
                catch { }

                try
                {
                    eggVisual = vars.declarations.Get("EggVisual") as GameObject;
                }
                catch { }

                try
                {
                    respawnTime = (float)vars.declarations.Get("RespawnTime");
                }
                catch { }
            }

            // Fallback discovery if variables are null/missing
            if (particle == null)
            {
                particle = GetComponentInChildren<ParticleSystem>(true);
            }
            if (eggVisual == null)
            {
                // Egg mesh is usually named "Eggs (7)" or similar in children
                var visualTrans = transform.Find("Eggs (7)") ?? transform.Find("Eggs");
                if (visualTrans != null)
                {
                    eggVisual = visualTrans.gameObject;
                }
            }
        }

        private void SetupInteractionCanvas()
        {
            if (interactionCanvasPrefab != null)
            {
                interactionCanvas = Instantiate(interactionCanvasPrefab, transform);
                interactionCanvas.name = "InteractionCanvas";
                interactionCanvas.transform.localPosition = new Vector3(0f, 0.4f, 0f);
                interactionCanvas.transform.localRotation = Quaternion.identity;
                interactionCanvas.transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);

                // Update text to [Collect Egg]
                var tmp = interactionCanvas.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "[Collect Egg]";
                }

                // Wire up click
                var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(Collect);
                }

                interactionCanvas.SetActive(false);
            }
        }

        private void Update()
        {
            if (playerInside && !isCollected && interactionCanvas != null && interactionCanvas.activeSelf)
            {
#if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                {
                    Collect();
                }
#else
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Collect();
                }
#endif
            }
        }

        public void Collect()
        {
            if (isCollected) return;

            isCollected = true;
            playerInside = false;

            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }

            // Play particles
            if (particle != null)
            {
                particle.gameObject.SetActive(true);
                particle.Play();
            }

            // Hide visual
            if (eggVisual != null)
            {
                eggVisual.SetActive(false);
            }

            // Notify container
            if (container != null)
            {
                container.OnEggCollected(this);
            }

            // Respawn routine
            StartCoroutine(RespawnSequence());
        }

        private IEnumerator RespawnSequence()
        {
            yield return new WaitForSeconds(respawnTime);

            // Reset state
            isCollected = false;
            if (eggVisual != null)
            {
                eggVisual.SetActive(true);
            }
            if (particle != null)
            {
                particle.Stop();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null && !isCollected)
            {
                playerInside = true;
                if (interactionCanvas != null && !interactionCanvas.activeSelf)
                {
                    interactionCanvas.SetActive(true);
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            HardwareRig rig = other.GetComponentInParent<HardwareRig>();
            if (rig != null)
            {
                playerInside = false;
                if (interactionCanvas != null)
                {
                    interactionCanvas.SetActive(false);
                }
            }
        }
    }
}
