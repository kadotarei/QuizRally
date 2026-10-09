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
        PreviewQuestion,    // 4. 問題文・選択肢の事前表示（タップでマップへ移行）
        Navigation,         // 5. 周回案内画面（館内地図＋〇〇へ行ってみよう！）
        MainQuiz            // 6. 本番クイズ（回答中）
    }
    private GameState currentState = GameState.RuleScreen;

    [System.Serializable]
    public struct Question
    {
        [TextArea(2, 4)]
        public string navigationText; // 周回案内文章
        public Sprite mapSprite;      // --- 追加: 各問題に対応するマップ画像 ---
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

    [Header("Show Map Button Reference (マップ表示ボタン)")]
    public GameObject showMapButton;               // 問題文・選択肢と同時に出すボタン
    public UIPopAnimation showMapButtonPopAnim;

    [Header("Option Image Reference (選択肢用画像)")]
    public Image optionLeftUI;
    public Image optionRightUI;
    public GameObject optionLeftFrameUI;  // 追加：左イラストの枠画像
    public GameObject optionRightFrameUI; // 追加：右イラストの枠画像

    [Header("Button Text References")]
    public TextMeshProUGUI retryButtonText;
    public TextMeshProUGUI titleButtonText;
    public TextMeshProUGUI useCouponButtonText;
    public TextMeshProUGUI showMapButtonText;
    public TextMeshProUGUI arrivedButtonText;

    [Header("UI Container Reference")]
    public GameObject questionPanel;
    public GameObject optionLeftPanel;
    public GameObject optionRightPanel;
    public GameObject resultPanel;

    [Header("Score & Ranking UI Reference")]
    public GameObject scoreFrameImage;        // スコア枠（画像オブジェクト）
    public TextMeshProUGUI scoreText;          // スコア詳細表示テキスト
    public UIPopAnimation scoreFramePopAnim;   // スコア枠アニメーション

    public GameObject rankingFrameImage;      // ランキング枠（画像オブジェクト）
    public TextMeshProUGUI rankingText;        // ランキング表示テキスト
    public UIPopAnimation rankingFramePopAnim; // ランキング枠アニメーション

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
    public GameObject questionNumberFrameImage; // 「第◯問 / 全◯問」テキストの枠画像オブジェクト

    [Header("Timer Settings")]
    public Slider timerSlider;
    public Image timerFillImage;            // --- 追加: スライダーの Fill 画像 (Fill Area/Fill) ---
    public RectTransform timerHandleRect;   // --- 追加: トロッコ Handle の RectTransform ---
    public float timeLimit = 15f;
    private float currentTimer = 0f;
    private bool isTimerRunning = false;

    // --- 追加: トロッコ ガタゴト振動のパラメータ ---
    public float rumbleFrequency = 25f;     // 揺れる速度
    public float rumbleAmountY = 3.0f;      // 上下の揺れ幅(ピクセル)
    public float rumbleAmountRotation = 4f; // 傾きの揺れ幅(度)
    private Vector2 handleOriginalAnchoredPos; // ← これを追加（波線が消えます）

    private float totalTimeSpent = 0f;       // 回答にかかった合計時間（秒）
    private int finalScore = 0;              // 最終算出スコア

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
    private Coroutine mapButtonPulseCoroutine; // 追加：マップボタンのアピール演出コルーチン
    public UIPopAnimation optionLeftFramePopAnim;        // --- 追加: 左イラスト枠用ポップアニメーション ---
    public UIPopAnimation optionRightFramePopAnim;       // --- 追加: 右イラスト枠用ポップアニメーション ---
    public UIPopAnimation questionNumberFramePopAnim;    // --- 追加: 「第◯問 / 全◯問」枠用ポップアニメーション ---


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

        if (timerHandleRect != null)
        {
            handleOriginalAnchoredPos = timerHandleRect.anchoredPosition;
        }

        mainCamera = Camera.main;
        if (mainCamera == null) mainCamera = FindAnyObjectByType<Camera>();
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;

        if (markImage != null) markImage.gameObject.SetActive(false);
        if (retryButton != null) retryButton.SetActive(false);
        if (titleButton != null) titleButton.SetActive(false);
        if (couponPanel != null) couponPanel.SetActive(false);
        if (navigationPanel != null) navigationPanel.SetActive(false);
        if (showMapButton != null) showMapButton.SetActive(false);

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
        if (optionLeftFrameUI != null) optionLeftFrameUI.SetActive(false);
        if (optionRightFrameUI != null) optionRightFrameUI.SetActive(false);
    }



    #region --- 環境判定ヘルパー ---

    private bool IsMobilePlatform()
    {
        // 1. エディタ実行時はPCとしてテストできるようにする
        if (Application.isEditor)
        {
            return false;
        }

        // 2. 明確なPC系OS（Windows / Mac OS X / Linux）かつタッチ操作非対応の場合はPC（キーボード操作）判定
        bool isDesktopOS = SystemInfo.operatingSystem.Contains("Windows") ||
                           SystemInfo.operatingSystem.Contains("Mac") ||
                           SystemInfo.operatingSystem.Contains("Linux");

        // Input.touchSupported を使用してタッチ操作に対応しているか判定
        if (isDesktopOS && Input.touchSupported == false)
        {
            return false; // PC環境
        }

        // 3. それ以外（Android / iOS / スマホブラウザ / 判断がつかない場合）はデフォルトでスマホ判定
        return true;
    }

    private string GetOperationGuideText()
    {
        if (IsMobilePlatform())
        {
            return GetText("スマホを傾けて選んでね！", "Tilt your phone to choose!");
        }
        else
        {
            return GetText("左右キー（← / →）で選んでね！", "Use Left/Right arrow keys to choose!");
        }
    }

    // --- 「次へ進む」専用の案内文言 ---
    private string GetAdvanceGuideText()
    {
        if (IsMobilePlatform())
        {
            return GetText("スマホを傾けて次へ進んでね！", "Tilt your phone to continue!");
        }
        else
        {
            return GetText("左右キー（← / →）で次へ進んでね！", "Use Left/Right arrow keys to continue!");
        }
    }

    // --- 追加: 「スタート」専用の案内文言 ---
    private string GetStartGuideText()
    {
        if (IsMobilePlatform())
        {
            return GetText("スマホを傾けてスタート！", "Tilt your phone to START!");
        }
        else
        {
            return GetText("左右キー（← / →）を押してスタート！", "Use Left/Right arrow keys to START!");
        }
    }

    #endregion



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

        // --- 修正: クーポンの使用状態に合わせてテキストを更新 ---
        if (useCouponButtonText != null)
        {
            bool isUsed = PlayerPrefs.GetInt(CouponUsedKey, 0) == 1;
            useCouponButtonText.text = isUsed ? 
                GetText("クーポン使用済み", "Coupon Used") : 
                GetText("クーポンを使用する", "Use Coupon");
        }

        if (showMapButtonText != null)
            showMapButtonText.text = GetText("マップを確認してみよう！", "Let's Check the Map!");

        if (arrivedButtonText != null)
            arrivedButtonText.text = GetText("到着した！", "I've Arrived!");

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

            case GameState.PreviewQuestion:
                break;

            case GameState.Navigation:
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
        
        // --- 修正: GetOperationGuideText() から GetAdvanceGuideText() に変更 ---
        SetResultText(GetText(
            $"【 ルール説明 】\n確認したら、{GetAdvanceGuideText()}",
            $"【 HOW TO PLAY 】\nWhen ready, {GetAdvanceGuideText()}"
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

        // --- 修正: 左右の矢印を削除 ---
        if (optionLeftText != null) optionLeftText.text = GetText("○（そう！）", "YES");
        if (optionRightText != null) optionRightText.text = GetText("×（ちがう）", "NO");

        PlayQuestionPop();
        PlayOptionsPop();

        string tutorialGuide = IsMobilePlatform() ?
            GetText("スマホを傾けて正解を選んでね！", " Tilt your phone to choose the correct answer!") :
            GetText("キーボードの左右キーを押して正解を選んでね！", "Use the left and right arrow keys on your keyboard to select the correct answer!");

        SetResultText(tutorialGuide);

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
            SetResultText(GetText("おっと！「左」を選んでね！", "Oops! Select 'LEFT'!"));
            Invoke(nameof(ResetTutorialState), 1.5f);
        }
    }


void ResetTutorialState()
    {
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);
        
        string tutorialGuide = IsMobilePlatform() ?
            GetText("スマホを傾けて正解を選んでね！", " Tilt your phone to choose the correct answer!") :
            GetText("キーボードの左右キーを押して正解を選んでね！", "Use the left and right arrow keys on your keyboard to select the correct answer!");

        SetResultText(tutorialGuide);

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
            $"全5問に挑戦しよう！準備ができたら\n{GetStartGuideText()}",
            $"Try all 5 questions!\nWhen ready, {GetStartGuideText()}"
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

        ShowQuestionPreview();
    }

        private void ShowQuestionPreview()
    {
        currentState = GameState.PreviewQuestion;
        SetQuizUIActive(false); 
        if (navigationPanel != null) navigationPanel.SetActive(false);
        if (mainCamera != null) mainCamera.backgroundColor = defaultColor;
        if (markImage != null) markImage.gameObject.SetActive(false);

        if (questionPanel != null) questionPanel.SetActive(true);
        if (optionLeftPanel != null) optionLeftPanel.SetActive(true);
        if (optionRightPanel != null) optionRightPanel.SetActive(true);
        if (progressStatusText != null) progressStatusText.gameObject.SetActive(true);
        
        // --- 修正: 第◯問枠の表示とアニメーション再生 ---
        if (questionNumberFrameImage != null) 
        {
            questionNumberFrameImage.SetActive(true);
            if (questionNumberFramePopAnim != null) questionNumberFramePopAnim.PlayPop();
        }

        UpdateStatusUI();

        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
        Question q = sourceQuestions[currentQuestionIndex];
        
        if (questionText != null) questionText.text = $"Q{currentQuestionIndex + 1}. {q.questionText}";
        
        if (optionLeftText != null) optionLeftText.text = q.optionLeft;
        if (optionRightText != null) optionRightText.text = q.optionRight;

        // --- 修正: 左イラスト & 枠の表示とアニメーション再生 ---
        if (optionLeftUI != null)
        {
            bool hasImg = q.optionLeftImage != null;
            optionLeftUI.sprite = q.optionLeftImage;
            optionLeftUI.gameObject.SetActive(hasImg);
            
            if (optionLeftFrameUI != null) 
            {
                optionLeftFrameUI.SetActive(hasImg);
                if (hasImg && optionLeftFramePopAnim != null) optionLeftFramePopAnim.PlayPop();
            }
            if (hasImg && optionLeftImagePopAnim != null) optionLeftImagePopAnim.PlayPop();
        }

        // --- 修正: 右イラスト & 枠の表示とアニメーション再生 ---
        if (optionRightUI != null)
        {
            bool hasImg = q.optionRightImage != null;
            optionRightUI.sprite = q.optionRightImage;
            optionRightUI.gameObject.SetActive(hasImg);
            
            if (optionRightFrameUI != null) 
            {
                optionRightFrameUI.SetActive(hasImg);
                if (hasImg && optionRightFramePopAnim != null) optionRightFramePopAnim.PlayPop();
            }
            if (hasImg && optionRightImagePopAnim != null) optionRightImagePopAnim.PlayPop();
        }

        PlayQuestionPop();
        PlayOptionsPop();

        if (showMapButton != null)
        {
            showMapButton.SetActive(true);
            if (showMapButtonPopAnim != null) showMapButtonPopAnim.PlayPop();
            StartMapButtonPulseAnimation();
        }

        ResetHoldState();
        StartTransitionCoolDown(0.3f);
    }


    public void OnClickShowMapButton()
    {
        if (currentState != GameState.PreviewQuestion || isTransitionCoolingDown || isButtonTransitioning) return;

        StartCoroutine(ShowMapButtonRoutine());
    }

    private IEnumerator ShowMapButtonRoutine()
    {
        isButtonTransitioning = true;

        // アピール演出を停止
        StopMapButtonPulseAnimation();

        PlaySE(buttonSE);
        if (showMapButtonPopAnim != null) showMapButtonPopAnim.PlayPop();

        float waitTime = 0.25f;
        if (buttonSE != null) waitTime = Mathf.Max(waitTime, buttonSE.length);
        yield return new WaitForSeconds(waitTime);

        if (showMapButton != null) showMapButton.SetActive(false);
        SetQuizUIActive(false);
        HideOptionImages();

        ShowNavigationScreen();
        isButtonTransitioning = false;
    }
       #region --- マップボタン アピール演出 ---

    private void StartMapButtonPulseAnimation()
    {
        StopMapButtonPulseAnimation();
        if (showMapButton != null)
        {
            mapButtonPulseCoroutine = StartCoroutine(PulseMapButtonRoutine());
        }
    }

    private void StopMapButtonPulseAnimation()
    {
        if (mapButtonPulseCoroutine != null)
        {
            StopCoroutine(mapButtonPulseCoroutine);
            mapButtonPulseCoroutine = null;
        }

        if (showMapButton != null)
        {
            showMapButton.transform.localScale = Vector3.one;
            showMapButton.transform.localRotation = Quaternion.identity;
        }
    }

    private IEnumerator PulseMapButtonRoutine()
    {
        Transform btnTransform = showMapButton.transform;
        Vector3 baseScale = Vector3.one;

        float pulseSpeed = 4.0f;   // ポヨンポヨンの速度
        float scaleAmount = 0.08f; // 拡大率 (最大1.08倍)
        float rotateAmount = 1.2f; // --- 修正: 傾く角度をかなり控えめに (3.5f -> 1.2f) ---

        while (showMapButton != null && showMapButton.activeSelf)
        {
            float time = Time.time * pulseSpeed;
            float sineValue = Mathf.Sin(time);

            // 1. ポヨンポヨンと拡大縮小
            float scaleOffset = sineValue * scaleAmount;
            btnTransform.localScale = baseScale + new Vector3(scaleOffset, scaleOffset, 0f);

            // 2. 控えめな左右の揺れ
            float angle = Mathf.Sin(time * 0.8f) * rotateAmount;
            btnTransform.localRotation = Quaternion.Euler(0f, 0f, angle);

            yield return null;
        }

        // 非表示・終了時は元に戻す
        if (btnTransform != null)
        {
            btnTransform.localScale = Vector3.one;
            btnTransform.localRotation = Quaternion.identity;
        }
    }

    #endregion

    void ShowNavigationScreen()
    {
        currentState = GameState.Navigation;
        isButtonTransitioning = false; 
        SetQuizUIActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        UpdateAllUITexts();

        if (navigationPanel != null) navigationPanel.SetActive(true);

        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
        if (currentQuestionIndex < sourceQuestions.Length)
        {
            Question currentQ = sourceQuestions[currentQuestionIndex];
            
            // 周回案内テキストの更新
            if (navigationText != null)
            {
                string headerText = GetText(
                    $"【第 {currentQuestionIndex + 1} 問目のスポット】",
                    $"[Spot for Question {currentQuestionIndex + 1}]"
                );
                navigationText.text = $"{headerText}\n{currentQ.navigationText}";
            }

            // --- 追加: 各問題に対応したマップ画像をUIに切り替えて表示 ---
            if (mapImage != null)
            {
                if (currentQ.mapSprite != null)
                {
                    mapImage.sprite = currentQ.mapSprite;
                    mapImage.gameObject.SetActive(true);
                }
                else
                {
                    // 画像が登録されていない場合は非表示（またはデフォルト画像にする場合は適宜調整）
                    mapImage.gameObject.SetActive(false);
                }
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
        
        if (questionNumberFrameImage != null) 
        {
            questionNumberFrameImage.SetActive(active);
            if (active && questionNumberFramePopAnim != null) questionNumberFramePopAnim.PlayPop();
        }
        
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
            float progress = Mathf.Clamp01(currentTimer / timeLimit);

            if (timerSlider != null)
            {
                timerSlider.value = progress;
            }

            // --- 追加: Fill の表示/非表示切り替え ---
            if (timerFillImage != null)
            {
                timerFillImage.gameObject.SetActive(progress > 0f);
            }

            // --- 追加: トロッコのガタゴト振動アニメーション ---
            if (timerHandleRect != null)
            {
                if (progress > 0f)
                {
                    float yOffset = Mathf.Sin(Time.time * rumbleFrequency) * rumbleAmountY;
                    float rotOffset = Mathf.Sin(Time.time * rumbleFrequency * 0.7f) * rumbleAmountRotation;

                    timerHandleRect.anchoredPosition = handleOriginalAnchoredPos + new Vector2(0f, yOffset);
                    timerHandleRect.localRotation = Quaternion.Euler(0f, 0f, rotOffset);
                }
                else
                {
                    // タイマー終了時は元の姿勢に戻す
                    timerHandleRect.anchoredPosition = handleOriginalAnchoredPos;
                    timerHandleRect.localRotation = Quaternion.identity;
                }
            }

            if (currentTimer <= 0f)
            {
                isTimerRunning = false;
                totalTimeSpent += timeLimit; 
                SetResultText("<color=blue>Time Up!</color>");
                Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
                Question currentQ = sourceQuestions[currentQuestionIndex];
                Answer(!currentQ.isLeftCorrect);
                return;
            }
        }

        // （以降の入力判定等は既存通り）

        float inputValue = GetInputValue();

        if (waitingForNeutral)
        {
            if (Mathf.Abs(inputValue) < resetThreshold)
            {
                waitingForNeutral = false;
                SetResultText(GetOperationGuideText());
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
        
        // --- 修正: "← " を削除 ---
        if (optionLeftText != null) optionLeftText.text = q.optionLeft;
                // 左選択肢イラスト＆枠
           // 左選択肢イラスト＆枠
        if (optionLeftUI != null)
        {
            bool hasImg = q.optionLeftImage != null;
            optionLeftUI.sprite = q.optionLeftImage;
            optionLeftUI.gameObject.SetActive(hasImg);
            
            if (optionLeftFrameUI != null)
            {
                optionLeftFrameUI.SetActive(hasImg);
                if (hasImg && optionLeftFramePopAnim != null) optionLeftFramePopAnim.PlayPop();
            }

            if (hasImg && optionLeftImagePopAnim != null)
            {
                optionLeftImagePopAnim.PlayPop();
            }
        }

        if (optionRightPanel != null) optionRightPanel.SetActive(true);
        if (optionRightText != null) optionRightText.text = q.optionRight;
        
        // 右選択肢イラスト＆枠
        if (optionRightUI != null)
        {
            bool hasImg = q.optionRightImage != null;
            optionRightUI.sprite = q.optionRightImage;
            optionRightUI.gameObject.SetActive(hasImg);
            
            if (optionRightFrameUI != null)
            {
                optionRightFrameUI.SetActive(hasImg);
                if (hasImg && optionRightFramePopAnim != null) optionRightFramePopAnim.PlayPop();
            }

            if (hasImg && optionRightImagePopAnim != null)
            {
                optionRightImagePopAnim.PlayPop();
            }
        }


        PlayOptionsPop();

        if (!waitingForNeutral)
        {
            SetResultText(GetOperationGuideText());
            currentTimer = timeLimit;
            isTimerRunning = true;

            if (timerSlider != null) timerSlider.value = 1f;

            // --- 追加: タイマー開始時に Fill を表示し、Handle の姿態をリセット ---
            if (timerFillImage != null) timerFillImage.gameObject.SetActive(true);
            if (timerHandleRect != null)
            {
                timerHandleRect.anchoredPosition = handleOriginalAnchoredPos;
                timerHandleRect.localRotation = Quaternion.identity;
            }
        }


        hasAnswered = false;
        ResetHoldState();
    }

       void Answer(bool choseLeft)
    {
        hasAnswered = true;
        isTimerRunning = false;
        ResetHoldState();

        float timeTaken = timeLimit - Mathf.Max(0f, currentTimer);
        totalTimeSpent += timeTaken;

        Question[] sourceQuestions = (currentLanguage == Language.Japanese) ? questionsJapanese : questionsEnglish;
        Question q = sourceQuestions[currentQuestionIndex];
        bool isCorrect = (choseLeft == q.isLeftCorrect);

        // --- 修正: 判定表示中もイラスト本体と枠画像を両方表示維持する ---
        if (optionLeftUI != null)
        {
            bool hasImg = q.optionLeftImage != null;
            optionLeftUI.gameObject.SetActive(hasImg);
            if (optionLeftFrameUI != null) optionLeftFrameUI.SetActive(hasImg);
        }
        if (optionRightUI != null)
        {
            bool hasImg = q.optionRightImage != null;
            optionRightUI.gameObject.SetActive(hasImg);
            if (optionRightFrameUI != null) optionRightFrameUI.SetActive(hasImg);
        }

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
            // 次の問題へ移る直前のタイミングでイラストと枠を隠す
            Invoke(nameof(HideOptionImages), 1.5f);
            Invoke(nameof(StartQuestionSequence), 1.5f);
        }
    }

    void CalculateFinalScore()
    {
        int baseScore = correctCount * 2000;
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

        float accuracy = ((float)correctCount / totalQuestionCount) * 100f;

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
        // 正解数に応じた一言メッセージの切替
        string resultHeader;
        if (correctCount == 5)
        {
            resultHeader = GetText("おめでとう！", "Congratulations!");
        }
        else if (correctCount >= 3)
        {
            resultHeader = GetText("おしい！", "So Close!");
        }
        else if (correctCount >= 1)
        {
            resultHeader = GetText("がんばった！", "Good Job!");
        }
        else
        {
            resultHeader = GetText("ざんねん…", "Too Bad...");
        }

        if (clearResultText != null)
        {
            clearResultText.gameObject.SetActive(true);

            if (correctCount == 5)
            {
                // クーポン獲得（全問正解）時
                bool isUsed = PlayerPrefs.GetInt(CouponUsedKey, 0) == 1;
                if (!isUsed)
                {
                    // 初獲得時
                    clearResultText.text = GetText(
                        "全問正解！クーポンをゲットしたよ！",
                        "Perfect score! You got a coupon!"
                    );
                }
                else
                {
                    // すでに獲得（使用）済みの場合
                    clearResultText.text = GetText(
                        "全問正解！クーポン獲得済みです！",
                        "Perfect score! Coupon already claimed!"
                    );
                }
            }
            else
            {
                // クーポン未獲得時
                clearResultText.text = GetText(
                    $"{resultHeader}\n5問中 <color=red>{correctCount}</color> 問正解でした！",
                    $"{resultHeader}\nYou scored <color=red>{correctCount}</color> / {totalQuestionCount}!"
                );
            }

            if (clearResultTextPopAnim != null) clearResultTextPopAnim.PlayPop();
        }


        if (couponPanel != null)
        {
            couponPanel.SetActive(true);
            if (couponImage != null) couponImage.SetActive(true);

            bool isUsed = PlayerPrefs.GetInt(CouponUsedKey, 0) == 1;
            
            if (useCouponButton != null)
            {
                // ボタンは常に表示状態にする
                useCouponButton.SetActive(true);

                // Buttonコンポーネントを取得して有効/無効を切り替え
                Button btn = useCouponButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.interactable = !isUsed; // 使用済みの場合は押せない状態にする
                }

                // ボタンのテキストを切り替え
                if (useCouponButtonText != null)
                {
                    useCouponButtonText.text = isUsed ? 
                        GetText("クーポン使用済み", "Coupon Used") : 
                        GetText("クーポンを使用する", "Use Coupon");
                }

                // 未使用の場合のみポップアニメーションを再生
                if (!isUsed && couponButtonPopAnim != null) couponButtonPopAnim.PlayPop();
            }

            // 使用済みバッジ（済スタンプ）の表示切替
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
        scores.Sort((a, b) => b.CompareTo(a)); 

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

        // クーポン使用状態フラグを保存
        PlayerPrefs.SetInt(CouponUsedKey, 1);
        PlayerPrefs.Save();

        // ボタンを押せない状態（無効）にする
        if (useCouponButton != null)
        {
            Button btn = useCouponButton.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = false;
            }
        }

        // テキストを「クーポン使用済み」に変更する
        if (useCouponButtonText != null)
        {
            useCouponButtonText.text = GetText("クーポン使用済み", "Coupon Used");
        }

        // 使用済みバッジ（済スタンプ）を表示する
        if (usedCouponBadge != null)
        {
            usedCouponBadge.SetActive(true);
        }
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
