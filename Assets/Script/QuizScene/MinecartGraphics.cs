using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class MinecartGraphics : MonoBehaviour
{
    [Header("Beer Cart Sprites (ミス回数ごとの画像)")]
    [Tooltip("0: 満杯(初期) / 1: 半分 / 2: 残り2本 / 3: 空っぽ")]
    public Sprite[] cartSprites;

    [Header("Impact Animation Settings (ガチャン演出設定)")]
    [Tooltip("切り替え時の瞬間拡大倍率")]
    public float punchScale = 1.25f;

    [Tooltip("衝撃の演出時間（秒）")]
    public float animDuration = 0.25f;

    [Tooltip("切り替え時の衝撃フラッシュカラー（うっすら赤み）")]
    public Color impactColor = new Color(1f, 0.6f, 0.6f, 1f);

    private Image cartImage;
    private int currentMissCount = 0;
    private Vector3 originalScale;
    private Color originalColor;
    private Coroutine currentAnimCoroutine;

    void Awake()
    {
        cartImage = GetComponent<Image>();
        originalScale = transform.localScale;
        if (cartImage != null)
        {
            originalColor = cartImage.color;
        }
    }

    void Start()
    {
        UpdateSprite();
    }

    /// <summary>
    /// 不正解の時に呼び出すメソッド（演出付き）
    /// </summary>
    public void OnMiss()
    {
        currentMissCount++;
        
        if (cartSprites != null && currentMissCount >= cartSprites.Length)
        {
            currentMissCount = cartSprites.Length - 1;
        }

        // 演出アニメーションを開始
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
        }
        currentAnimCoroutine = StartCoroutine(PlayImpactAnimation());
    }

    /// <summary>
    /// ゲームリセット時にミス回数を0に戻す
    /// </summary>
    public void ResetCart()
    {
        currentMissCount = 0;
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
        }
        transform.localScale = originalScale;
        if (cartImage != null) cartImage.color = originalColor;

        UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (cartSprites != null && cartSprites.Length > 0 && cartImage != null)
        {
            cartImage.sprite = cartSprites[currentMissCount];
        }
    }

    /// <summary>
    /// ガチャン！と切り替わる手応えアニメーション
    /// </summary>
    private IEnumerator PlayImpactAnimation()
    {
        // 1. スプライトの切り替え
        UpdateSprite();

        float elapsed = 0f;

        // 2. 瞬間拡大 ＆ 赤色フラッシュ（「ガッ！」と衝撃が入る瞬間）
        transform.localScale = originalScale * punchScale;
        if (cartImage != null) cartImage.color = impactColor;

        // 一瞬ヒットストップ感を入れるため少しだけ待つ
        yield return new WaitForSeconds(0.04f);

        // 3. 元の大きさ・色へ跳ね返りながら滑らかに戻る（バウンドイージング）
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;

            // Elastic / Overshoot 的なバウンド曲線（勢いよく戻ってピタッと止まる）
            float bounceFactor = Mathf.Sin(t * Mathf.PI * 1.5f) * (1f - t);

            // スケールと色の補間
            transform.localScale = Vector3.Lerp(originalScale, originalScale * punchScale, bounceFactor);
            if (cartImage != null)
            {
                cartImage.color = Color.Lerp(impactColor, originalColor, t);
            }

            yield return null;
        }

        // 4. 確実に元の状態に固定
        transform.localScale = originalScale;
        if (cartImage != null) cartImage.color = originalColor;
    }
}
