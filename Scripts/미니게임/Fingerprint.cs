using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Fingerprint : MiniGame
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI suspectTitleText;
    [SerializeField] private TextMeshProUGUI evidenceTitleText;
    [SerializeField] private TextMeshProUGUI nextButtonText;
    [SerializeField] private TextMeshProUGUI submitButtonText;
    [SerializeField] private TextMeshProUGUI failTitleText;

    [Header("Buttons")]
    [SerializeField] private Button evidenceClickButton;
    [SerializeField] private Button submitButton;

    [Header("Image")]
    [SerializeField] private Image suspectFingerprintImage;
    [SerializeField] private Image evidenceFingerprintImage;

    [Header("Effect Object")]
    [SerializeField] private GameObject suspectEffectObject;
    [SerializeField] private GameObject evidenceEffectObject;

    [Header("Effect RectTransforms")]
    [SerializeField] private RectTransform suspectEffectStartRect;
    [SerializeField] private RectTransform evidenceEffectStartRect;
    [SerializeField] private RectTransform suspectImageEndRect;
    [SerializeField] private RectTransform evidenceImageEndRect;
    [SerializeField] private RectTransform leftFrameRect;
    [SerializeField] private RectTransform rightFrameRect;

    [Header("Fail Effect")]
    [SerializeField] private GameObject failPanel;
    [SerializeField] private float failEffectBlinkInterval = 0.5f;
    [SerializeField] private float failEffectDuration = 3.0f;

    [Header("Sprites")]
    [SerializeField] private Sprite[] fingerprintSprites;

    [Header("Decoration")]
    [SerializeField] private Color fingerprintSubtractiveColor;
    [SerializeField] private float fingerprintSwapLerpMultiplier = 20f;
    [SerializeField] private Color fingerprintBaseColor;
    [SerializeField] private Vector3 comparisonMinScale;

    private int evidenceIndex = 0;
    private int suspectIndex = 0;

    private bool isComparing = false;
    private Coroutine compareRoutine = null;
    private Coroutine swapRoutine = null;

    private RectTransform suspectEffectRect;
    private RectTransform evidenceEffectRect;

    private const int excludeRange = 2;
    private const float scanDurationSeconds = 1f;

    private const string tableName = "MiniGameUI";
    private const string descriptionKey = "Fingerprint_Description";
    private const string suspectTitleKey = "Fingerprint_SuspectTitle";
    private const string evidenceTitleKey = "Fingerprint_EvidenceTitle";
    private const string nextButtonKey = "Fingerprint_NextButton";
    private const string submitButtonKey = "Fingerprint_SubmitButton";
    private const string failTitleKey = "Fingerprint_FailTitle";

    #region MiniGame Override
    protected override void Start()
    {
        base.Start();

        suspectEffectRect = suspectEffectObject.GetComponent<RectTransform>();
        evidenceEffectRect = evidenceEffectObject.GetComponent<RectTransform>();

        evidenceClickButton.onClick.RemoveListener(SetImage);
        evidenceClickButton.onClick.AddListener(SetImage);

        submitButton.onClick.RemoveListener(Submit);
        submitButton.onClick.AddListener(Submit);

        // 로직이 멈춰도 이펙트가 멈추면 안되니, 상시 폴링
        StartCoroutine(DecorationEffectRoutine());
    }

    protected override void PrepareGame()
    {
        InitializeText();
        InitializeImage();
        InitializeVariable();
    }

    protected override void CleanupGame()
    {
        InitializeVariable();
    }

    #endregion

    #region Initialize
    private void InitializeText()
    {
        descriptionText.text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, descriptionKey);
        suspectTitleText.text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, suspectTitleKey);
        evidenceTitleText.text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, evidenceTitleKey);
        nextButtonText.text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, nextButtonKey);
        submitButtonText.text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, submitButtonKey);
        failTitleText.text = LocalizationSettings.StringDatabase.GetLocalizedString(tableName, failTitleKey);
    }

    private void InitializeVariable()
    {
        SetInteractable(true);
        isComparing = false;

        if (compareRoutine != null)
        {
            StopCoroutine(compareRoutine);
        }
        compareRoutine = null;

        if (swapRoutine != null)
        {
            StopCoroutine(swapRoutine);
        }
        swapRoutine = null;
    }

    private void InitializeImage()
    {
        int count = fingerprintSprites.Length;

        suspectIndex = Random.Range(0, count);
        evidenceIndex = PickEvidenceIndex(suspectIndex, count);

        ApplySuspectSprite();
        ApplyEvidenceSprite();
    }

    #endregion

    #region Loop
    private IEnumerator DecorationEffectRoutine()
    {
        Color baselineSubtractiveColor = new Color(0f, 0f, 0f, 0f);
        Vector3 scaleBuffer = Vector3.one;
        Vector3 targetScaleBuffer = Vector3.one;

        while (true)
        {
            fingerprintSubtractiveColor = Color.Lerp(fingerprintSubtractiveColor, baselineSubtractiveColor, Time.deltaTime * fingerprintSwapLerpMultiplier);
            evidenceFingerprintImage.color = fingerprintBaseColor - fingerprintSubtractiveColor;

            targetScaleBuffer = (isComparing) ? comparisonMinScale : Vector3.one;
            scaleBuffer = Vector3.Lerp(scaleBuffer, targetScaleBuffer, Time.deltaTime * fingerprintSwapLerpMultiplier);

            leftFrameRect.localScale = scaleBuffer;
            rightFrameRect.localScale = scaleBuffer;

            yield return null;
        }
    }

    private void SetImage()
    {
        if (isComparing) return;

        evidenceIndex++;
        if (evidenceIndex >= fingerprintSprites.Length)
        {
            evidenceIndex = 0;
        }

        fingerprintSubtractiveColor = Color.black;

        ApplyEvidenceSprite();
    }

    private int PickEvidenceIndex(int suspect, int count)
    {
        List<int> candidates = new List<int>(count);

        int min = suspect - excludeRange;
        int max = suspect + excludeRange;

        for (int i = 0; i < count; i++)
        {
            if (i < min || i > max)
            {
                candidates.Add(i);
            }
        }

        int pick = Random.Range(0, candidates.Count);
        return candidates[pick];
    }

    private void Submit()
    {
        if (isComparing || compareRoutine != null) return;

        compareRoutine = StartCoroutine(CompareRoutine());
    }

    private IEnumerator CompareRoutine()
    {
        isComparing = true;
        SetInteractable(false);

        yield return PlaySubmitEffect();

        bool isMatch = (evidenceIndex == suspectIndex);

        if (isMatch)
        {
            Clear();
        }

        else
        {
            yield return PlayFailEffect();
            InitializeVariable();
        }

    }

    private void SetInteractable(bool interactable)
    {
        evidenceClickButton.interactable = interactable;
        submitButton.interactable = interactable;
    }

    private void ApplySuspectSprite()
    {
        Sprite sprite = fingerprintSprites[suspectIndex];
        suspectFingerprintImage.sprite = sprite;
    }

    private void ApplyEvidenceSprite()
    {
        Sprite sprite = fingerprintSprites[evidenceIndex];
        evidenceFingerprintImage.sprite = sprite;
    }

    #endregion

    #region Effect
    private IEnumerator PlaySubmitEffect()
    {
        Vector2 suspectStart = suspectEffectStartRect.anchoredPosition;
        Vector2 suspectEnd = suspectImageEndRect.anchoredPosition;

        Vector2 evidenceStart = evidenceEffectStartRect.anchoredPosition;
        Vector2 evidenceEnd = evidenceImageEndRect.anchoredPosition;

        suspectEffectRect.anchoredPosition = suspectStart;
        evidenceEffectRect.anchoredPosition = evidenceStart;

        RevealEffect();

        float elapsed = 0.0f;

        while (elapsed < scanDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / scanDurationSeconds);

            suspectEffectRect.anchoredPosition = Vector2.Lerp(suspectStart, suspectEnd, t);
            evidenceEffectRect.anchoredPosition = Vector2.Lerp(evidenceStart, evidenceEnd, t);

            yield return null;
        }

        HideEffects();
    }

    private IEnumerator PlayFailEffect()
    {
        float elapsed = 0.0f;

        while(elapsed < failEffectDuration)
        {
            failPanel.SetActive(true);
            yield return new WaitForSeconds(failEffectBlinkInterval);
            failPanel.SetActive(false);
            yield return new WaitForSeconds(failEffectBlinkInterval);
            elapsed += failEffectBlinkInterval * 2;
        }

    }

    private void RevealEffect()
    {
        suspectEffectObject.SetActive(true);
        evidenceEffectObject.SetActive(true);
    }

    private void HideEffects()
    {
        suspectEffectObject.SetActive(false);
        evidenceEffectObject.SetActive(false);
    }

    #endregion
}
