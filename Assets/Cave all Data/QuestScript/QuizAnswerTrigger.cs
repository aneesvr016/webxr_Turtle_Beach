using UnityEngine;
using Fusion.XR.Shared.Rig;

[RequireComponent(typeof(Collider))]
public class QuizAnswerTrigger : MonoBehaviour
{
    [Tooltip("The answer character corresponding to this spot (A, B, C, or D)")]
    public string answerChar;

    private QuizScript quizScript;
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

        // Setup interaction canvas link if pre-created
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

    private void Update()
    {
        if (playerInside && quizScript != null && !string.IsNullOrEmpty(answerChar) && interactionCanvas != null && interactionCanvas.activeSelf)
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
        if (quizScript != null && !string.IsNullOrEmpty(answerChar))
        {
            Debug.Log($"[QuizAnswerTrigger] Player selected answer: {answerChar}");
            quizScript.SelectAnswer(answerChar.ToUpper());
            
            if (interactionCanvas != null)
            {
                interactionCanvas.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HardwareRig rig = other.GetComponentInParent<HardwareRig>();
        if (rig != null && quizScript != null && !string.IsNullOrEmpty(answerChar))
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