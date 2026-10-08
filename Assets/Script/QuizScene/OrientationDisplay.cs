using UnityEngine;
using TMPro;

public class OrientationDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI statusText;

    private GyroReceiver gyroReceiver;

    void Start()
    {
        gyroReceiver = FindAnyObjectByType<GyroReceiver>();
    }

    void Update()
    {
        if (statusText == null) return;

        if (gyroReceiver == null)
        {
            statusText.text = "GyroReceiver が見つかりません";
            return;
        }

        // 横持ちの向きを判定
        string orientationStr = DetectLandscapeOrientation();

        // テキスト表示
        statusText.text = $"[ 端末の向き・角度検出 ]\n" +
                          $"検出持ち方 : {orientationStr}\n" +
                          $"計算後のGamma: {gyroReceiver.CurrentGamma:F1}°\n" +
                          $"--------------------\n" +
                          $"Raw Beta  : {gyroReceiver.RawBeta:F1}°\n" +
                          $"Raw Gamma : {gyroReceiver.RawGamma:F1}°";
    }

    /// <summary>
    /// GyroReceiverの生データ(Beta/Gamma)とOS設定から横持ち方向を判定
    /// </summary>
    private string DetectLandscapeOrientation()
    {
        // 1. OSの判定が取れている場合
        if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
        {
            return "LandscapeRight (逆さ横持ち)";
        }
        else if (Input.deviceOrientation == DeviceOrientation.LandscapeLeft)
        {
            return "LandscapeLeft (通常横持ち)";
        }

        // 2. ブラウザ等で DeviceOrientation が Unknown になる場合の生の角度による代替判定
        // 横持ち時は Gamma(左右傾き) の絶対値が大きくなるため、Gammaの符号で方向を特定可能
        if (Mathf.Abs(gyroReceiver.RawGamma) > 15f)
        {
            if (gyroReceiver.RawGamma > 0)
            {
                return "LandscapeRight (逆さ横持ち / Raw判定)";
            }
            else
            {
                return "LandscapeLeft (通常横持ち / Raw判定)";
            }
        }

        return "判定中 / 垂直・水平置き";
    }
}
