using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class PINInput : MiniGame
{
    [Header("모드 컨테이너")]
    [SerializeField] private GameObject modeAContainer;
    [SerializeField] private GameObject modeBContainer;

    [Header("Mode_A TMP")]
    [SerializeField] private TextMeshProUGUI targetTMP;
    [SerializeField] private TextMeshProUGUI inputTMP_A;

    [Header("Target TMP 난수 설정")]
    [SerializeField, Range(0f, 1f)] private float randomizeChance = 0.75f;
    [SerializeField] private float rotateZ = 40f;
    [SerializeField] private float offsetX = 85f;
    [SerializeField] private float normalFontSize = 70f;
    [SerializeField] private float randomizedFontSize = 100f;

    [Header("Mode_B TMP")]
    [SerializeField] private TextMeshProUGUI[] slotCells_B = new TextMeshProUGUI[6];
    [SerializeField] private TextMeshProUGUI modeBTimerTMP;

    [Header("Mode_B Timer")]
    [SerializeField] private float modeBTimeLimitSeconds = 10f;

    [Header("난이도 설정")]
    [SerializeField, Range(4, 6)] private int pinLength = 4;
    [SerializeField, Range(0f, 1f)] private float reentryChance = 0.3f;

    private enum PinMode
    {
        A, B
    }

    private const char MaskChar = '*';
    private const string tableName = "MiniGameUI";
    private const string startTextKey = "PINInput_StartText";
    private const string modeBTimerTextKey = "PINInput_ModeBTimerText";

    private PinMode mode;
    private string targetPin = string.Empty;
    private string inputA = string.Empty;
    private string inputB = string.Empty;

    private RectTransform targetRectTransform;
    private Vector2 targetAnchoredPosition;
    private Vector3 targetLocalEulerAngles;
    private float targetFontSize;

    private Coroutine modeBTimerCoroutine;
    private bool isPaused;
    private bool isModeBTimeExpired;

    private string startText = string.Empty;
    private string modeBTimerFormat = string.Empty;

    #region MiniGame Override
    protected override void Start()
    {
        base.Start();
        CacheTargetVisual();
        InitializeLocalizedText();
        RefreshModeBTimerDisplay(0);
    }

    public override void ConfigureForStage(GameLevel level, int day)
    {
        base.ConfigureForStage(level, day);

        switch (level)
        {
            case GameLevel.Easy:
                pinLength = 4;
                reentryChance = 0.2f;
                randomizeChance = 0.85f;
                break;

            case GameLevel.Hard:
                pinLength = 5;
                reentryChance = 0.4f;
                randomizeChance = 0.7f;
                break;
        }
    }

    protected override void PrepareGame()
    {
        StopModeBTimer();
        SetupSlotsForLength_B(pinLength);
        ResetInputs();
        RefreshInputDisplays();
        ApplyNewTargetPin();
        ApplyTargetVisualStyle();
        ResetToModeA();
    }

    protected override void CleanupGame()
    {
        StopModeBTimer();
        ResetInputs();
        RefreshInputDisplays();
        ResetToModeA();
        ResetTargetVisual();
    }

    public override void PauseGame()
    {
        base.PauseGame();
        isPaused = true;
    }

    public override void ResumeGame()
    {
        base.ResumeGame();
        isPaused = false;
    }

    #endregion

    #region Initialize
    private void InitializeLocalizedText()
    {
        startText = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, startTextKey);
        modeBTimerFormat = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, modeBTimerTextKey);
    }

    private void CacheTargetVisual()
    {
        if (targetTMP == null) return;

        targetRectTransform = targetTMP.rectTransform;
        targetAnchoredPosition = targetRectTransform.anchoredPosition;
        targetLocalEulerAngles = targetRectTransform.localEulerAngles;
        targetFontSize = targetTMP.fontSize;
    }

    private void ResetToModeA()
    {
        SwitchMode(PinMode.A);
    }

    private void SwitchMode(PinMode next)
    {
        mode = next;

        modeAContainer.SetActive(mode == PinMode.A);
        modeBContainer.SetActive(mode == PinMode.B);

        if (mode == PinMode.A)
        {
            StopModeBTimer();
            RefreshDisplayA();
        }
        else
        {
            inputB = string.Empty;
            ClearSlotTexts_B();
            StartModeBTimer();
        }
    }

    private void StartModeBTimer()
    {
        StopModeBTimer();
        isModeBTimeExpired = false;
        modeBTimerCoroutine = StartCoroutine(ModeBTimerRoutine());
    }

    private void StopModeBTimer()
    {
        if (modeBTimerCoroutine != null)
        {
            StopCoroutine(modeBTimerCoroutine);
            modeBTimerCoroutine = null;
        }

        isModeBTimeExpired = false;
        RefreshModeBTimerDisplay(0);
    }

    private IEnumerator ModeBTimerRoutine()
    {
        float remaining = Mathf.Max(0f, modeBTimeLimitSeconds);
        int lastShownSeconds = -1;

        while (remaining > 0f)
        {
            if (isPaused)
            {
                yield return null;
                continue;
            }

            int displaySeconds = Mathf.CeilToInt(remaining);
            if (displaySeconds != lastShownSeconds)
            {
                RefreshModeBTimerDisplay(displaySeconds);
                lastShownSeconds = displaySeconds;
            }

            remaining -= Time.deltaTime;
            yield return null;
        }

        RefreshModeBTimerDisplay(0);
        isModeBTimeExpired = true;
        modeBTimerCoroutine = null;
        disconnectUI.OnDisconnect();
    }

    private void ResetTargetVisual()
    {
        if (targetTMP == null) return;
        if (targetRectTransform == null) return;

        targetTMP.fontSize = targetFontSize;
        targetRectTransform.anchoredPosition = targetAnchoredPosition;
        targetRectTransform.localEulerAngles = targetLocalEulerAngles;
    }

    private void RefreshModeBTimerDisplay(int seconds)
    {
        if (modeBTimerTMP == null) return;

        if (string.IsNullOrEmpty(modeBTimerFormat))
        {
            modeBTimerFormat = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, modeBTimerTextKey);
        }

        modeBTimerTMP.text = string.Format(modeBTimerFormat, seconds);
    }

    private void ApplyTargetVisualStyle()
    {
        ResetTargetVisual();

        bool useNormalStyle = Random.value < randomizeChance;

        if (useNormalStyle)
        {
            targetTMP.fontSize = normalFontSize;

            Vector3 euler = targetRectTransform.localEulerAngles;
            euler.z = 0f;
            targetRectTransform.localEulerAngles = euler;
            return;
        }

        targetTMP.fontSize = randomizedFontSize;

        float x = Random.value < 0.5f ? -offsetX : offsetX;
        Vector2 anchoredPosition = targetAnchoredPosition;
        anchoredPosition.x += x;
        targetRectTransform.anchoredPosition = anchoredPosition;

        Vector3 randomizedEuler = targetLocalEulerAngles;
        randomizedEuler.z += Random.Range(-rotateZ, rotateZ);
        targetRectTransform.localEulerAngles = randomizedEuler;
    }

    private string CreateRandomPin(int length)
    {
        StringBuilder sb = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            int digit = Random.Range(0, 10);
            sb.Append((char)('0' + digit));
        }

        return sb.ToString();
    }

    private void ApplyNewTargetPin()
    {
        targetPin = CreateRandomPin(pinLength);
        targetTMP.text = targetPin;
    }

    private void ResetInputs()
    {
        inputA = string.Empty;
        inputB = string.Empty;
    }

    private void RefreshInputDisplays()
    {
        RefreshDisplayA();
        ClearSlotTexts_B();
    }

    private bool IsCorrectPin(string input)
    {
        return input.Length == pinLength && input == targetPin;
    }

    private void FailModeA()
    {
        inputA = string.Empty;
        RefreshDisplayA();
        SoundManager.Instance.PlaySFX(SFXType.SFX_MiniGame_Fail);
    }

    private void FailModeB()
    {
        if (isModeBTimeExpired) return;

        inputB = string.Empty;
        ClearSlotTexts_B();
        SoundManager.Instance.PlaySFX(SFXType.SFX_MiniGame_Fail);
    }

    private void CompleteModeA()
    {
        if (Random.value < reentryChance)
        {
            SwitchMode(PinMode.B);
            return;
        }

        Clear();
    }

    private void CompleteModeB()
    {
        StopModeBTimer();
        Clear();
    }
    #endregion

    #region ModeA
    public void AddDigitA(int digit)
    {
        if (mode != PinMode.A) return;
        if (digit < 0 || digit > 9) return;
        if (inputA.Length >= pinLength) return;

        inputA += (char)('0' + digit);
        RefreshDisplayA();

        if (inputA.Length == pinLength)
        {
            ValidateA();
        }
    }

    public void BackspaceA()
    {
        if (mode != PinMode.A) return;
        if (inputA.Length == 0) return;

        inputA = inputA.Substring(0, inputA.Length - 1);
        RefreshDisplayA();
    }

    public void ClearA()
    {
        if (mode != PinMode.A) return;

        inputA = string.Empty;
        RefreshDisplayA();
    }

    private void RefreshDisplayA()
    {
        if (string.IsNullOrEmpty(inputA))
        {
            if (string.IsNullOrEmpty(startText))
            {
                startText = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, startTextKey);
            }

            inputTMP_A.text = startText;
        }

        else
        {
            inputTMP_A.text = inputA;
        }
    }

    private void ValidateA()
    {
        if (!IsCorrectPin(inputA))
        {
            FailModeA();
            return;
        }

        CompleteModeA();
    }
    #endregion

    #region ModeB
    private void SetupSlotsForLength_B(int length)
    {
        for (int i = 0; i < slotCells_B.Length; i++)
        {
            TextMeshProUGUI cell = slotCells_B[i];
            bool active = i < length;
            cell.gameObject.SetActive(active);
            cell.text = string.Empty;
        }
    }

    private void ClearSlotTexts_B()
    {
        for (int i = 0; i < slotCells_B.Length; i++)
        {
            TextMeshProUGUI cell = slotCells_B[i];

            if (cell.gameObject.activeSelf)
            {
                cell.text = string.Empty;
            }
        }
    }

    private void SetSlotMaskB(int index)
    {
        if (index < 0 || index >= slotCells_B.Length) return;

        TextMeshProUGUI cell = slotCells_B[index];
        if (!cell.gameObject.activeSelf) return;

        cell.text = MaskChar.ToString();
    }

    public void AddDigitB(int digit)
    {
        if (mode != PinMode.B) return;
        if (digit < 0 || digit > 9) return;
        if (inputB.Length >= pinLength) return;

        int slotIndex = inputB.Length;
        inputB += (char)('0' + digit);
        SetSlotMaskB(slotIndex);

        if (inputB.Length == pinLength)
        {
            ValidateB();
        }
    }

    private void ValidateB()
    {
        if (!IsCorrectPin(inputB))
        {
            FailModeB();
            return;
        }

        CompleteModeB();
    }
    #endregion
}
