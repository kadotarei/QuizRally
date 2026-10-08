using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro; // TextMeshProを使うために必要
using UnityEngine.UI; // ButtonやImageを使うために必要

public class QuizManager : MonoBehaviour
{
    // クイズのデータ構造を定義
    [System.Serializable]
    public class QuizData
    {
        [TextArea] public string questionText; // 問題文
        public string ansA; // 左側の選択肢
        public string ansB; // 右側の選択肢
        public int correctIndex; // 正解の番号 (1: 左, 2: 右)
    }

    [Header("UIの設定")]
    public TextMeshProUGUI questionTextUI; // 画面の問題文テキスト
    public TextMeshProUGUI ansATextUI;     // 左ボタンのテキスト
    public TextMeshProUGUI ansBTextUI;     // 右ボタンのテキスト
    public GameObject correctImage;         // 〇の画像
    public GameObject incorrectImage;       // ×の画像

    [Header("クイズのデータ")]
    public List<QuizData> originalQuizList; // 事前に登録するクイズのリスト
    private List<QuizData> currentPlayList; // 今回のプレイで使うクイズのリスト
    private QuizData currentQuiz;           // 現在出題中のクイズ

    private int questionCount = 0;          // 今何問目か

    void Start()
    {
        // 〇×画像は最初は確実に隠しておく
        correctImage.SetActive(false);
        incorrectImage.SetActive(false);

        // クイズのリストを複製して、ランダムに並び替える準備
        currentPlayList = new List<QuizData>(originalQuizList);
        ShuffleList(currentPlayList);

        // 最初の問題を表示
        SetQuestion();
    }

    // クイズを画面にセットする関数
    void SetQuestion()
    {
        // 5問解き終わったら、または問題がなくなったら終了（今回は5問で制限）
        if (questionCount >= 5 || currentPlayList.Count == 0)
        {
            questionTextUI.text = "クイズ終了！お疲れ様でした。";
            ansATextUI.transform.parent.gameObject.SetActive(false); // ボタンを隠す
            ansBTextUI.transform.parent.gameObject.SetActive(false);
            return;
        }

        // リストの先頭からクイズを取り出す
        currentQuiz = currentPlayList[0];
        currentPlayList.RemoveAt(0); // 使った問題はリストから消す

        // 画面にテキストを反映
        questionTextUI.text = currentQuiz.questionText;
        ansATextUI.text = currentQuiz.ansA;
        ansBTextUI.text = currentQuiz.ansB;

        questionCount++;
    }

    // ボタンが押されたときに実行する関数 (引数に選んだ番号を入れる)
    public void OnAnswerClick(int selectIndex)
    {
        // 正解判定
        if (selectIndex == currentQuiz.correctIndex)
        {
            StartCoroutine(ShowResultAndNext(true)); // 〇を表示して次へ
        }
        else
        {
            StartCoroutine(ShowResultAndNext(false)); // ×を表示して次へ
        }
    }

    // 〇×を表示して1秒待つ「コルーチン」という仕組み
    IEnumerator ShowResultAndNext(bool isCorrect)
    {
        // 〇か×を表示
        if (isCorrect) correctImage.SetActive(true);
        else incorrectImage.SetActive(true);

        // 1秒間待つ
        yield return new WaitForSeconds(1.0f);

        // 〇×を非表示に戻す
        correctImage.SetActive(false);
        incorrectImage.SetActive(false);

        // 次の問題へ
        SetQuestion();
    }

    // リストをランダムに並び替える関数（シャッフル）
    void ShuffleList(List<QuizData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            QuizData temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
    }
}