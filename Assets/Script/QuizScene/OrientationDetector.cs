using UnityEngine;

public class OrientationDetector : MonoBehaviour
{
    // 現在の横持ちの向きを取得する関数
    public string GetHorizontalOrientation()
    {
        // Y軸の加速度を取得
        float accelY = Input.acceleration.y;

        // 加速度のしきい値（誤判定を防ぐため少し余裕を持たせる）
        if (accelY < -0.2f)
        {
            return "LandscapeLeft (通常横持ち)";
        }
        else if (accelY > 0.2f)
        {
            return "LandscapeRight (逆さ横持ち)";
        }
        else
        {
            return "水平または縦持ち";
        }
    }

    void Update()
    {
        // テスト表示用
        // Debug.Log(GetHorizontalOrientation());
    }
}
