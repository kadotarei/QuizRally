using UnityEngine;

public class CouponRedirect : MonoBehaviour
{
    // LINEのクーポンURL（管理画面で発行されたURL）
    private string lineCouponUrl = "https://lin.ee/PoXjQRJ"; 

    // ボタンが押された時に実行するメソッド
    public void OpenLineCoupon()
    {
        // 指定したURLをブラウザの別タブで開く
        Application.OpenURL(lineCouponUrl);
    }
}
