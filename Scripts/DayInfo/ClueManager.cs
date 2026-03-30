using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class ClueManager : MonoBehaviour
{
    private const string SystemMessageAllCluesEmittedKey = "MSG_ALL_CLUES_EMITTED";
    private const string SystemMessageTableDescriptionKey = "MSG_TABLE_DESCRIPTION";

    [Header("Board UI")]
    [SerializeField] private BoardUI boardUI;

    [Header("Input")]
    [SerializeField] private InputController inputController;

    [Header("Clue")]
    [SerializeField] private ClueGenerator clueGenerator;
    [SerializeField] private ClueLogView clueLogView;
    [SerializeField] private ClueLogStore clueLogStore;
    [SerializeField] private ClueLanguageManager clueLanguageManager;

    [Header("SO Data")]
    [SerializeField] private RoundClueDataIndexSO roundClueDataIndex;

    private Category activeCategory;
    private int requestCount = 0;

    private SuspectItem[] curSuspects = Array.Empty<SuspectItem>();
    private Keyword[] curKeywords = Array.Empty<Keyword>();

    private readonly List<Category> presentCategories = new List<Category>();

    private KeywordIndexMap keywordIndexMap;
    private BoardData boardData;

    #region public API
    public void Initialize(Category[] cats)
    {
        ResetAllCacheAndUI();
        ApplyCategories(cats);
        SetupClueGenerationForCurrentDay();
    }

    public void IncreaseRequestCount()
    {
        SetRequestCount(requestCount + 1);
    }

    public void RequestClue()
    {
        if (requestCount <= 0 || clueGenerator == null) return;

        GenerateStatus status = clueGenerator.Generate(out Clue clue);
        if (status == GenerateStatus.Success)
        {
            clue.text = clueLanguageManager.FormatClue(clue);
            clueLogStore.AddClue(clue);
            clueLogView.Append(clue);

            SetRequestCount(requestCount - 1);
        }

        else if (status == GenerateStatus.Exhausted)
        {
            AppendSystemNotice(SystemMessageAllCluesEmittedKey);
            SetRequestCount(0);
        }

    }

    public void ResetActiveBoardState()
    {
        if (boardUI != null)
        {
            boardData.ResetCategory(activeCategory);
            boardUI.ResetAllCellVisuals();
            boardUI.ResetAllKeywordViewsVisuals();
            RefreshInvokers();

            CellState[,] states = boardData.GetBoard(activeCategory);
            boardUI.ApplyBoardStates(states);
        }
    }
    #endregion

    #region private API
    private void Awake()
    {
        if (boardUI == null || inputController == null || clueGenerator == null || clueLogView == null || clueLogStore == null || clueLanguageManager == null || roundClueDataIndex == null)
        {
            Debug.LogWarning("필수 참조 부족");
            return;
        }

        boardData = new BoardData(boardUI.Rows, boardUI.Cols);
        keywordIndexMap = new KeywordIndexMap(boardUI.Rows);

        boardUI.SetGetClueButtonListener(RequestClue);

        inputController.RequestedClueEvent += RequestClue;
        inputController.ToggleDocumentEvent += ToggleCategory;
    }

    private void OnDestroy()
    {
        inputController.RequestedClueEvent -= RequestClue;
        inputController.ToggleDocumentEvent -= ToggleCategory;
    }

    private void ResetAllCacheAndUI()
    {
        boardData.ClearAll();
        keywordIndexMap.Clear();

        boardUI.Initialize();
        clueLogView.Initialize();
        clueLogStore.Initialize();

        SetRequestCount(0);
    }

    private void ApplyCategories(Category[] categories)
    {
        if (categories == null || categories.Length == 0) return;

        presentCategories.Clear();
        presentCategories.AddRange(categories);

        activeCategory = presentCategories[0];
        SetupBoardForCategory(activeCategory);
    }

    private ISyntaxRenderer CreateSyntaxRenderer()
    {
        Locale locale = LocalizationSettings.SelectedLocale;
        string code = locale != null ? locale.Identifier.Code : "ko";

        if (code.StartsWith("ko"))
        {
            return new SyntaxRendererKo();
        }

        return new SyntaxRendererEn();
    }

    private void SetupClueGenerationForCurrentDay()
    {
        if (CaseManager.Instance == null) return;

        if (!RoundClueDataBuilder.BuildRoundClueData(CaseManager.Instance, out RoundClueData roundClueData, out CompareClue compareClue)) return;
        clueLanguageManager.SetSyntaxRenderer(CreateSyntaxRenderer());

        StageInfo stage = StageManager.CurrentStage;

        if (!roundClueDataIndex.TryGet(stage, out RoundClueDataSO roundClueDataSO) || roundClueDataSO == null) return;

        clueGenerator.Initialize(roundClueData, compareClue, roundClueDataSO);
    }

    public void ToggleCategory()
    {
        if (presentCategories.Count == 0) return;

        int categoryIndex = presentCategories.IndexOf(activeCategory);
        if (categoryIndex < 0) categoryIndex = 0;

        int next = (categoryIndex + 1) % presentCategories.Count;
        SetupBoardForCategory(presentCategories[next]);
    }

    private void SetupBoardForCategory(Category category)
    {
        activeCategory = category;
        if (!boardUI.ApplyCategoryLabels(clueLanguageManager, activeCategory, SystemMessageTableDescriptionKey)) return;

        SetActiveCategoryData(activeCategory);
        keywordIndexMap.Rebuild(curSuspects, curKeywords);

        boardUI.SetHeaders(clueLanguageManager, curSuspects, curKeywords);
        boardUI.ResetAllKeywordViewsVisuals();
        boardUI.ResetAllCellVisuals();

        Dictionary<Keyword, CellState> keywordMap = boardData.GetKeywordMap(activeCategory);
        boardUI.ApplyKeywordLineStates(keywordMap, curKeywords);

        CellState[,] states = boardData.GetBoard(activeCategory);
        boardUI.ApplyBoardStates(states);

        RefreshInvokers();
    }

    private void SetActiveCategoryData(Category category)
    {
        if (CaseManager.Instance == null)
        {
            curSuspects = Array.Empty<SuspectItem>();
            curKeywords = Array.Empty<Keyword>();
            return;
        }

        SuspectItem[] suspectItems = CaseManager.Instance.GetSuspects(category);
        Keyword[] keywords = CaseManager.Instance.GetKeywords(category);

        curSuspects = suspectItems;
        curKeywords = keywords;
    }

    private void OnKeywordClicked(Keyword keyword, int dir)
    {
        if (keyword == null) return;
        if (!keywordIndexMap.TryGetColumn(keyword, out int col)) return;

        CellState oldState = boardData.GetKeywordState(activeCategory, keyword);
        CellState newState = NextState(oldState, dir);

        boardData.SetKeywordState(activeCategory, keyword, newState);

        boardUI.ApplyKeywordVisual(col, newState);

        boardData.ClearColumn(activeCategory, col);

        if (keywordIndexMap.TryGetRows(keyword, out List<int> rowList) && rowList != null)
        {
            int rowCount = boardUI.Rows;

            for (int i = 0; i < rowList.Count; i++)
            {
                int row = rowList[i];
                if (row >= 0 && row < rowCount)
                {
                    boardData.SetCell(activeCategory, row, col, newState);
                }
            }
        }

        if (boardUI != null)
        {
            boardUI.ApplyBoardStates(boardData.GetBoard(activeCategory));
        }
    }

    private CellState NextState(CellState current, int dir)
    {
        bool direction = dir >= 0;

        switch (current)
        {
            case CellState.None:
                return direction ? CellState.Match : CellState.Unknown;

            case CellState.Match:
                return direction ? CellState.Mismatch : CellState.None;

            case CellState.Mismatch:
                return direction ? CellState.Unknown : CellState.Match;

            case CellState.Unknown:
                return direction ? CellState.None : CellState.Mismatch;

            default:
                return CellState.None;
        }
    }

    private void UpdateGetClueButtonInteractivity()
    {
        bool ok = requestCount > 0 && !clueGenerator.IsExhausted;
        boardUI.SetGetClueButtonInteractable(ok);
    }

    private void AppendSystemNotice(string systemMessageKey)
    {
        string message = clueLanguageManager.GetSystemMessage(systemMessageKey);
        AppendNotice(message);
    }

    private void AppendNotice(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        string text = clueLanguageManager.ApplySyntax(message);

        Clue notice = new Clue
        {
            type = ClueType.AttrT,
            text = text,
            tokens = Array.Empty<string>()
        };

        clueLogView.Append(notice);
    }

    private void RefreshInvokers()
    {
        boardUI.ClearAllInvokers();
        boardUI.WireInvokers(clueLanguageManager, curKeywords, OnKeywordClicked);
    }

    private void SetRequestCount(int value)
    {
        requestCount = Mathf.Max(0, value);
        boardUI.SetRequestCount(requestCount);
        UpdateGetClueButtonInteractivity();
    }
    #endregion
}
