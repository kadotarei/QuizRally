using System.Collections;
using UnityEngine;

public class UIPopAnimation : MonoBehaviour
{
    [Header("Pop Settings")]
    [Tooltip("ピコンッと弾む拡大倍率")]
    public float popScale = 1.18f;

    [Tooltip("アニメーションにかかる時間（秒）")]
    public float duration = 0.2f;

    private Vector3 originalScale;
    private Coroutine currentCoroutine;

    void Awake()
    {
        originalScale = transform.localScale;
    }

    /// <summary>
    /// ピコンッ！と弾む演出を再生
    /// </summary>
    public void PlayPop()
    {
        if (!gameObject.activeInHierarchy) return;

        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        currentCoroutine = StartCoroutine(CoPlayPop());
    }

    private IEnumerator CoPlayPop()
    {
        float elapsed = 0f;

        // 一瞬でグッと大きくなる（前半）
        float upDuration = duration * 0.35f;
        while (elapsed < upDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / upDuration;
            // EaseOutQuad 的な素早い広がり
            float smoothedT = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.Lerp(originalScale, originalScale * popScale, smoothedT);
            yield return null;
        }

        // バウンドしながら元のサイズに戻る（後半）
        elapsed = 0f;
        float downDuration = duration * 0.65f;
        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / downDuration;
            // Sine波を活用してキュッと元の大きさに収まる
            float bounce = Mathf.Sin(t * Mathf.PI) * 0.15f;
            transform.localScale = Vector3.Lerp(originalScale * popScale, originalScale, t) + (originalScale * bounce);
            yield return null;
        }

        transform.localScale = originalScale;
    }
}
