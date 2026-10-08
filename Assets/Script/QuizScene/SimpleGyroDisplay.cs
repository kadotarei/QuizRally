using UnityEngine;
using TMPro;

public class SimpleGyroDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI angleText; // 角度を表示するText

    private GyroReceiver gyroReceiver;

    void Start()
    {
        // TiltQuizManagerと同様にGyroReceiverを探して保持
        gyroReceiver = FindAnyObjectByType<GyroReceiver>();
    }

    void Update()
    {
        if (angleText == null) return;

        // 1. TiltQuizManagerと同じ方法で入力値（-1.0 〜 1.0）を取得
        float inputValue = GetInputValue();

        // 2. 入力値を角度（度数法）に変換
        // GetInputValue() の値は概ね -1.0 〜 1.0 なので、45度（または90度）を掛けて角度化します
        // （※ GyroReceiverでは γ / 45f しているため、45を掛けると元のガンマ角になります）
        float currentAngle = inputValue * 45f;

        // 3. 画面にテキストを表示
        angleText.text = $"[ 端末の傾き ]\n" +
                         $"入力値 (Normalized) : {inputValue:F2}\n" +
                         $"傾き角度 (Estimate) : {currentAngle:F1}°";
    }

    /// <summary>
    /// TiltQuizManager.cs と全く同じロジックで傾き入力値を取得する関数
    /// </summary>
    private float GetInputValue()
    {
        float inputValue = 0f;

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL環境（スマホのブラウザ）の場合
        if (gyroReceiver != null)
        {
            inputValue = Mathf.Clamp(gyroReceiver.CurrentGamma / 45f, -1f, 1f);
        }
#else
        // エディター上やiOS/Androidネイティブビルドの場合
        inputValue = Input.acceleration.x;
#endif

        // キーボード（A/Dキーや左右矢印キー）入力のフォールバック処理
        if (Mathf.Abs(inputValue) < 0.05f)
        {
            inputValue = Input.GetAxis("Horizontal");
        }

        return inputValue;
    }
}
