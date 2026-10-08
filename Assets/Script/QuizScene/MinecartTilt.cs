using UnityEngine;

public class MinecartTilt : MonoBehaviour
{
    [Header("Tilt Settings")]
    [Tooltip("最大傾き角度")]
    public float maxTiltAngle = 15f; 

    [Tooltip("傾き始めのスピード（ふわっと動く）")]
    public float tiltSmoothSpeed = 5f;

    [Tooltip("水平に戻る時の基本スピード")]
    public float returnSpeed = 12f;

    [Header("Pivot Positions (相対位置)")]
    [Tooltip("トロッコの中心から見た『左車輪』の相対位置(X)")]
    public float leftWheelOffsetX = -100f;
    [Tooltip("トロッコの中心から見た『右車輪』の相対位置(X)")]
    public float rightWheelOffsetX = 100f;
    [Tooltip("車輪の高さ位置(Y)")]
    public float wheelOffsetY = -50f;

    [Header("Rumble / Vibration Settings (ガタゴト揺れ設定)")]
    [Tooltip("揺れの強さ（上下の高さ移動量）")]
    public float vibrationAmountY = 3f;

    [Tooltip("揺れの強さ（微小な回転揺れ）")]
    public float vibrationAmountAngle = 1f;

    [Tooltip("ガタゴト動くスピード（高いほど小刻み）")]
    public float vibrationSpeed = 20f;

    private float currentTargetAngle = 0f;
    private float currentAngle = 0f;
    private RectTransform rectTransform;

    // イージング制御用
    private float returnProgress = 0f;
    private float startAngleForReturn = 0f;

    // ピボット切替管理用
    private enum PivotState { Center, LeftWheel, RightWheel }
    private PivotState currentPivotState = PivotState.Center;

    // 揺れ計算用
    private Vector3 baseLocalPosition;
    private bool isBasePositionSaved = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Start()
    {
        SaveBasePosition();
    }

    void SaveBasePosition()
    {
        if (rectTransform != null)
        {
            baseLocalPosition = rectTransform.localPosition;
            isBasePositionSaved = true;
        }
    }

    void Update()
    {
        // --- 1. 傾き角度の計算 ---
        if (currentTargetAngle != 0f)
        {
            // 【傾ける時】通常の滑らかな補間（Lerp）
            currentAngle = Mathf.Lerp(currentAngle, currentTargetAngle, Time.deltaTime * tiltSmoothSpeed);
            
            // 戻り用プログレスのリセットと現在角度の記録
            returnProgress = 0f;
            startAngleForReturn = currentAngle;
        }
        else
        {
            // 【戻る時】出だしは遅く、着地直前に一気に加速する（Ease-In Quadratic / Cubic）
            if (Mathf.Abs(currentAngle) > 0.01f)
            {
                returnProgress += Time.deltaTime * returnSpeed;
                float t = Mathf.Clamp01(returnProgress);

                // t * t * t (Ease-In Cubic): 出だし超低速 ➔ 終盤に爆発的加速
                float easeInFactor = t * t * t; 

                // 記録した開始角度から0度（水平）へイージング補間
                currentAngle = Mathf.Lerp(startAngleForReturn, 0f, easeInFactor);
            }
            else
            {
                currentAngle = 0f;
                returnProgress = 0f;
            }
        }

        // --- 2. 軸（Pivot）切り替え制御 ---
        if (currentAngle > 0.01f) // 左へ傾く（左車輪が軸）
        {
            if (currentPivotState != PivotState.LeftWheel)
            {
                SetPivotSmart(new Vector2(leftWheelOffsetX, wheelOffsetY));
                currentPivotState = PivotState.LeftWheel;
            }
        }
        else if (currentAngle < -0.01f) // 右へ傾く（右車輪が軸）
        {
            if (currentPivotState != PivotState.RightWheel)
            {
                SetPivotSmart(new Vector2(rightWheelOffsetX, wheelOffsetY));
                currentPivotState = PivotState.RightWheel;
            }
        }

        // --- 3. ガタゴト揺れの計算 (PerlinNoise) ---
        float noiseY = (Mathf.PerlinNoise(Time.time * vibrationSpeed, 0f) - 0.5f) * 2f * vibrationAmountY;
        float noiseAngle = (Mathf.PerlinNoise(0f, Time.time * vibrationSpeed) - 0.5f) * 2f * vibrationAmountAngle;

        // --- 4. 回転の適用 ---
        float finalAngle = currentAngle + noiseAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, finalAngle);

        // --- 5. 上下位置の適用 ---
        if (isBasePositionSaved)
        {
            rectTransform.localPosition = new Vector3(baseLocalPosition.x, baseLocalPosition.y + noiseY, baseLocalPosition.z);
        }
    }

    void SetPivotSmart(Vector2 localPivotPos)
    {
        Vector2 pivotPoint = new Vector2(
            0.5f + (localPivotPos.x / rectTransform.rect.width),
            0.5f + (localPivotPos.y / rectTransform.rect.height)
        );

        Vector2 size = rectTransform.rect.size;
        Vector2 deltaPivot = rectTransform.pivot - pivotPoint;
        
        Vector3 deltaPosition = transform.localRotation * new Vector3(deltaPivot.x * size.x, deltaPivot.y * size.y, 0f);
        
        rectTransform.pivot = pivotPoint;
        rectTransform.localPosition -= deltaPosition;

        baseLocalPosition = rectTransform.localPosition;
    }

    public void SetTiltInput(float inputValue, float inputThreshold)
    {
        if (inputValue < -inputThreshold)
        {
            currentTargetAngle = maxTiltAngle;
        }
        else if (inputValue > inputThreshold)
        {
            currentTargetAngle = -maxTiltAngle;
        }
        else
        {
            currentTargetAngle = 0f;
        }
    }
}
