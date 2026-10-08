using UnityEngine;

public class OrientationChecker : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("縦向き（または画面が横持ち以外になっている）時に表示する警告パネル")]
    public GameObject rotateNoticePanel;

    void Update()
    {
        CheckOrientation();
    }

    void CheckOrientation()
    {
        if (rotateNoticePanel == null) return;

        bool showNotice = false;

#if UNITY_WEBGL && !UNITY_EDITOR
        ScreenOrientation screenOrient = Screen.orientation;
        DeviceOrientation deviceOrient = Input.deviceOrientation;

        // 1. Screen.orientation が横持ちではない場合
        bool isScreenNotLandscape = (screenOrient != ScreenOrientation.LandscapeLeft && 
                                     screenOrient != ScreenOrientation.LandscapeRight);

        // 2. Input.deviceOrientation が縦持ち（正方向・上下逆さま）の場合
        bool isDevicePortrait = (deviceOrient == DeviceOrientation.Portrait || 
                                 deviceOrient == DeviceOrientation.PortraitUpsideDown);

        // どちらか一方でも縦持ちを検知したら警告を表示
        if (isScreenNotLandscape || isDevicePortrait)
        {
            showNotice = true;
        }
#else
        // エディタ上や非WebGL環境では Game ビューの縦横比で判定 (高さ > 幅 の場合は縦持ちとみなす)
        if (Screen.height > Screen.width)
        {
            showNotice = true;
        }
#endif

        // パネルの状態に変更がある場合のみ SetActive を呼び出す
        if (rotateNoticePanel.activeSelf != showNotice)
        {
            rotateNoticePanel.SetActive(showNotice);
        }
    }
}
