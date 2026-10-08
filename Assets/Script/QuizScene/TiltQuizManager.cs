using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Runtime.InteropServices;

// --- 言語管理用Enum ---
public enum Language
{
    Japanese,
    English
}

public class TiltQuizManager : MonoBehaviour
{
    // WebGLビルド時に現在タブ（親ウィンドウ）でURLを開くための外部JS関数宣言
    [DllImport("__Internal")]
    private static extern void OpenUrlInSameTab(string url);

    private enum GameState
    {
        RuleScreen,         // 1. ルール画像表示中
        TutorialQuestion,   // 2. チュートリアル問題中
        TutorialReady,      // 3. 本番前の準備説明中
        MainQuiz            // 4. 本番クイズ中
    }
    private GameState currentState = GameState.RuleScreen;

    [System.Serializable]
    public struct Question
    {
        public string questionText;
        public string optionLeft;
        public string optionRight;
        public bool isLeftCorrect;
    }

    [Header("Language Settings")]
    public Language currentLanguage = Language.Japanese;

    [Header("Rule UI Reference")]
    public GameObject ruleImageObject;
    public GameObject ruleImageObjectChild;
    public Sprite ruleSpriteJapanese; // 💡 日本語のルール画像
    public Sprite ruleSpriteEnglish;  // 💡 英語のルール画像

    [Header("UI Text Reference")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI optionLeftText;
    public TextMeshProUGUI optionRightText;
    public TextMeshProUGUI resultText;
    public GameObject subText;

    [Header("Button Text References")]
    public TextMeshProUGUI retryButtonText;
    public TextMeshProUGUI titleButtonText;
    public TextMeshProUGUI couponButtonText;

    [Header("UI Container Reference (背景画像枠のオブジェクト)")]
    public GameObject questionPanel;
    public GameObject optionLeftPanel;
    public GameObject optionRightPanel;
    public GameObject resultPanel;

    [Header("End Game Buttons")]
    public GameObject retryButton;
    public GameObject titleButton;
    public string titleSceneName = "TitleScene";

    [Header("Coupon Settings")]
    public GameObject couponButton;
    public UIPopAnimation couponButtonPopAnim;
    public string couponUrl = "https://line.me/";
    private const string CouponClaimedKey = "HasClaimedCoupon";

    [Header("Fade Settings")]
    public float screenFadeDuration = 0.5f;
    public Color fadeColor = Color.black;
    private CanvasGroup fadeCanvasGroup;

    [Header("Status UI Reference")]
    public TextMeshProUGUI remainingToClearText;
    public TextMeshProUGUI remainingMissesText;

    [Header("Score & Ranking UI Reference")]
    public GameObject scorePanel;
    public TextMeshProUGUI scoreText;
    public GameObject rankingPanel;          
    public TextMeshProUGUI rankingText;

    [Header("Timer Settings")]
    public Slider timerSlider;
    public float timeLimit = 10f;
    private float currentTimer = 0f;
    private bool isTimerRunning = false;

    private float totalRemainingTime = 0f;
    private int totalAnswerAttempts = 0;

    [Header("UI Image Settings (〇×表示用)")]
    public Image markImage;
    public Sprite correctSprite;
    public Sprite incorrectSprite;

    [Header("傾き・キー状態のUI画像設定")]
    public Image statusImage;
    public float maxTiltAngle = 30f;
    public float tiltSmoothSpeed = 10f;

    [Header("Quiz Data (日本語)")]
    public Question[] questionsJapanese;

    [Header("Quiz Data (English)")]
    public Question[] questionsEnglish;

    private List<Question> remainingQuestions = new List<Question>();
    private int correctCount = 0;
    private const int targetCorrectCount = 10;

    [Header("Game Rules")]
    private int missCount = 0;
    private const int maxMissCount = 3;

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
    public AudioClip gameOverSE;   

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

    [Header("UI Pop Animations (ピコン演出用参照)")]
    public UIPopAnimation questionPopAnim;
    public UIPopAnimation optionLeftPopAnim;
    public UIPopAnimation optionRightPopAnim;
    public UIPopAnimation resultPopAnim;
    public UIPopAnimation remainingClearPopAnim;
    public UIPopAnimation remainingMissesPopAnim;
    
    public UIPopAnimation retryButtonPopAnim;
    public UIPopAnimation titleButtonPopAnim;
    public UIPopAnimation markPopAnim;

    private bool hasAnswered = false;
    private bool waitingForNeutral = false;
    private bool isGameOver = false;
    private bool isButtonTransitioning = false; 
    private Camera mainCamera;

    private bool isReadyToStart = false; 
    private bool isTransitionCoolingDown = false; 

    private static bool isFirstPlay = true; 

    private int lastClearValue = -1;
    private int lastMissValue = -1;

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
        if (couponButton != null) couponButton.SetActive(false);
        if (subText != null) subText.SetActive(false);

        ResetHoldState();

        if (scorePanel != null) scorePanel.SetActive(false);
        if (scoreText != null) scoreText.gameObject.SetActive(false);
        if (rankingPanel != null) rankingPanel.SetActive(false);

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

    #region --- 言語切り替え・テキスト取得ヘルパー ---

    private string GetText(string jp, string en)
    {
        return currentLanguage == Language.Japanese ? jp : en;
    }

    public void SetLanguage(int langIndex)
    {
        currentLanguage = (Language)langIndex;
        UpdateAllUITexts();

        if (currentState == GameState.MainQuiz && remainingQuestions.Count > 0 && !isGameOver)
        {
            ShowQuestion();
        }
    }

    public void UpdateAllUITexts()
    {
        if (retryButtonText != null)
            retryButtonText.text = GetText("もう一度あそぶ", "Play Again");

        if (titleButtonText != null)
            titleButtonText.text = GetText("タイトルへ", "Title Screen");

        if (couponButtonText != null)
            couponButtonText.text = GetText("クーポンGET!", "Get Coupon!");

        UpdateStatusUI();
    }

    #endregion

    #region --- 画面フェード機能 ---

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

        if (isScreenNotLandscape || isDevicePortrait)
        {
            return 0f;
        }
#else
        if (Screen.height > Screen.width)
        {
            return 0f;
        }
#endif

        Vector3 acc = Input.acceleration;
        inputValue = Mathf.Clamp(acc.y / 0.5f, -1f, 1f);

        if (acc.x < 0f)
        {
            inputValue = -inputValue;
        }

        if (Mathf.Abs(inputValue) < 0.05f)
        {
            inputValue = Input.GetAxis("Horizontal");
        }

        return inputValue;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D) && !isGameOver) { DebugSkipToClear(); return; }
        if (Input.GetKeyDown(KeyCode.C)) { ClearRankingData(); }

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

            // 💡 現在の言語に合わせてルール画像の Sprite を切り替える
            Image ruleImg = ruleImageObjectChild.GetComponent<Image>();
            if (ruleImg != null)
            {
                if (currentLanguage == Language.Japanese && ruleSpriteJapanese != null)
                {
                    ruleImg.sprite = ruleSpriteJapanese;
                }
                else if (currentLanguage == Language.English && ruleSpriteEnglish != null)
                {
                    ruleImg.sprite = ruleSpriteEnglish;
                }
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

        if (remainingToClearText != null) 
            remainingToClearText.text = GetText("【チュートリアル】", "【TUTORIAL】");
        if (remainingMissesText != null) 
            remainingMissesText.text = "";

        if (questionText != null) 
            questionText.text = GetText(
                "練習問題：半田赤レンガは100年以上の歴史を持つ？",
                "Practice: Does the Handa Red Brick Building have over 100 years of history?"
            );

        if (optionLeftText != null) 
            optionLeftText.text = GetText("← ○（そう！）", "← YES");

        if (optionRightText != null) 
            optionRightText.text = GetText("×（ちがう） →", "NO →");

        PlayQuestionPop();
        PlayOptionsPop();

        SetResultText(GetText(
            "スマホを「左」にしっかり傾けて「○」を選んでみよう！",
            "Tilt your phone to the LEFT to select 'YES'!"
        ));

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
            if (Mathf.Abs(inputValue) < resetThreshold)
            {
                waitingForNeutral = false;
            }
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
        
        SetResultText(GetText(
            "スマホを「左」に傾けて「○」を選んでみよう！",
            "Tilt your phone to the LEFT to select 'YES'!"
        ));

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
            "全10問クリアを目指そう！準備ができたら\n【 スマホを左右どちらかに傾けて 】スタート！",
            "Aim to clear all 10 questions!\nTilt your phone left or right to START!"
        ));

        isReadyToStart = true; 
        StartTransitionCoolDown(0.5f);
    }

    void StartMainQuiz()
    {
        currentState = GameState.MainQuiz;
        SetQuizUIActive(true);
        waitingForNeutral = true; 
        ResetHoldState();

        InitializeQuiz();
        ShowQuestion();
    }

    void SetQuizUIActive(bool active)
    {
        if (questionPanel != null) questionPanel.SetActive(active);
        if (optionLeftPanel != null) optionLeftPanel.SetActive(active);
        if (optionRightPanel != null) optionRightPanel.SetActive(active);
        if (resultPanel != null) resultPanel.SetActive(active);

        if (timerSlider != null) timerSlider.gameObject.SetActive(active);
        if (remainingToClearText != null) remainingToClearText.gameObject.SetActive(active);
        if (remainingMissesText != null) remainingMissesText.gameObject.SetActive(active);
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

    #endregion

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
                SetResultText("<color=blue>Time Up!</color>");
                Question currentQ = remainingQuestions[0];
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

    void InitializeQuiz()
    {
        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;

        remainingQuestions = new List<Question>(sourceQuestions);
        ShuffleQuestions(remainingQuestions);

        correctCount = 0;
        missCount = 0;
        totalRemainingTime = 0f;
        totalAnswerAttempts = 0;
        isGameOver = false;

        lastClearValue = -1;
        lastMissValue = -1;

        if (minecartGraphics != null)
        {
            minecartGraphics.ResetCart();
        }

        UpdateStatusUI();
    }

    void ShuffleQuestions(List<Question> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            Question temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
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
        int remainingToClear = Mathf.Max(0, targetCorrectCount - correctCount);
        int remainingMisses = Mathf.Max(0, maxMissCount - missCount);

        if (remainingToClearText != null)
        {
            remainingToClearText.text = GetText($"クリアまであと: {remainingToClear}問", $"To Clear: {remainingToClear}");
            if (lastClearValue != -1 && lastClearValue != remainingToClear)
            {
                if (remainingClearPopAnim != null) remainingClearPopAnim.PlayPop();
            }
            lastClearValue = remainingToClear;
        }

        if (remainingMissesText != null)
        {
            remainingMissesText.text = GetText($"あと {remainingMisses} 問ミスでゲームオーバー", $"Misses left: {remainingMisses}");
            if (lastMissValue != -1 && lastMissValue != remainingMisses)
            {
                if (remainingMissesPopAnim != null) remainingMissesPopAnim.PlayPop();
            }
            lastMissValue = remainingMisses;
        }
    }

    void ShowQuestion()
    {
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);

        UpdateStatusUI();

        if (correctCount >= targetCorrectCount)
        {
            if (questionText != null) 
                questionText.text = GetText("クリアおめでとうございます！", "Congratulations! You Cleared!");
            PlayQuestionPop();
            
            if (optionLeftPanel != null) optionLeftPanel.SetActive(false);
            if (optionRightPanel != null) optionRightPanel.SetActive(false);
            if (subText != null) subText.SetActive(false);

            SetResultText(GetText("10問正解を達成しました！", "You answered 10 questions correctly!"));

            hasAnswered = true;
            isGameOver = true;
            isTimerRunning = false;

            PlaySE(clearSE);

            CalculateAndShowScore();
            ShowEndButtons();

            return;
        }

        if (remainingQuestions.Count == 0)
        {
            Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
            remainingQuestions = new List<Question>(sourceQuestions);
            ShuffleQuestions(remainingQuestions);
        }

        if (subText != null) subText.SetActive(true);

        Question q = remainingQuestions[0];
        
        if (questionText != null) questionText.text = $"Q{correctCount + 1}. {q.questionText}?";
        PlayQuestionPop();
        
        if (optionLeftPanel != null) optionLeftPanel.SetActive(true);
        if (optionLeftText != null) optionLeftText.text = "← " + q.optionLeft;
        
        if (optionRightPanel != null) optionRightPanel.SetActive(true);
        if (optionRightText != null) optionRightText.text = q.optionRight + " →";

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

        totalAnswerAttempts++;

        Question q = remainingQuestions[0];
        bool isCorrect = (choseLeft == q.isLeftCorrect);

        ShowMark(isCorrect ? correctSprite : incorrectSprite);

        if (isCorrect)
        {
            PlaySE(correctSE);
            correctCount++;
            totalRemainingTime += Mathf.Max(0f, currentTimer);
            remainingQuestions.RemoveAt(0);
            
            SetResultText(GetText("<color=red>正解!</color>", "<color=red>Correct!</color>"));

            if (mainCamera != null) mainCamera.backgroundColor = correctColor;
        }
        else
        {
            PlaySE(incorrectSE);
            missCount++;

            if (minecartGraphics != null)
            {
                minecartGraphics.OnMiss();
            }

            if (currentTimer > 0f)
            {
                SetResultText(GetText(
                    $"<color=blue>不正解...</color> (ミス: {missCount}/{maxMissCount})",
                    $"<color=blue>Wrong...</color> (Misses: {missCount}/{maxMissCount})"
                ));
            }
            if (mainCamera != null) mainCamera.backgroundColor = incorrectColor;

            Question wrongQ = remainingQuestions[0];
            remainingQuestions.RemoveAt(0);
            remainingQuestions.Add(wrongQ);

            if (missCount >= maxMissCount)
            {
                isGameOver = true;
                UpdateStatusUI();
                Invoke(nameof(ShowGameOver), 1.5f);
                return;
            }
        }

        UpdateStatusUI();

        Invoke(nameof(NextQuestion), 1.5f);
    }

    #region --- アニメーションヘルパー ---

    private void ShowMark(Sprite sprite)
    {
        if (markImage != null)
        {
            markImage.gameObject.SetActive(true);
            markImage.sprite = sprite;

            if (markPopAnim != null)
            {
                markPopAnim.PlayPop();
            }
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

    #region --- オーディオ・フェード処理 ---

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
        if (targetVolume <= 0f)
        {
            source.Stop();
        }
    }

    void PlaySE(AudioClip clip)
    {
        if (seAudioSource != null && clip != null)
        {
            seAudioSource.PlayOneShot(clip);
        }
    }

    #endregion

    void CalculateAndShowScore()
    {
        float accuracyRate = ((float)targetCorrectCount / totalAnswerAttempts) * 100f;
        int finalScore = Mathf.FloorToInt(accuracyRate * totalRemainingTime * 10f);

        if (scorePanel != null) scorePanel.SetActive(true);

        if (scoreText != null)
        {
            scoreText.gameObject.SetActive(true);
            scoreText.text = GetText(
                $"正答率: {accuracyRate:F1}%\n" +
                $"残り時間合計: {totalRemainingTime:F1}秒\n" +
                $"<size=120%><color=green>SCORE: {finalScore}</color></size>",

                $"Accuracy: {accuracyRate:F1}%\n" +
                $"Time Left: {totalRemainingTime:F1}s\n" +
                $"<size=120%><color=green>SCORE: {finalScore}</color></size>"
            );
        }

        SaveAndShowRanking(finalScore);
    }

    void SaveAndShowRanking(int newScore)
    {
        List<int> ranking = new List<int>();

        for (int i = 1; i <= 5; i++)
        {
            ranking.Add(PlayerPrefs.GetInt($"HighScore_{i}", 0));
        }

        ranking.Add(newScore);
        ranking.Sort((a, b) => b.CompareTo(a));

        for (int i = 0; i < 5; i++)
        {
            PlayerPrefs.SetInt($"HighScore_{i + 1}", ranking[i]);
        }
        PlayerPrefs.Save();

        if (rankingPanel != null) rankingPanel.SetActive(true);

        if (rankingText != null)
        {
            rankingText.gameObject.SetActive(true);
            string rankingString = "【 RANKING 】\n";
            for (int i = 0; i < 5; i++)
            {
                string rankSuffix = (i == 0) ? "st" : (i == 1) ? "nd" : (i == 2) ? "rd" : "th";
                string rankLabel = GetText($"{i + 1}位", $"{i + 1}{rankSuffix}");
                rankingString += $"{rankLabel}: {ranking[i]}pt\n";
            }
            rankingText.text = rankingString;
        }
    }

    void ShowGameOver()
    {
        PlaySE(gameOverSE);

        if (questionText != null) 
            questionText.text = GetText("ゲームオーバー", "GAME OVER");
        PlayQuestionPop();

        if (optionLeftPanel != null) optionLeftPanel.SetActive(false);
        if (optionRightPanel != null) optionRightPanel.SetActive(false);
        if (subText != null) subText.SetActive(false);

        SetResultText(GetText("3回間違えたため失敗です。", "Failed due to 3 wrong answers."));

        isTimerRunning = false;
        if (markImage != null) markImage.gameObject.SetActive(false);

        ShowEndButtons();
    }

    void ShowEndButtons()
    {
        if (retryButton != null) retryButton.SetActive(true);
        if (titleButton != null) titleButton.SetActive(true);

        if (correctCount >= targetCorrectCount && couponButton != null)
        {
            bool hasClaimed = PlayerPrefs.GetInt(CouponClaimedKey, 0) == 1;
            if (!hasClaimed)
            {
                couponButton.SetActive(true);
                if (couponButtonPopAnim != null) couponButtonPopAnim.PlayPop();
            }
        }
    }

    void NextQuestion()
    {
        float inputValue = GetInputValue();
        if (Mathf.Abs(inputValue) >= resetThreshold)
        {
            waitingForNeutral = true;
            SetResultText(GetText("スマホを水平に戻すか、キーを離してください。", "Return your phone to level or release keys."));
        }

        ShowQuestion();
    }

    #region --- ボタン処理 ---

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

    public void OnClickCouponButton()
    {
        if (isButtonTransitioning) return;

        if (seAudioSource != null && buttonSE != null)
        {
            seAudioSource.PlayOneShot(buttonSE);
        }

        if (couponButtonPopAnim != null) couponButtonPopAnim.PlayPop();

        PlayerPrefs.SetInt(CouponClaimedKey, 1);
        PlayerPrefs.Save();

#if UNITY_WEBGL && !UNITY_EDITOR
        OpenUrlInSameTab(couponUrl);
#else
        Application.OpenURL(couponUrl);
#endif

        if (couponButton != null) couponButton.SetActive(false);
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

    void DebugSkipToClear()
    {
        currentState = GameState.MainQuiz;
        SetQuizUIActive(true);
        correctCount = targetCorrectCount;
        totalAnswerAttempts = 10;
        totalRemainingTime = 50f;
        ShowQuestion();
    }

    void ClearRankingData()
    {
        for (int i = 1; i <= 5; i++)
        {
            PlayerPrefs.DeleteKey($"HighScore_{i}");
        }
        PlayerPrefs.DeleteKey(CouponClaimedKey);
        PlayerPrefs.Save();
        Debug.Log("ランキングデータおよびクーポン獲得履歴を削除しました！");

        if (rankingText != null && rankingText.gameObject.activeSelf)
        {
            string rankLabel = GetText("位", "pt");
            rankingText.text = "【 RANKING 】\n1位: 0pt\n2位: 0pt\n3位: 0pt\n4位: 0pt\n5位: 0pt\n";
        }
    }
    #endregion 
}
