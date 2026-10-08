using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleManager : MonoBehaviour
{
    [Header("Gyro Reference")]
    public GyroReceiver gyroReceiver;

    [Header("UI Pop Animation Settings")]
    public UIPopAnimation startButtonPopAnim;    // スタートボタン用ポップ演出
    public UIPopAnimation japaneseButtonPopAnim; // 日本語ボタン用ポップ演出
    public UIPopAnimation englishButtonPopAnim;  // 英語ボタン用ポップ演出

    [Header("UI Panels & Buttons")]
    public GameObject startButton;            // スタートボタン
    public GameObject languageSelectPanel;     // 言語選択ボタン（日本語/English）をまとめた親パネル

    [Header("Button Text References (固定表示切り替え用)")]
    public TextMeshProUGUI startButtonText;
    public TextMeshProUGUI japaneseButtonText;
    public TextMeshProUGUI englishButtonText;

    [Header("Audio Settings")]
    public AudioSource bgmAudioSource;
    public AudioSource seAudioSource;
    public AudioClip bgmClip;
    public AudioClip buttonSE;
    public string quizSceneName = "QuizScene";

    [Header("BGM Fade Settings")]
    public float defaultBgmVolume = 0.5f;
    public float fadeDuration = 1.0f;
    private Coroutine bgmFadeCoroutine;

    [Header("Fade Settings")]
    public float screenFadeDuration = 0.5f;          // 画面フェードにかかる時間（秒）
    public Color fadeColor = Color.black;            // フェード用パネルの色
    private CanvasGroup fadeCanvasGroup;             // フェード制御用CanvasGroup

    private bool isTransitioning = false;

    private const string LanguagePrefKey = "SelectedLanguage"; // PlayerPrefs用キー

    void Start()
    {
        // 言語選択パネルの初期状態は非表示
        if (languageSelectPanel != null)
        {
            languageSelectPanel.SetActive(false);
        }

        if (startButton != null)
        {
            startButton.SetActive(true);
            
            // 画面開始時にスタートボタンをポンッと出す
            if (startButtonPopAnim != null)
            {
                startButtonPopAnim.PlayPop();
            }
        }

        // 画面フェード用UIの初期化とフェードイン開始
        SetupFadeUI();
        StartCoroutine(FadeInScreen());

        PlayBGMWithFadeIn();
    }

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

    /// <summary>
    /// スタートボタンを押した時の処理（言語選択パネルを表示）
    /// </summary>
    public void OnClickStartButton()
    {
        if (isTransitioning) return;

        if (startButtonPopAnim != null)
        {
            startButtonPopAnim.PlayPop();
        }
        PlayButtonSE();

        if (startButton != null)
        {
            startButton.SetActive(false);
        }

        if (languageSelectPanel != null)
        {
            languageSelectPanel.SetActive(true);

            if (japaneseButtonPopAnim != null) japaneseButtonPopAnim.PlayPop();
            if (englishButtonPopAnim != null) englishButtonPopAnim.PlayPop();
        }
    }

    /// <summary>
    /// 「日本語」ボタンを押したときのイベントハンドラ
    /// </summary>
    public void OnClickJapaneseButton()
    {
        if (japaneseButtonPopAnim != null) japaneseButtonPopAnim.PlayPop();
        SelectLanguageAndStartGame(Language.Japanese);
    }

    /// <summary>
    /// 「English」ボタンを押したときのイベントハンドラ
    /// </summary>
    public void OnClickEnglishButton()
    {
        if (englishButtonPopAnim != null) englishButtonPopAnim.PlayPop();
        SelectLanguageAndStartGame(Language.English);
    }

    /// <summary>
    /// 言語設定を保存してゲームシーンへ遷移
    /// </summary>
    private void SelectLanguageAndStartGame(Language lang)
    {
        if (isTransitioning) return;
        isTransitioning = true;

        // 選択された言語を PlayerPrefs に保存
        PlayerPrefs.SetInt(LanguagePrefKey, (int)lang);
        PlayerPrefs.Save();

        // WebGLのジャイロ利用許可をリクエスト
        if (gyroReceiver != null)
        {
            gyroReceiver.EnableGyroPermission();
        }

        StartCoroutine(PlaySEAndChangeScene(quizSceneName));
    }

    private void PlayButtonSE()
    {
        if (seAudioSource != null && buttonSE != null)
        {
            seAudioSource.PlayOneShot(buttonSE);
        }
    }

    private IEnumerator PlaySEAndChangeScene(string sceneName)
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

    #endregion
}
