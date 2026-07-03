using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string question;

    public string optionA;
    public string optionB;
    public string optionC;
    public string optionD;

    [Tooltip("Correct Answer: A, B, C, or D")]
    public string correctAnswer;
}

public class QuizScript : MonoBehaviour
{
    [Header("Quiz Data")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("UI References (Unity Text)")]
    public Text questionText;
    public Text optionAText;
    public Text optionBText;
    public Text optionCText;
    public Text optionDText;
    public Text feedbackText;
    public Text resultText;

    private int currentQuestionIndex = 0;
    private int correctCount = 0;
    private int wrongCount = 0;

    public UnityEvent onQuizStart;
    public UnityEvent onQuizFinish;

    public UnityEvent onCurrent;
    public UnityEvent onWrong;
    public void StartQuiz()
    {
        feedbackText.text = "";
        resultText.text = "";
        ShowQuestion();
        onQuizStart?.Invoke();
    }

    public void OnFinishQuiz()
    {
        feedbackText.text = "";
        resultText.text = "";
    }
    //void Start()
    //{
    //    feedbackText.text = "";
    //    resultText.text = "";
   
    //}

    void ShowQuestion()
    {
        if (currentQuestionIndex >= questions.Count)
        {
            ShowResult();
            return;
        }

        QuizQuestion q = questions[currentQuestionIndex];

        questionText.text ="Question " +(currentQuestionIndex +1) + "\n" + q.question;
        optionAText.text =  q.optionA;
        optionBText.text = q.optionB;
        optionCText.text =  q.optionC;
        optionDText.text =  q.optionD;

        feedbackText.text = "";
    }

    public void SelectAnswer(string answer)
    {
        QuizQuestion q = questions[currentQuestionIndex];

        if (answer == q.correctAnswer.ToUpper())
        {
            correctCount++;
            onCurrent?.Invoke();
            feedbackText.text = "Correct Answer!";
        }
        else
        {
            wrongCount++;
            onWrong?.Invoke();
            feedbackText.text = "Wrong Answer";
        }

        currentQuestionIndex++;
        Invoke(nameof(ShowQuestion), 1.5f);
    }

    void ShowResult()
    {
        questionText.text = "Quiz Completed!";
        optionAText.text = "";
        optionBText.text = "";
        optionCText.text = "";
        optionDText.text = "";
        feedbackText.text = "";

        resultText.text =
            "Correct Answers: " + correctCount +
            "\nWrong Answers: " + wrongCount;

        StartCoroutine(FinalizeQuiz());
    }

    IEnumerator FinalizeQuiz()
    {
        yield return new WaitForSeconds(5f);
        OnFinishQuiz();
        onQuizFinish?.Invoke();
    }
}
