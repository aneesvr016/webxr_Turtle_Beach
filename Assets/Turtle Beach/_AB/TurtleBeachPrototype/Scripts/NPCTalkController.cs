using System.Collections;
using UnityEngine;

namespace AB.TurtleBeach
{
    public class NPCTalkController : MonoBehaviour
    {
        [Header("Audio Settings")]
        public AudioSource audioSource;
        
        [Header("Animation Settings")]
        public Animator animator;
        public string talkingStateName = "Talking";
        public string idleStateName = "Idle(1)";
        
        [Header("UI Subtitles (Optional)")]
        [TextArea(3, 5)]
        public string subtitleText;
        
        private bool isSpeaking = false;
        private Coroutine talkCoroutine;

        private void Awake()
        {
            if (audioSource == null) audioSource = GetComponentInChildren<AudioSource>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public void PlayTalk()
        {
            if (isSpeaking) return;
            if (talkCoroutine != null) StopCoroutine(talkCoroutine);
            talkCoroutine = StartCoroutine(TalkSequence());
        }

        public void StopTalk()
        {
            if (!isSpeaking) return;
            if (talkCoroutine != null)
            {
                StopCoroutine(talkCoroutine);
                talkCoroutine = null;
            }
            ResetToIdle();
        }

        private IEnumerator TalkSequence()
        {
            isSpeaking = true;

            // Start Animation
            if (animator != null)
            {
                SetTalkingAnimParameter(true);
                animator.CrossFade(talkingStateName, 0.2f);
            }

            // Start Audio
            if (audioSource != null && audioSource.clip != null)
            {
                audioSource.Play();
                yield return new WaitForSeconds(audioSource.clip.length);
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            ResetToIdle();
        }

        private void ResetToIdle()
        {
            isSpeaking = false;
            if (audioSource != null) audioSource.Stop();
            
            if (animator != null)
            {
                SetTalkingAnimParameter(false);
                animator.CrossFade(idleStateName, 0.2f);
            }
        }

        private void SetTalkingAnimParameter(bool value)
        {
            if (animator == null) return;
            foreach (var param in animator.parameters)
            {
                if (param.name == "isTalking" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("isTalking", value);
                }
                else if (param.name == "talking" && param.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool("talking", value);
                }
            }
        }
    }
}