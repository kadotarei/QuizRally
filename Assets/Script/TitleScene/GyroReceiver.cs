using System.Runtime.InteropServices;
using UnityEngine;

public class GyroReceiver : MonoBehaviour
{
    [DllImport("__Internal")]
    private static extern void InitGyroAndRequestPermission(string objectName, string callbackName);

    public float CurrentGamma { get; private set; }
    public float RawBeta { get; private set; }  // ★ 追加：生のBeta値
    public float RawGamma { get; private set; } // ★ 追加：生のGamma値

    private void Awake()
    {
        // シーンを跨いでもこのジャイロ受信オブジェクトが破壊されないように設定
        DontDestroyOnLoad(gameObject);
    }

    public void EnableGyroPermission()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        InitGyroAndRequestPermission(gameObject.name, "OnReceiveGyroData");
#endif
    }

    public void OnReceiveGyroData(string dataStr)
    {
        string[] values = dataStr.Split(',');
        if (values.Length >= 2)
        {
            if (float.TryParse(values[0], out float beta) && float.TryParse(values[1], out float gamma))
            {
                RawBeta = beta;   // ★ 生のBetaを保存
                RawGamma = gamma; // ★ 生のGammaを保存

                if (Screen.width > Screen.height)
                {
                    if (Input.deviceOrientation == DeviceOrientation.LandscapeRight)
                    {
                        CurrentGamma = -beta;
                    }
                    else
                    {
                        CurrentGamma = beta;
                    }
                }
                else
                {
                    CurrentGamma = gamma;
                }
            }
        }
    }
}
