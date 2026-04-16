using UnityEngine;

public abstract class MiniGame : MonoBehaviour, IPanelContent
{
    private const float CLEAR_PANEL_DURATION_SECONDS = 1f;

    [SerializeField] protected int difficulty = -1;

    protected DisconnectUI disconnectUI;
    protected ClearUI clearUI;
    protected GameLevel stageLevel = GameLevel.Easy;
    protected int stageDay = 0;

    public ContentType contentType { get; set; }
    public bool HasMountedOnce { get; private set; }
    public int Difficulty => difficulty;

    private MiniGameManager miniGameManager;

    private void Awake()
    {
        disconnectUI = GetComponent<DisconnectUI>();
        clearUI = GetComponent<ClearUI>();
    }

    protected virtual void Start()
    {
        clearUI.OnClearPanelFinished += disconnectUI.OnDisconnect;
    }

    protected virtual void OnDestroy()
    {
        clearUI.OnClearPanelFinished -= disconnectUI.OnDisconnect;
    }

    protected virtual void SetupGame()
    {
        clearUI.Initialize();
    }

    protected void Clear()
    {
        miniGameManager.IncreaseClueGauge(this);
        clearUI.ShowClearPanel(CLEAR_PANEL_DURATION_SECONDS, difficulty);
    }

    protected abstract void PrepareGame();

    protected abstract void CleanupGame();

    public void SetManager(MiniGameManager manager)
    {
        miniGameManager = manager;
    }

    public virtual void ConfigureForStage(GameLevel level, int day)
    {
        stageLevel = level;
        stageDay = day;
    }

    public virtual void Restore()
    {
        PrepareGame();
    }

    public virtual void PauseGame()
    {
        disconnectUI.PausePresentation();
        clearUI.PausePresentation();
    }

    public virtual void ResumeGame()
    {
        disconnectUI.ResumePresentation();
        clearUI.ResumePresentation();
    }

    public void OnMounted()
    {
        HasMountedOnce = true;
        SetupGame();
    }

    public void OnShown()
    {
        PrepareGame();
    }

    public void OnUnmount()
    {
        CleanupGame();
        disconnectUI.ResetPresentation();
        clearUI.Initialize();
    }

    public void HandleDisconnectCompleted()
    {
        miniGameManager.HandleClearOutcome(this);
    }
}
