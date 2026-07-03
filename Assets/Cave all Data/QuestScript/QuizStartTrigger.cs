using UnityEngine;
using Fusion.XR.Shared.Rig;

[RequireComponent(typeof(Collider))]
public class QuizStartTrigger : MonoBehaviour
{
    private QuizScript quizScript;
    private bool isQuizActive = false;
    private GameObject interactionCanvas;
    private bool playerInside = false;

    private void Awake()
    {
        quizScript = GetComponentInParent<QuizScript>();
        
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (quizScript != null)
        {
            quizScript.onQuizFinish.AddListener(OnQuizFinished);
        }

        // Setup the interaction canvas link if pre-created
        Transform canvasT = transform.Find("InteractionCanvas");
        if (canvasT != null)
        {
            interactionCanvas = canvasT.gameObject;
            interactionCanvas.SetActive(false);
            
            var btn = interactionCanvas.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(Interact);
            }
        }
    }

    private void OnDestroy()
    {
        if (quizScript != null)
        {
            quizScript.onQuizFinish.RemoveListener(OnQuizFinished);
        }
    }

    private void OnQuizFinished()
    {
        isQuizActive = false;
    }

    private void Update()
    {
        if (playerInside && !isQuizActive && interactionCanvas != null && interactionCanvas.activeSelf)
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                Interact();
            }
#else
            if (Input.GetKeyDown(KeyCode.E))
            {
                Interact();
            }
#endif
        }
    }

    private void Interact()
    {
        if (quizScript != null && !isQuizActive)
        {
            Debug.Log("[QuizStartTrigger] Player interacted to start quiz.");
            isQuizActive = true;
            quizScript.StartQuiz();

            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null && quizScript != null && !isQuizActive)
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