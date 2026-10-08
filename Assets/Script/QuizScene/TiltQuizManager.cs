using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime.InteropServices;

public enum Language
{
    Japanese,
    English
}

public class TiltQuizManager : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void OpenUrlInSameTab(string url);

    private enum GameState
    {
        RuleScreen,         // 1. ルール画像表示中
        TutorialQuestion,   // 2. チュートリアル問題中
        TutorialReady,      // 3. 本番前の準備説明中
        PreviewQuestion,    // 4. 問題文・選択肢の事前表示（3秒間）
        Navigation,         // 5. 周回案内画面（館内地図＋〇〇へ行ってみよう！）
        MainQuiz            // 6. 本番クイズ（回答中）
    }
    private GameState currentState = GameState.RuleScreen;

    [System.Serializable]
    public struct Question
    {
        [TextArea(2, 4)]
        public string navigationText; // 周回案内文章
        [TextArea(2, 4)]
        public string questionText;
        public string optionLeft;
        public string optionRight;
        public Sprite optionLeftImage;  // 左選択肢の画像
        public Sprite optionRightImage; // 右選択肢の画像
        public bool isLeftCorrect;
    }

    [Header("Language Settings")]
    public Language currentLanguage = Language.Japanese;

    [Header("Rule UI Reference")]
    public GameObject ruleImageObject;
    public GameObject ruleImageObjectChild;
    public Sprite ruleSpriteJapanese;
    public Sprite ruleSpriteEnglish;

    [Header("Navigation UI Reference (移動案内)")]
    public GameObject navigationPanel;
    public TextMeshProUGUI navigationText;
    public Image mapImage; // 館内地図画像用UI
    public GameObject arrivedButton;

    [Header("UI Text Reference")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI optionLeftText;
    public TextMeshProUGUI optionRightText;
    public TextMeshProUGUI resultText;
    public GameObject subText;

    [Header("Option Image Reference (選択肢用画像)")]
    public Image optionLeftUI;
    public Image optionRightUI;

    [Header("Button Text References")]
    public TextMeshProUGUI retryButtonText;
    public TextMeshProUGUI titleButtonText;
    public TextMeshProUGUI useCouponButtonText;

    [Header("UI Container Reference")]
    public GameObject questionPanel;
    public GameObject optionLeftPanel;
    public GameObject optionRightPanel;
    public GameObject resultPanel;

    [Header("Score & Ranking UI Reference")]
    public GameObject scoreFrameImage;        // 💡 スコア枠（画像オブジェクト）
    public TextMeshProUGUI scoreText;          // 💡 スコア詳細表示テキスト
    public UIPopAnimation scoreFramePopAnim;   // 💡 スコア枠アニメーション

    public GameObject rankingFrameImage;      // 💡 ランキング枠（画像オブジェクト）
    public TextMeshProUGUI rankingText;        // 💡 ランキング表示テキスト
    public UIPopAnimation rankingFramePopAnim; // 💡 ランキング枠アニメーション

    [Header("End Game Buttons")]
    public GameObject retryButton;
    public GameObject titleButton;
    public string titleSceneName = "TitleScene";

    [Header("Coupon / Result UI Settings")]
    public GameObject couponPanel;           // クーポン風リザルト画面
    public GameObject couponImage;           // クーポン画像オブジェクト
    public TextMeshProUGUI clearResultText;  // クリア専用の結果テキスト枠
    public GameObject useCouponButton;       // 「クーポンを使用する」ボタン
    public GameObject usedCouponBadge;        // 「使用済み」表示用オブジェクト
    public UIPopAnimation couponButtonPopAnim;
    public UIPopAnimation clearResultTextPopAnim; // クリアテキスト用アニメーション
    private const string CouponUsedKey = "HasUsedCoupon_RedBrick";

    [Header("Fade Settings")]
    public float screenFadeDuration = 0.5f;
    public Color fadeColor = Color.black;
    private CanvasGroup fadeCanvasGroup;

    [Header("Status UI Reference")]
    public TextMeshProUGUI progressStatusText; // 「第 1 / 5 問」等を表示するUI

    [Header("Timer Settings")]
    public Slider timerSlider;
    public float timeLimit = 15f;
    public float questionPreviewDuration = 3f; // 事前表示の秒数（3秒）
    private float currentTimer = 0f;
    private bool isTimerRunning = false;

    private float totalTimeSpent = 0f;       // 💡 回答にかかった合計時間（秒）
    private int finalScore = 0;              // 💡 最終算出スコア

    [Header("UI Image Settings (〇×表示用)")]
    public Image markImage;
    public Sprite correctSprite;
    public Sprite incorrectSprite;

    [Header("傾き・キー状態のUI画像設定")]
    public Image statusImage;
    public float maxTiltAngle = 30f;
    public float tiltSmoothSpeed = 10f;

    [Header("Quiz Data (全5問・順番固定)")]
    public Question[] questionsJapanese = new Question[5];
    public Question[] questionsEnglish = new Question[5];

    private int currentQuestionIndex = 0;
    private int correctCount = 0;
    private const int totalQuestionCount = 5;

    [Header("Game Rules")]
    private int missCount = 0;

    [Header("Tilt / Key Settings")]
    public float inputThreshold = 0.15f;
    public float resetThreshold = 0.15f;

    [Header("Tilt Hold Settings")]
    public float holdDuration = 0.5f; 
    private float currentHoldTime = 0f;
    private int currentHoldDirection = 0;

    [Header("Hold Gauge UI Settings")]
    public Image holdProgressImageLeft;
    public Image holdProgressImageRight;

    [Header("Audio Settings")]
    public AudioSource bgmAudioSource;
    public AudioSource seAudioSource;
    public AudioClip bgmClip;
    public AudioClip startSE;      
    public AudioClip quizStartSE;  
    public AudioClip correctSE;
    public AudioClip incorrectSE;
    public AudioClip buttonSE;     
    public AudioClip clearSE;      

    [Header("BGM Fade Settings")]
    public float defaultBgmVolume = 0.5f;
    public float fadeDuration = 1.0f;
    private Coroutine bgmFadeCoroutine;

    [Header("Color Settings")]
    public Color defaultColor = Color.white;
    public Color correctColor = new Color(0.8f, 1f, 0.8f);
    public Color incorrectColor = new Color(1f, 0.8f, 0.8f);

    [Header("Minecart Tilt & Graphics System")]
    public MinecartTilt minecartTilt; 
    public MinecartGraphics minecartGraphics;

    [Header("UI Pop Animations")]
    public UIPopAnimation questionPopAnim;
    public UIPopAnimation optionLeftPopAnim;
    public UIPopAnimation optionRightPopAnim;
    public UIPopAnimation optionLeftImagePopAnim;  
    public UIPopAnimation optionRightImagePopAnim; 
    public UIPopAnimation resultPopAnim;
    public UIPopAnimation retryButtonPopAnim;
    public UIPopAnimation titleButtonPopAnim;
    public UIPopAnimation navigationTextPopAnim;
    public UIPopAnimation arrivedButtonPopAnim;
    public UIPopAnimation markPopAnim;

    private bool hasAnswered = false;
    private bool waitingForNeutral = false;
    private bool isGameOver = false;
    private bool isButtonTransitioning = false; 
    private Camera mainCamera;

    private bool isReadyToStart = false; 
    private bool isTransitionCoolingDown = false; 
    private static bool isFirstPlay = true; 

    private const string RankingKeyPrefix = "RedBrickQuiz_Rank_";

    void Start()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Input.simulateMouseWithTouches = true;

        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }

        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindAnyObjectByType<Camera>();
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;

        if (markImage != null) markImage.gameObject.SetActive(false);
        if (retryButton != null) retryButton.SetActive(false);
        if (titleButton != null) titleButton.SetActive(false);
        if (couponPanel != null) couponPanel.SetActive(false);
        if (navigationPanel != null) navigationPanel.SetActive(false);

        if (scoreFrameImage != null) scoreFrameImage.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (rankingFrameImage != null) rankingFrameImage.SetActive(false);
        if (rankingText != null) rankingText.gameObject.SetActive(false);

        if (subText != null) subText.SetActive(false);

        HideOptionImages();

        ResetHoldState();
        SetupFadeUI();
        StartCoroutine(FadeInScreen());
        PlayBGMWithFadeIn();

        int savedLanguage = PlayerPrefs.GetInt("SelectedLanguage", 0);
        currentLanguage = (Language)savedLanguage;

        UpdateAllUITexts();

        if (isFirstPlay)
        {
            isFirstPlay = false; 
            StartRuleScreen();   
        }
        else
        {
            if (ruleImageObject != null) ruleImageObject.SetActive(false);
            ShowTutorialReadyScreenDirectly();
        }
    }

    private void HideOptionImages()
    {
        if (optionLeftUI != null) optionLeftUI.gameObject.SetActive(false);
        if (optionRightUI != null) optionRightUI.gameObject.SetActive(false);
    }

    #region --- 言語切り替え・テキスト取得 ---

    private string GetText(string jp, string en)
    {
        return currentLanguage == Language.Japanese ? jp : en;
    }

    public void SetLanguage(int langIndex)
    {
        currentLanguage = (Language)langIndex;
        UpdateAllUITexts();

        if (currentState == GameState.MainQuiz && !isGameOver)
        {
            ShowQuestion();
        }
        else if (currentState == GameState.Navigation)
        {
            ShowNavigationScreen();
        }
    }

    public void UpdateAllUITexts()
    {
        if (retryButtonText != null)
            retryButtonText.text = GetText("もう一度あそぶ", "Play Again");

        if (titleButtonText != null)
            titleButtonText.text = GetText("タイトルへ", "Title Screen");

        if (useCouponButtonText != null)
            useCouponButtonText.text = GetText("クーポンを使用する", "Use Coupon");

        UpdateStatusUI();
    }

    #endregion

    #region --- フェード処理 ---

    private void SetupFadeUI()
    {
        Canvas mainCanvas = FindAnyObjectByType<Canvas>();
        if (mainCanvas == null) return;

        GameObject fadeObj = new GameObject("ScreenFadePanel");
        fadeObj.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform rect = fadeObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image fadeImage = fadeObj.AddComponent<Image>();
        fadeImage.color = fadeColor;
        fadeImage.raycastTarget = true;

        fadeCanvasGroup = fadeObj.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 1f;
        fadeCanvasGroup.blocksRaycasts = true;

        fadeObj.transform.SetAsLastSibling();
    }

    private IEnumerator FadeInScreen()
    {
        if (fadeCanvasGroup == null) yield break;
        float elapsed = 0f;
        fadeCanvasGroup.alpha = 1f;

        while (elapsed < screenFadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / screenFadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeOutScreen()
    {
        if (fadeCanvasGroup == null) yield break;
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        float startAlpha = fadeCanvasGroup.alpha;

        while (elapsed < screenFadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, elapsed / screenFadeDuration);
            yield return null;
        }

        fadeCanvasGroup.alpha = 1f;
    }

    #endregion

    private float GetInputValue()
    {
        float inputValue = 0f;

#if UNITY_WEBGL && !UNITY_EDITOR
        ScreenOrientation screenOrient = Screen.orientation;
        DeviceOrientation deviceOrient = Input.deviceOrientation;

        bool isScreenNotLandscape = (screenOrient != ScreenOrientation.LandscapeLeft && 
                                     screenOrient != ScreenOrientation.LandscapeRight);
        bool isDevicePortrait = (deviceOrient == DeviceOrientation.Portrait || 
                                 deviceOrient == DeviceOrientation.PortraitUpsideDown);

        if (isScreenNotLandscape || isDevicePortrait) return 0f;
#else
        if (Screen.height > Screen.width) return 0f;
#endif

        Vector3 acc = Input.acceleration;
        inputValue = Mathf.Clamp(acc.y / 0.5f, -1f, 1f);

        if (acc.x < 0f) inputValue = -inputValue;

        if (Mathf.Abs(inputValue) < 0.05f)
        {
            inputValue = Input.GetAxis("Horizontal");
        }

        return inputValue;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D) && !isGameOver) { DebugSkipToClear(); return; }
        if (Input.GetKeyDown(KeyCode.C)) { ClearCouponData(); }

        UpdateStatusImageRotation();
        float inputValue = GetInputValue();

        switch (currentState)
        {
            case GameState.RuleScreen:
                if (!isTransitionCoolingDown)
                {
                    HandleHoldTransitionInput(inputValue, () => {
                        PlaySE(startSE);
                        StartTutorialQuestion();
                    });
                }
                break;

            case GameState.TutorialQuestion:
                HandleTutorialQuestionUpdate();
                break;

            case GameState.TutorialReady:
                if (isReadyToStart && !isTransitionCoolingDown)
                {
                    HandleHoldTransitionInput(inputValue, () => {
                        PlaySE(quizStartSE);
                        StartMainQuiz();
                    });
                }
                break;

            case GameState.Navigation:
                // 💡 「到着した！」ボタンは傾き操作を除外したため、キー/傾きによる自動遷移は行いません。
                break;

            case GameState.MainQuiz:
                HandleMainQuizUpdate();
                break;
        }
    }

    void HandleHoldTransitionInput(float inputValue, System.Action onComplete)
    {
        if (inputValue < -inputThreshold)
        {
            ProcessHoldInput(-1, onComplete);
        }
        else if (inputValue > inputThreshold)
        {
            ProcessHoldInput(1, onComplete);
        }
        else
        {
            ResetHoldState();
        }
    }

    #region --- フェーズ遷移ロジック ---

    void StartRuleScreen()
    {
        currentState = GameState.RuleScreen;
        if (ruleImageObject != null)
        {
            ruleImageObject.SetActive(true);
            Image ruleImg = ruleImageObjectChild.GetComponent<Image>();
            if (ruleImg != null)
            {
                ruleImg.sprite = (currentLanguage == Language.Japanese) ? ruleSpriteJapanese : ruleSpriteEnglish;
            }
        }
        
        SetQuizUIActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);
        
        SetResultText(GetText(
            "【 ルール説明 】\n確認したら、スマホを左右どちらかに傾けてね！",
            "【 HOW TO PLAY 】\nTilt your phone left or right when ready!"
        ));

        StartTransitionCoolDown(0.8f);
    }

    void StartTutorialQuestion()
    {
        currentState = GameState.TutorialQuestion;
        if (ruleImageObject != null) ruleImageObject.SetActive(false);
        SetQuizUIActive(true);
        HideOptionImages();

        if (progressStatusText != null) progressStatusText.text = GetText("【チュートリアル】", "【TUTORIAL】");

        if (questionText != null) 
            questionText.text = GetText("練習問題：半田赤レンガは100年以上の歴史を持つ？", "Practice: Does Red Brick Building have over 100 years of history?");

        if (optionLeftText != null) optionLeftText.text = GetText("← ○（そう！）", "← YES");
        if (optionRightText != null) optionRightText.text = GetText("×（ちがう） →", "NO →");

        PlayQuestionPop();
        PlayOptionsPop();

        SetResultText(GetText("スマホを「左」にしっかり傾けて「○」を選んでみよう！", "Tilt your phone to the LEFT to select 'YES'!"));

        hasAnswered = false;
        waitingForNeutral = true;
        ResetHoldState();
        StartTransitionCoolDown(0.8f);
    }

    void HandleTutorialQuestionUpdate()
    {
        float inputValue = GetInputValue();

        if (waitingForNeutral)
        {
            if (Mathf.Abs(inputValue) < resetThreshold) waitingForNeutral = false;
            return;
        }

        if (hasAnswered || isTransitionCoolingDown) return;

        if (inputValue < -inputThreshold)
        {
            ProcessHoldInput(-1, () => AnswerTutorial(true));
        }
        else if (inputValue > inputThreshold)
        {
            ProcessHoldInput(1, () => AnswerTutorial(false));
        }
        else
        {
            ResetHoldState();
        }
    }

    void AnswerTutorial(bool choseLeft)
    {
        hasAnswered = true;
        ResetHoldState();

        if (choseLeft) 
        {
            PlaySE(correctSE);
            ShowMark(correctSprite);
            if (mainCamera != null) mainCamera.backgroundColor = correctColor;
            SetResultText(GetText("大正解！バッチリです！", "Correct! Great job!"));
            Invoke(nameof(ShowTutorialReadyScreen), 1.5f);
        }
        else 
        {
            PlaySE(incorrectSE);
            ShowMark(incorrectSprite);
            if (mainCamera != null) mainCamera.backgroundColor = incorrectColor;
            SetResultText(GetText("おっと！左に傾けて「○」を選んでね！", "Oops! Tilt LEFT to select 'YES'!"));
            Invoke(nameof(ResetTutorialState), 1.5f);
        }
    }

    void ResetTutorialState()
    {
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);
        
        SetResultText(GetText("スマホを「左」に傾けて「○」を選んでみよう！", "Tilt your phone to the LEFT to select 'YES'!"));

        hasAnswered = false;
        waitingForNeutral = true;
        ResetHoldState();
    }

    void ShowTutorialReadyScreen()
    {
        currentState = GameState.TutorialReady;
        isReadyToStart = false; 
        SetQuizUIActive(false);
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);

        if (resultPanel != null) resultPanel.SetActive(true);
        SetResultText(GetText("操作はバッチリ！次からはいよいよ本番だよ！", "You got it! Get ready for the main quiz!"));

        Invoke(nameof(ShowFinalInstructionText), 2.5f);
    }

    void ShowTutorialReadyScreenDirectly()
    {
        currentState = GameState.TutorialReady;
        SetQuizUIActive(false);
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);

        if (resultPanel != null) resultPanel.SetActive(true);
        ShowFinalInstructionText();
    }

    void ShowFinalInstructionText()
    {
        SetResultText(GetText(
            "全5問に挑戦しよう！準備ができたら\n【 スマホを左右どちらかに傾けて 】スタート！",
            "Try all 5 questions!\nTilt your phone left or right to START!"
        ));

        isReadyToStart = true; 
        StartTransitionCoolDown(0.5f);
    }

    void StartMainQuiz()
    {
        currentQuestionIndex = 0;
        correctCount = 0;
        missCount = 0;
        totalTimeSpent = 0f;
        isGameOver = false;

        if (minecartGraphics != null) minecartGraphics.ResetCart();

        StartQuestionSequence();
    }

    #endregion

    #region --- 問題表示 ＆ 周回案内（ナビゲーション）フロー ---

    void StartQuestionSequence()
    {
        if (currentQuestionIndex >= totalQuestionCount)
        {
            ShowClearResult();
            return;
        }

        StartCoroutine(QuestionPreviewRoutine());
    }

    private IEnumerator QuestionPreviewRoutine()
    {
        currentState = GameState.PreviewQuestion;
        SetQuizUIActive(false);
        if (navigationPanel != null) navigationPanel.SetActive(false);
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);

        // 💡 事前表示：問題文と2つの選択肢を表示
        if (questionPanel != null) questionPanel.SetActive(true);
        if (optionLeftPanel != null) optionLeftPanel.SetActive(true);
        if (optionRightPanel != null) optionRightPanel.SetActive(true);
        if (progressStatusText != null) progressStatusText.gameObject.SetActive(true);
        UpdateStatusUI();

        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
        Question q = sourceQuestions[currentQuestionIndex];
        
        if (questionText != null) questionText.text = $"Q{currentQuestionIndex + 1}. {q.questionText}";
        if (optionLeftText != null) optionLeftText.text = "← " + q.optionLeft;
        if (optionRightText != null) optionRightText.text = q.optionRight + " →";

        if (optionLeftUI != null)
        {
            if (q.optionLeftImage != null)
            {
                optionLeftUI.sprite = q.optionLeftImage;
                optionLeftUI.gameObject.SetActive(true);
            }
            else optionLeftUI.gameObject.SetActive(false);
        }

        if (optionRightUI != null)
        {
            if (q.optionRightImage != null)
            {
                optionRightUI.sprite = q.optionRightImage;
                optionRightUI.gameObject.SetActive(true);
            }
            else optionRightUI.gameObject.SetActive(false);
        }

        PlayQuestionPop();
        PlayOptionsPop();

        // 💡 3秒間待機
        yield return new WaitForSeconds(questionPreviewDuration);

        // 💡 問題・選択肢を非表示にしてからナビゲーション表示へ移行
        SetQuizUIActive(false);
        HideOptionImages();

        ShowNavigationScreen();
    }

    void ShowNavigationScreen()
    {
        currentState = GameState.Navigation;
        isButtonTransitioning = false; 
        SetQuizUIActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        if (navigationPanel != null) navigationPanel.SetActive(true);

        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
        if (currentQuestionIndex < sourceQuestions.Length)
        {
            Question currentQ = sourceQuestions[currentQuestionIndex];
            if (navigationText != null)
            {
                // 💡 改行を削除して1行/一続きに
                navigationText.text = $"【第 {currentQuestionIndex + 1} 問目のスポット】 {currentQ.navigationText}";
            }
        }

        if (navigationTextPopAnim != null) navigationTextPopAnim.PlayPop();
        if (arrivedButtonPopAnim != null) arrivedButtonPopAnim.PlayPop();

        ResetHoldState();
        StartTransitionCoolDown(0.5f);
    }

    public void OnClickArrivedButton()
    {
        if (currentState != GameState.Navigation || isTransitionCoolingDown || isButtonTransitioning) return;

        StartCoroutine(ArrivedButtonRoutine());
    }

    private IEnumerator ArrivedButtonRoutine()
    {
        isButtonTransitioning = true;

        PlaySE(buttonSE);
        if (arrivedButtonPopAnim != null) arrivedButtonPopAnim.PlayPop();

        float waitTime = 0.35f;
        if (buttonSE != null) waitTime = Mathf.Max(waitTime, buttonSE.length);
        yield return new WaitForSeconds(waitTime);

        if (navigationPanel != null) navigationPanel.SetActive(false);

        currentState = GameState.MainQuiz;
        SetQuizUIActive(true);
        if (resultPanel != null) resultPanel.SetActive(true);

        waitingForNeutral = true;
        ResetHoldState();
        ShowQuestion();
        isButtonTransitioning = false;
    }

    #endregion

    void SetQuizUIActive(bool active)
    {
        if (questionPanel != null) questionPanel.SetActive(active);
        if (optionLeftPanel != null) optionLeftPanel.SetActive(active);
        if (optionRightPanel != null) optionRightPanel.SetActive(active);
        if (resultPanel != null) resultPanel.SetActive(active);

        if (timerSlider != null) timerSlider.gameObject.SetActive(active);
        if (progressStatusText != null) progressStatusText.gameObject.SetActive(active);
        if (subText != null) subText.SetActive(active);
    }

    void StartTransitionCoolDown(float time)
    {
        isTransitionCoolingDown = true;
        Invoke(nameof(ResetCoolDown), time);
    }

    void ResetCoolDown()
    {
        isTransitionCoolingDown = false;
    }

    #region --- 本番クイズ処理 ---

    void HandleMainQuizUpdate()
    {
        if (isGameOver) return;

        if (isTimerRunning && !hasAnswered && !waitingForNeutral)
        {
            currentTimer -= Time.deltaTime;

            if (timerSlider != null)
            {
                timerSlider.value = Mathf.Clamp01(currentTimer / timeLimit);
            }

            if (currentTimer <= 0f)
            {
                isTimerRunning = false;
                totalTimeSpent += timeLimit; // 制限時間いっぱいを使用
                SetResultText("<color=blue>Time Up!</color>");
                Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
                Question currentQ = sourceQuestions[currentQuestionIndex];
                Answer(!currentQ.isLeftCorrect);
                return;
            }
        }

        float inputValue = GetInputValue();

        if (waitingForNeutral)
        {
            if (Mathf.Abs(inputValue) < resetThreshold)
            {
                waitingForNeutral = false;
                SetResultText(GetText("スマホを傾けるか、左右キーで答えよう！", "Tilt your phone or press Left/Right keys!"));
                currentTimer = timeLimit;
                isTimerRunning = true;
            }
            return;
        }

        if (hasAnswered) return;

        if (inputValue < -inputThreshold)
        {
            ProcessHoldInput(-1, () => Answer(true));
        }
        else if (inputValue > inputThreshold)
        {
            ProcessHoldInput(1, () => Answer(false));
        }
        else
        {
            ResetHoldState();
        }
    }

    void ProcessHoldInput(int targetDirection, System.Action onExecute)
    {
        if (currentHoldDirection != targetDirection)
        {
            currentHoldDirection = targetDirection;
            currentHoldTime = 0f;
        }

        currentHoldTime += Time.deltaTime;
        float progress = Mathf.Clamp01(currentHoldTime / holdDuration);

        if (targetDirection == -1)
        {
            if (holdProgressImageLeft != null)
            {
                holdProgressImageLeft.gameObject.SetActive(true);
                holdProgressImageLeft.fillAmount = progress;
            }
            if (holdProgressImageRight != null) holdProgressImageRight.gameObject.SetActive(false);
        }
        else if (targetDirection == 1)
        {
            if (holdProgressImageRight != null)
            {
                holdProgressImageRight.gameObject.SetActive(true);
                holdProgressImageRight.fillAmount = progress;
            }
            if (holdProgressImageLeft != null) holdProgressImageLeft.gameObject.SetActive(false);
        }

        if (currentHoldTime >= holdDuration)
        {
            ResetHoldState();
            onExecute?.Invoke();
        }
    }

    void ResetHoldState()
    {
        currentHoldDirection = 0;
        currentHoldTime = 0f;

        if (holdProgressImageLeft != null)
        {
            holdProgressImageLeft.fillAmount = 0f;
            holdProgressImageLeft.gameObject.SetActive(false);
        }
        if (holdProgressImageRight != null)
        {
            holdProgressImageRight.fillAmount = 0f;
            holdProgressImageRight.gameObject.SetActive(false);
        }
    }

    void UpdateStatusImageRotation()
    {
        float inputValue = GetInputValue();
        if (minecartTilt != null)
        {
            minecartTilt.SetTiltInput(inputValue, inputThreshold);
        }
    }

    void UpdateStatusUI()
    {
        if (progressStatusText != null)
        {
            int displayIndex = Mathf.Min(currentQuestionIndex + 1, totalQuestionCount);
            progressStatusText.text = GetText($"第 {displayIndex} / {totalQuestionCount} 問", $"Q {displayIndex} / {totalQuestionCount}");
        }
    }

    void ShowQuestion()
    {
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);

        UpdateStatusUI();

        if (currentQuestionIndex >= totalQuestionCount)
        {
            ShowClearResult();
            return;
        }

        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
        Question q = sourceQuestions[currentQuestionIndex];

        if (subText != null) subText.SetActive(true);

        if (questionText != null) questionText.text = $"Q{currentQuestionIndex + 1}. {q.questionText}";
        PlayQuestionPop();

        if (optionLeftPanel != null) optionLeftPanel.SetActive(true);
        if (optionLeftText != null) optionLeftText.text = "← " + q.optionLeft;
        
        if (optionLeftUI != null)
        {
            if (q.optionLeftImage != null)
            {
                optionLeftUI.sprite = q.optionLeftImage;
                optionLeftUI.gameObject.SetActive(true);
                if (optionLeftImagePopAnim != null) optionLeftImagePopAnim.PlayPop();
            }
            else
            {
                optionLeftUI.gameObject.SetActive(false);
            }
        }

        if (optionRightPanel != null) optionRightPanel.SetActive(true);
        if (optionRightText != null) optionRightText.text = q.optionRight + " →";
        
        if (optionRightUI != null)
        {
            if (q.optionRightImage != null)
            {
                optionRightUI.sprite = q.optionRightImage;
                optionRightUI.gameObject.SetActive(true);
                if (optionRightImagePopAnim != null) optionRightImagePopAnim.PlayPop();
            }
            else
            {
                optionRightUI.gameObject.SetActive(false);
            }
        }

        PlayOptionsPop();

        if (!waitingForNeutral)
        {
            SetResultText(GetText("スマホを傾けるか、左右キーで答えよう！", "Tilt your phone or press Left/Right keys!"));
            currentTimer = timeLimit;
            isTimerRunning = true;
            if (timerSlider != null) timerSlider.value = 1f;
        }

        hasAnswered = false;
        ResetHoldState();
    }

    void Answer(bool choseLeft)
    {
        hasAnswered = true;
        isTimerRunning = false;
        ResetHoldState();

        // 💡 かかった時間を加算
        float timeTaken = timeLimit - Mathf.Max(0f, currentTimer);
        totalTimeSpent += timeTaken;

        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
        Question q = sourceQuestions[currentQuestionIndex];
        bool isCorrect = (choseLeft == q.isLeftCorrect);

        ShowMark(isCorrect ? correctSprite : incorrectSprite);

        if (isCorrect)
        {
            PlaySE(correctSE);
            correctCount++;

            SetResultText(GetText("<color=red>正解!</color>", "<color=red>Correct!</color>"));
            if (mainCamera != null) mainCamera.backgroundColor = correctColor;
        }
        else
        {
            PlaySE(incorrectSE);
            missCount++;

            if (minecartGraphics != null) minecartGraphics.OnMiss();

            if (currentTimer > 0f)
            {
                SetResultText(GetText("<color=blue>不正解...</color>", "<color=blue>Wrong...</color>"));
            }
            if (mainCamera != null) mainCamera.backgroundColor = incorrectColor;
        }

        currentQuestionIndex++;

        if (currentQuestionIndex >= totalQuestionCount)
        {
            Invoke(nameof(ShowClearResult), 1.5f);
        }
        else
        {
            HideOptionImages();
            Invoke(nameof(StartQuestionSequence), 1.5f);
        }
    }

    void CalculateFinalScore()
    {
        // 💡 スコア計算式
        // 基礎点：1問正解につき2,000点
        int baseScore = correctCount * 2000;
        
        // タイムボーナス：残った秒数（合計） × 100点
        float totalRemainingTime = Mathf.Max(0f, (totalQuestionCount * timeLimit) - totalTimeSpent);
        int timeBonus = Mathf.RoundToInt(totalRemainingTime * 100f);

        finalScore = baseScore + timeBonus;
    }

    void ShowClearResult()
    {
        SetQuizUIActive(false);
        HideOptionImages();
        hasAnswered = true;
        isGameOver = true;
        isTimerRunning = false;

        if (markImage != null) markImage.gameObject.SetActive(false);

        PlaySE(clearSE);

        CalculateFinalScore();

        // 💡 正答率・時間の計算
        float accuracy = ((float)correctCount / totalQuestionCount) * 100f;

        // 💡 スコア詳細の表示＆アニメーション
        if (scoreFrameImage != null)
        {
            scoreFrameImage.SetActive(true);
            if (scoreFramePopAnim != null) scoreFramePopAnim.PlayPop();
        }

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = GetText(
                $"【 結果詳細 】\n" +
                $"回答時間: {totalTimeSpent:F1}秒\n" +
                $"正答率: {accuracy:F0}%\n" +
                $"最終スコア: {finalScore:N0}点",

                $"【 RESULTS 】\n" +
                $"Time: {totalTimeSpent:F1}s\n" +
                $"Accuracy: {accuracy:F0}%\n" +
                $"Final Score: {finalScore:N0}"
            );
        }

        // 💡 ランキングの更新と表示＆アニメーション
        UpdateAndSaveRanking(finalScore);

        if (rankingFrameImage != null)
        {
            rankingFrameImage.SetActive(true);
            if (rankingFramePopAnim != null) rankingFramePopAnim.PlayPop();
        }

        if (rankingText != null)
        {
            rankingText.gameObject.SetActive(true);
            DisplayRanking();
        }

        // クリア専用テキストへ設定
        if (clearResultText != null)
        {
            clearResultText.gameObject.SetActive(true);
            clearResultText.text = GetText(
                $"おめでとう！\n5問中 <color=red>{correctCount}</color> 問正解でした！",
                $"Congratulations!\nYou scored <color=red>{correctCount}</color> / {totalQuestionCount}!"
            );
            if (clearResultTextPopAnim != null) clearResultTextPopAnim.PlayPop();
        }

        if (couponPanel != null)
        {
            couponPanel.SetActive(true);
            if (couponImage != null) couponImage.SetActive(true);

            bool isUsed = PlayerPrefs.GetInt(CouponUsedKey, 0) == 1;
            if (useCouponButton != null)
            {
                useCouponButton.SetActive(!isUsed);
                if (!isUsed && couponButtonPopAnim != null) couponButtonPopAnim.PlayPop();
            }
            if (usedCouponBadge != null)
            {
                usedCouponBadge.SetActive(isUsed);
            }
        }

        ShowEndButtons();
    }

    #endregion

    #region --- ランキング処理 ---

    private void UpdateAndSaveRanking(int newScore)
    {
        List<int> scores = new List<int>();

        for (int i = 1; i <= 3; i++)
        {
            scores.Add(PlayerPrefs.GetInt(RankingKeyPrefix + i, 0));
        }

        scores.Add(newScore);
        scores.Sort((a, b) => b.CompareTo(a)); // 降順ソート

        for (int i = 0; i < 3; i++)
        {
            PlayerPrefs.SetInt(RankingKeyPrefix + (i + 1), scores[i]);
        }
        PlayerPrefs.Save();
    }

    private void DisplayRanking()
    {
        int rank1 = PlayerPrefs.GetInt(RankingKeyPrefix + "1", 0);
        int rank2 = PlayerPrefs.GetInt(RankingKeyPrefix + "2", 0);
        int rank3 = PlayerPrefs.GetInt(RankingKeyPrefix + "3", 0);

        if (rankingText != null)
        {
            rankingText.text = GetText(
                $"【 ランキング 】\n" +
                $"1位 : {rank1:N0}点\n" +
                $"2位 : {rank2:N0}点\n" +
                $"3位 : {rank3:N0}点",

                $"【 RANKING 】\n" +
                $"1st : {rank1:N0}\n" +
                $"2nd : {rank2:N0}\n" +
                $"3rd : {rank3:N0}"
            );
        }
    }

    #endregion

    #region --- アニメーション・演出ヘルパー ---

    private void ShowMark(Sprite sprite)
    {
        if (markImage != null)
        {
            markImage.gameObject.SetActive(true);
            markImage.sprite = sprite;
            if (markPopAnim != null) markPopAnim.PlayPop();
        }
    }

    private void SetResultText(string text)
    {
        if (resultText != null)
        {
            resultText.gameObject.SetActive(true);
            resultText.text = text;
            if (resultPopAnim != null) resultPopAnim.PlayPop();
        }
    }

    private void PlayQuestionPop()
    {
        if (questionPopAnim != null) questionPopAnim.PlayPop();
    }

    private void PlayOptionsPop()
    {
        if (optionLeftPopAnim != null) optionLeftPopAnim.PlayPop();
        if (optionRightPopAnim != null) optionRightPopAnim.PlayPop();
    }

    #endregion

    #region --- オーディオ処理 ---

    void PlayBGMWithFadeIn()
    {
        if (bgmAudioSource != null && bgmClip != null)
        {
            bgmAudioSource.clip = bgmClip;
            bgmAudioSource.loop = true;
            bgmAudioSource.volume = 0f;
            bgmAudioSource.Play();

            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeAudioSource(bgmAudioSource, defaultBgmVolume, fadeDuration));
        }
    }

    void FadeOutBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            if (bgmFadeCoroutine != null) StopCoroutine(bgmFadeCoroutine);
            bgmFadeCoroutine = StartCoroutine(FadeAudioSource(bgmAudioSource, 0f, fadeDuration));
        }
    }

    private IEnumerator FadeAudioSource(AudioSource source, float targetVolume, float duration)
    {
        float startVolume = source.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
            yield return null;
        }

        source.volume = targetVolume;
        if (targetVolume <= 0f) source.Stop();
    }

    void PlaySE(AudioClip clip)
    {
        if (seAudioSource != null && clip != null)
        {
            seAudioSource.PlayOneShot(clip);
        }
    }

    #endregion

    void ShowEndButtons()
    {
        if (retryButton != null) retryButton.SetActive(true);
        if (titleButton != null) titleButton.SetActive(true);
    }

    #region --- ボタンイベント関数 ---

    public void OnClickRetryButton()
    {
        if (isButtonTransitioning) return;
        isButtonTransitioning = true;

        if (retryButtonPopAnim != null) retryButtonPopAnim.PlayPop();

        Scene currentScene = SceneManager.GetActiveScene();
        StartCoroutine(PlayButtonSEAndChangeScene(currentScene.name));
    }

    public void OnClickTitleButton()
    {
        if (isButtonTransitioning) return;
        isButtonTransitioning = true;

        if (titleButtonPopAnim != null) titleButtonPopAnim.PlayPop();

        StartCoroutine(PlayButtonSEAndChangeScene(titleSceneName));
    }

    public void OnClickUseCouponButton()
    {
        if (seAudioSource != null && buttonSE != null)
        {
            seAudioSource.PlayOneShot(buttonSE);
        }

        PlayerPrefs.SetInt(CouponUsedKey, 1);
        PlayerPrefs.Save();

        if (useCouponButton != null) useCouponButton.SetActive(false);
        if (usedCouponBadge != null) usedCouponBadge.SetActive(true);
    }

    private IEnumerator PlayButtonSEAndChangeScene(string sceneName)
    {
        FadeOutBGM();
        StartCoroutine(FadeOutScreen());

        float waitTime = 0f;
        if (seAudioSource != null && buttonSE != null)
        {
            seAudioSource.PlayOneShot(buttonSE);
            waitTime = buttonSE.length;
        }

        yield return new WaitForSeconds(Mathf.Max(waitTime, screenFadeDuration));
        SceneManager.LoadScene(sceneName);
    }

    #endregion

    #region --- デバッグ機能 ---

    void DebugSkipToClear()
    {
        currentState = GameState.MainQuiz;
        correctCount = totalQuestionCount;
        totalTimeSpent = 12.5f;
        ShowClearResult();
    }

    void ClearCouponData()
    {
        PlayerPrefs.DeleteKey(CouponUsedKey);
        PlayerPrefs.Save();
        Debug.Log("クーポン使用履歴をリセットしました！");
    }

    #endregion
}
