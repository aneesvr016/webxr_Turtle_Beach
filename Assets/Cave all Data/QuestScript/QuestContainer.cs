using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion.XR.Shared.Rig;

public class QuestContainer : MonoBehaviour
{
    [Header("Quest Configuration")]
    public string questName;
    public string golemName;

    [Header("State Tracking")]
    public int totalPieces = 0;
    public int collectedPieces = 0;
    [HideInInspector] public bool isCompleted = false;

    [Header("Completion Effects")]
    public AudioClip completionSound;
    public GameObject completionParticlePrefab;

    private List<QuestPiece> pieces = new List<QuestPiece>();

    private void Awake()
    {
        // Find all QuestPiece components in children
        pieces.Clear();
        foreach (Transform child in transform)
        {
            var piece = child.GetComponent<QuestPiece>();
            if (piece != null)
            {
                pieces.Add(piece);
            }
        }
        totalPieces = pieces.Count;
    }

    public void OnPieceCollected(QuestPiece piece)
    {
        collectedPieces++;
        Debug.Log($"[QuestContainer] Piece collected! {collectedPieces}/{totalPieces} for Golem {golemName}.");

        if (collectedPieces >= totalPieces)
        {
            CompleteQuest();
        }
    }

    private void CompleteQuest()
    {
        Debug.Log($"[QuestContainer] Quest for {golemName} COMPLETED!");
        isCompleted = true; // Set session-completed flag

        // Play completion sound
        if (completionSound != null)
        {
            AudioSource.PlayClipAtPoint(completionSound, transform.position);
        }
        else
        {
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null) audioSource.Play();
        }

        // Spawn completion particles on the active Player (HardwareRig or DesktopRig)
        var rigs = FindObjectsOfType<HardwareRig>();
        HardwareRig localRig = null;
        foreach (var r in rigs)
        {
            if (r.gameObject.activeInHierarchy)
            {
                localRig = r;
                break;
            }
        }

        if (localRig != null && completionParticlePrefab != null)
        {
            // Instantiate particles on player position
            var pInstance = Instantiate(completionParticlePrefab, localRig.transform.position + Vector3.up * 1f, Quaternion.identity);
            
            // Parent particles to player so they follow the player as they move
            pInstance.transform.SetParent(localRig.transform);
            Destroy(pInstance, 5f); // Auto-cleanup after 5 seconds
            Debug.Log($"[QuestContainer] Spawned quest completion particles on Player: {localRig.name}");
        }
        else if (completionParticlePrefab != null)
        {
            // Fallback to Golem position if player rig is not found
            var golem = GameObject.Find(golemName);
            if (golem != null)
            {
                Instantiate(completionParticlePrefab, golem.transform.position + Vector3.up * 1f, Quaternion.identity);
            }
        }

        // Disable container
        gameObject.SetActive(false);
    }
}
