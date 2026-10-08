using UnityEngine;
using TMPro;

public class AllOrientationDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TextMeshProUGUI displayText;

    private GyroReceiver gyroReceiver;

    void Start()
    {
        // 1. スマホ標準のジャイロセンサーを有効化（Native/エディタ用）
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }

        // 2. GyroReceiver（WebGL用）の取得
        gyroReceiver = FindAnyObjectByType<GyroReceiver>();
    }

    void Update()
    {
        if (displayText == null) return;

        // --- 1. Screen Orientation (画面の持ち方設定) ---
        ScreenOrientation screenOrient = Screen.orientation;
        DeviceOrientation deviceOrient = Input.deviceOrientation;

        // --- 2. WebGL / GyroReceiver (JSからの Raw / Current 値) ---
        float rawGamma = 0f, rawBeta = 0f, rawAlpha = 0f;
        float currentGamma = 0f;
        
        if (gyroReceiver != null)
        {
            rawGamma = gyroReceiver.RawGamma;
            rawBeta = gyroReceiver.RawBeta;
            //rawAlpha = gyroReceiver.RawAlpha;
            currentGamma = gyroReceiver.CurrentGamma;
        }

        // --- 3. Native Gyroscope (Input.gyro / Unity標準) ---
        Vector3 gyroGravity = Input.gyro.gravity;               // 重力方向ベクトル
        Vector3 gyroRotationRate = Input.gyro.rotationRate;     // 回転速度 (rad/s)
        Vector3 gyroAttitudeEuler = Input.gyro.attitude.eulerAngles; // オイラー角 (0~360)

        // --- 4. Acceleration (加速度 / 重力ベクトル) ---
        Vector3 acc = Input.acceleration; // (X: 横, Y: 縦, Z: 前後)

        // --- 5. 画面解像度とアスペクト比 ---
        int width = Screen.width;
        int height = Screen.height;
        float aspect = (float)width / height;

        // --- 画面表示テキスト構築 ---
        displayText.text = 
            $"<b>【 画面・デバイス状態 】</b>\n" +
            $"Screen.orientation : {screenOrient}\n" +
            $"DeviceOrientation  : {deviceOrient}\n" +
            $"Screen Size        : {width} x {height} (Aspect: {aspect:F2})\n\n" +

            $"<b>【 WebGL (GyroReceiver) 】</b>\n" +
            $"Raw Gamma (左右)   : {rawGamma:F1}°\n" +
            $"Raw Beta  (前後)   : {rawBeta:F1}°\n" +
            $"Raw Alpha (方角)   : {rawAlpha:F1}°\n" +
            $"Current Gamma      : {currentGamma:F1}°\n\n" +

            $"<b>【 重力・加速度 (Input.acceleration) 】</b>\n" +
            $"Acc X (端末横方向) : {acc.x:F2}\n" +
            $"Acc Y (端末縦方向) : {acc.y:F2}\n" +
            $"Acc Z (端末画面垂直): {acc.z:F2}\n\n" +

            $"<b>【 Unity Gyro (Input.gyro) 】</b>\n" +
            $"Attitude Euler     : ({gyroAttitudeEuler.x:F0}°, {gyroAttitudeEuler.y:F0}°, {gyroAttitudeEuler.z:F0}°)\n" +
            $"Gravity Vector     : ({gyroGravity.x:F2}, {gyroGravity.y:F2}, {gyroGravity.z:F2})\n" +
            $"Rotation Rate      : ({gyroRotationRate.x:F1}, {gyroRotationRate.y:F1}, {gyroRotationRate.z:F1})";
    }
}
