using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class SharkQuizManager : MonoBehaviour
{
    [System.Serializable]
    public class QuizQuestion
    {
        public string questionText;
        public string optionA;
        public string optionB;
        public string optionC;
        public string optionD;
        public char correctAnswer; // 'A', 'B', 'C', or 'D'

        public QuizQuestion(string q, string a, string b, string c, string d, char correct)
        {
            questionText = q;
            optionA = a;
            optionB = b;
            optionC = c;
            optionD = d;
            correctAnswer = correct;
        }
    }

    [Header("UI Reference")]
    public TextMeshProUGUI quizDisplay; // SetupForText/QuizText

    [Header("Audio Clips")]
    public AudioClip correctAudio;
    public AudioClip wrongAudio;

    private AudioSource audioSource;
    private List<QuizQuestion> questions = new List<QuizQuestion>();
    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isQuizActive = false;
    private bool isTransitioning = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Try to auto-load the standard sound assets if not manually set in Editor
#if UNITY_EDITOR
        if (correctAudio == null)
        {
            correctAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Cave all Data/AudioClips/PointUP.mp3");
        }
        if (wrongAudio == null)
        {
            wrongAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Cave all Data/AudioClips/Wrong Buzzer.mp3");
        }
#endif

        // Initialize our high-quality, scientifically accurate quiz database
        InitializeQuestions();

        // Find the text mesh automatically if not set in the inspector
        if (quizDisplay == null)
        {
            GameObject textObj = GameObject.Find("QuizText");
            if (textObj == null) textObj = GameObject.Find("Shark data/The Great White Shark/Class/screen_black_0/SetupForText/QuizText");
            if (textObj != null)
            {
                quizDisplay = textObj.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void Start()
    {
        StartQuiz();
    }

    private void InitializeQuestions()
    {
        questions.Add(new QuizQuestion(
            "Which shark is the largest predatory fish in the world?",
            "A) Hammerhead Shark",
            "B) Great White Shark",
            "C) Mako Shark",
            "D) Whale Shark",
            'B'
        ));

        questions.Add(new QuizQuestion(
            "Which shark is known for its incredible speed of up to 45 mph?",
            "A) Great White Shark",
            "B) Hammerhead Shark",
            "C) Mako Shark",
            "D) Whale Shark",
            'C'
        ));

        questions.Add(new QuizQuestion(
            "Which shark is a filter feeder and primarily eats plankton?",
            "A) Great White Shark",
            "B) Hammerhead Shark",
            "C) Mako Shark",
            "D) Whale Shark",
            'D'
        ));

        questions.Add(new QuizQuestion(
            "Which shark has the largest brain-to-body ratio and advanced sensory mapping?",
            "A) Great White Shark",
            "B) Hammerhead Shark",
            "C) Mako Shark",
            "D) Whale Shark",
            'B'
        ));

        questions.Add(new QuizQuestion(
            "Carcharodon carcharias is the scientific name for which shark?",
            "A) Great White Shark",
            "B) Hammerhead Shark",
            "C) Mako Shark",
            "D) Whale Shark",
            'A'
        ));

        questions.Add(new QuizQuestion(
            "Which shark has small, flat teeth that are designed for crushing shells?",
            "A) Great White Shark",
            "B) Hammerhead Shark",
            "C) Mako Shark",
            "D) Whale Shark",
            'B'
        ));
    }

    public void StartQuiz()
    {
        currentQuestionIndex = 0;
        score = 0;
        isQuizActive = true;
        isTransitioning = false;
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        if (quizDisplay == null || currentQuestionIndex >= questions.Count) return;

        QuizQuestion q = questions[currentQuestionIndex];
        
        // Format the question beautifully for our modern classroom screen
        quizDisplay.text = $"<color=#5ac2ff><b>Question {currentQuestionIndex + 1}/{questions.Count}:</b></color>\n" +
                           $"{q.questionText}\n\n" +
                           $"<color=#ffffff>{q.optionA}    {q.optionB}</color>\n" +
                           $"<color=#ffffff>{q.optionC}    {q.optionD}</color>";

        isTransitioning = false;
    }

    public void OnSelectOption(char optionLabel)
    {
        if (!isQuizActive || isTransitioning) return;

        isTransitioning = true;
        QuizQuestion q = questions[currentQuestionIndex];

        bool isCorrect = (optionLabel == q.correctAnswer);
        if (isCorrect)
        {
            score++;
            if (audioSource != null && correctAudio != null)
            {
                audioSource.PlayOneShot(correctAudio);
            }
            quizDisplay.text = $"<color=#5eff73><b>CORRECT ANSWER!</b></color>\n\n" +
                               $"The correct option was indeed <color=#5eff73><b>{q.correctAnswer}</b></color>.\n" +
                               $"Great job! Moving to the next question...";
        }
        else
        {
            if (audioSource != null && wrongAudio != null)
            {
                audioSource.PlayOneShot(wrongAudio);
            }
            quizDisplay.text = $"<color=#ff5e5e><b>WRONG ANSWER!</b></color>\n\n" +
                               $"You selected option <color=#ff5e5e><b>{optionLabel}</b></color>.\n" +
                               $"The correct answer was <color=#5eff73><b>{q.correctAnswer}</b></color>.\n" +
                               $"Moving on...";
        }

        StartCoroutine(NextQuestionDelay());
    }

    private IEnumerator NextQuestionDelay()
    {
        yield return new WaitForSeconds(3.0f);

        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Count)
        {
            ShowQuestion();
        }
        else
        {
            ShowResults();
        }
    }

    private void ShowResults()
    {
        isQuizActive = false;
        if (quizDisplay == null) return;

        float percentage = ((float)score / questions.Count) * 100f;
        string gradeColor = percentage >= 70f ? "#5eff73" : "#ffbd5e";

        quizDisplay.text = $"<color=#5ac2ff><b>QUIZ COMPLETED!</b></color>\n\n" +
                           $"Your Final Score: <color={gradeColor}><b>{score} / {questions.Count} ({percentage:F0}%)</b></color>\n" +
                           $"Thank you for taking the Great White Shark Educational Quiz!\n\n" +
                           $"<color=#aaaaaa>Restarting the quiz in 5 seconds...</color>";

        StartCoroutine(RestartQuizDelay());
    }

    private IEnumerator RestartQuizDelay()
    {
        yield return new WaitForSeconds(5.0f);
        StartQuiz();
    }

    public bool IsQuizRunning()
    {
        return isQuizActive && !isTransitioning;
    }
}
