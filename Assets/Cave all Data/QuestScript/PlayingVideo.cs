using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;


public class PlayingVideo : MonoBehaviour
{
    [SerializeField] private bool CanAutoPlay = false;
    [SerializeField] private GameObject Canvas;
    [SerializeField] private List<VideoData> videoDataList;
    [SerializeField] private Image displayImage;
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private GameObject SubtitleCanvas;
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource AmbiantAudioSource;
   // [SerializeField] private SpatialPointOfInterest interest;
    [SerializeField] private GameObject questPiecesContainer;

    [Header("Animation Settings")]
    [SerializeField] private AnimationClip[] TalkingAnim;
    [SerializeField] private string IdleState = "Idle";
    [SerializeField] private string WaveState = "Wave";
    [SerializeField] private string TalkingState = "Talking";
    [SerializeField] private float WaveDuration = 2f;

    private AnimatorOverrideController overrideController;
    private Coroutine videoCoroutine;
    private Coroutine talkingCoroutine;

    private bool isPlaying = false;
    private int currentIndex = 0;
    private float timer = 0f;

    // 🔹 ADDED (SAFE)
    private List<KeyValuePair<AnimationClip, AnimationClip>> overrides;
    private AnimationClip talkingBaseClip;

    private void Awake()
    {
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);
        animator.runtimeAnimatorController = overrideController;

        // 🔹 CACHE OVERRIDES ONCE
        overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        overrideController.GetOverrides(overrides);

        // 🔹 FIND TALKING BASE CLIP
        foreach (var kvp in overrides)
        {
            if (kvp.Key.name == "Talking") // MUST match Animator clip name
            {
                talkingBaseClip = kvp.Key;
                break;
            }
        }

        if (talkingBaseClip == null)
            Debug.LogError("Talking clip not found in Animator!");
    }

    private void Start()
    {
        if (CanAutoPlay)
            Play();
    }

    public void OnPlayButtonPressed()
    {
        if (isPlaying)
            Pause();
        else
            Play();
    }

    public void Play()
    {
        if (videoDataList == null || videoDataList.Count == 0)
            return;

        if (videoCoroutine == null)
            videoCoroutine = StartCoroutine(PlayVideoDataSequence());

        isPlaying = true;

       // if (interest) interest.gameObject.SetActive(true);
        if (AmbiantAudioSource) AmbiantAudioSource.volume = 0.1f;
        if (audioSource) audioSource.Play();
        if (Canvas) Canvas.SetActive(true);
        if (SubtitleCanvas) SubtitleCanvas.SetActive(true);

        if (talkingCoroutine == null && TalkingAnim.Length > 0)
            talkingCoroutine = StartCoroutine(TalkingOverrideLoop());

        if (questPiecesContainer != null)
        {
            var container = questPiecesContainer.GetComponent<QuestContainer>();
            if (container != null && !container.isCompleted)
            {
                questPiecesContainer.SetActive(true);
                Debug.Log($"[PlayingVideo] Talk started! Enabled pieces container: {questPiecesContainer.name}");
            }
        }
    }

    public void Pause()
    {
        isPlaying = false;

        if (AmbiantAudioSource) AmbiantAudioSource.volume = 0.3f;
        if (audioSource) audioSource.Pause();

        StopTalking();
    }

    public void ResetPlayback()
    {
        if (videoCoroutine != null)
        {
            StopCoroutine(videoCoroutine);
            videoCoroutine = null;
        }

        isPlaying = false;
        currentIndex = 0;
        timer = 0f;

        StopTalking();

        if (audioSource)
        {
            audioSource.Stop();
            audioSource.time = 0f;
        }

        if (displayImage) displayImage.sprite = null;
        if (displayText) displayText.text = "";

        if (Canvas) Canvas.SetActive(false);
        if (SubtitleCanvas) SubtitleCanvas.SetActive(false);
       // if (interest) interest.gameObject.SetActive(false);

        if (AmbiantAudioSource) AmbiantAudioSource.volume = 0.3f;

        StartCoroutine(PlayWaveThenIdle());
    }

    private IEnumerator PlayVideoDataSequence()
    {
        while (currentIndex < videoDataList.Count)
        {
            var videoData = videoDataList[currentIndex];

            if (displayImage)
                displayImage.sprite = videoData.image;

            if (displayText)
                displayText.text = videoData.textInformation;

            //if (interest)
               // UpdatePointOfInterestDescription(videoData.textInformation);

            timer = 0f;

            while (timer < videoData.time)
            {
                if (!isPlaying)
                {
                    yield return null;
                    continue;
                }

                timer += Time.deltaTime;
                yield return null;
            }

            currentIndex++;
        }

        FinishPlayback();
    }

    private void FinishPlayback()
    {
        isPlaying = false;
        currentIndex = 0;
        videoCoroutine = null;

        StopTalking();

        if (displayImage) displayImage.sprite = null;
        if (displayText) displayText.text = "";

       // if (interest)
           // UpdatePointOfInterestDescription("Completed");

        if (AmbiantAudioSource) AmbiantAudioSource.volume = 0.3f;
        if (SubtitleCanvas) SubtitleCanvas.SetActive(false);

        if (Canvas)
        {
            displayImage.sprite = videoDataList[0].image;
            Canvas.SetActive(false);
        }

        if (audioSource) audioSource.Stop();
    }

    // =========================
    // TALKING OVERRIDE LOGIC
    // =========================

    private IEnumerator TalkingOverrideLoop()
    {
        animator.CrossFade(TalkingState, 0.1f);

        while (isPlaying)
        {
            AnimationClip clip =
                TalkingAnim[UnityEngine.Random.Range(0, TalkingAnim.Length)];

            // 🔹 APPLY OVERRIDE CORRECTLY
            for (int i = 0; i < overrides.Count; i++)
            {
                if (overrides[i].Key == talkingBaseClip)
                {
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(
                        talkingBaseClip,
                        clip
                    );
                    break;
                }
            }

            overrideController.ApplyOverrides(overrides);

            animator.Play(TalkingState, 0, 0f);

            // 🔹 WAIT FOR CURRENT TALKING CLIP TO FINISH
            yield return new WaitForSeconds(clip.length);
        }
    }

    private void StopTalking()
    {
        if (talkingCoroutine != null)
        {
            StopCoroutine(talkingCoroutine);
            talkingCoroutine = null;
        }

        animator.Play(IdleState);
    }

    private IEnumerator PlayWaveThenIdle()
    {
        animator.Play(WaveState);

        yield return new WaitForSeconds(WaveDuration);

        animator.Play(IdleState);
    }

    // =========================
    // POINT OF INTEREST
    // =========================

    private void RefreshPointOfInterest()
    {
       // if (!interest) return;

       // interest.gameObject.SetActive(false);
       // interest.gameObject.SetActive(true);

        //var tmps = interest.GetComponentsInChildren<TextMeshProUGUI>();
      //  foreach (var t in tmps)
          //  t.text = interest.description;

       // var texts = interest.GetComponentsInChildren<Text>();
       // foreach (var t in texts)
            //t.text = interest.description;
    }

    private void UpdatePointOfInterestDescription(string newDescription)
    {
      //  if (!interest) return;

       // interest.description = newDescription;
        RefreshPointOfInterest();
    }
    public void SkipCurrentSegment()
    {
        if (!isPlaying)
            return;

        if (currentIndex >= videoDataList.Count)
            return;

        // 🔹 Skip subtitle/image timer
        timer = videoDataList[currentIndex].time;

        // 🔹 Skip audio by the SAME duration
        if (audioSource && audioSource.clip != null)
        {
            float skipTime = videoDataList[currentIndex].time;
            audioSource.time = Mathf.Min(
                audioSource.time + skipTime,
                audioSource.clip.length
            );
        }
    }
}

[Serializable]
public class VideoData
{
    public float time;
    public Sprite image;
    public string textInformation;
}
