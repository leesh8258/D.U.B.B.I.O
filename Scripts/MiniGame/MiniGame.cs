using UnityEngine;

public abstract class MiniGame : MonoBehaviour, IPanelContent
{
    private const float CLEAR_PANEL_DURATION_SECONDS = 1f;

    public ContentType contentType { get; set; }
    public int clearCount { get; protected set; }

    protected DisconnectUI disconnectUI;
    protected ClearUI clearUI;

    protected GameLevel stageLevel = GameLevel.Easy;
    protected int stageDay = 0;

    public int difficulty = -1;

    private void Awake()
    {
        disconnectUI = GetComponent<DisconnectUI>();
        clearUI = GetComponent<ClearUI>();
    }

    protected virtual void Start()
    {
        if (clearUI != null)
        {
            clearUI.OnClearPanelFinished += disconnectUI.OnDisconnect;
        }
    }

    protected virtual void OnDestroy()
    {
        if (clearUI != null)
        {
            clearUI.OnClearPanelFinished -= disconnectUI.OnDisconnect;
        }
    }

    protected virtual void SetupGame()
    {
        clearCount = 0;

        if (clearUI != null)
        {
            clearUI.Initialize();
        }
    }

    protected void Clear()
    {
        clearCount++;
        MiniGameManager.Instance.ReportClear(this);

        if (clearUI != null)
        {
            clearUI.ShowClearPanel(CLEAR_PANEL_DURATION_SECONDS, difficulty);
        }
    }

    protected abstract void PrepareGame();

    protected abstract void CleanupGame();

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
        if (disconnectUI != null)
        {
            disconnectUI.PausePresentation();
        }

        if (clearUI != null)
        {
            clearUI.PausePresentation();
        }
    }

    public virtual void ResumeGame()
    {
        if (disconnectUI != null)
        {
            disconnectUI.ResumePresentation();
        }

        if (clearUI != null)
        {
            clearUI.ResumePresentation();
        }
    }

    public void OnMounted()
    {
        SetupGame();
    }

    public void OnShown()
    {
        PrepareGame();
    }

    public void OnUnmount()
    {
        CleanupGame();
    }
}
