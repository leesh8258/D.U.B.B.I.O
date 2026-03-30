using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance;

    public readonly string saveFileName = "AutoSave.json";
    private const int MAX_DAYS = 7;

    private string saveFilePath;
    private AESGenerator aes;
    private bool isBusy = false;

    private float playSessionStartRealtime;
    private bool playSessionStarted;

    public event Action<SaveData> OnWriteSaveData;
    public SaveData Current { get; private set; }

    [Serializable]
    private class SaveDataJson
    {
        public string saveFile;
        public string saveUTC;
    }

    [Serializable]
    public class SaveDataGame
    {
        public SaveData saveData;
        public string saveUTC;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;

        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        aes = new AESGenerator();
    }

    #region Save / Load 
    private async Task Save(SaveData saveData)
    {
        if (isBusy)
        {
            Debug.LogWarning("[GameSaveManager] Busy 상태");
            return;
        }
        
        if (saveData == null)
        {
            Debug.LogWarning("[GameSaveManager] 세이브데이터가 존재하지 않습니다.");
            return;
        }

        isBusy = true;

        try
        {
            AccumulatePlayTime(saveData);

            // false로 해야 일렬로 변환
            string saveDataToJson = JsonUtility.ToJson(saveData, false);
            string encryptFile = aes.Encrypt(saveDataToJson);
            string nowUTC = DateTime.UtcNow.ToString("o");

            SaveDataJson saveDataJson = new SaveDataJson
            {
                saveUTC = nowUTC,
                saveFile = encryptFile
            };

            string finalJson = JsonUtility.ToJson(saveDataJson, false);

            await Task.Run(() =>
            {
                string tempPath = saveFilePath + ".tmp";
                string bakPath = saveFilePath + ".bak";

                File.WriteAllText(tempPath, finalJson);

                if (File.Exists(saveFilePath))
                {
                    if (File.Exists(bakPath)) File.Delete(bakPath);
                    File.Move(saveFilePath, bakPath);
                }

                File.Move(tempPath, saveFilePath);

                if (File.Exists(bakPath)) File.Delete(bakPath);
            });
        }

        catch (Exception ex)
        {
            Debug.LogError("[GameSaveManager] 세이브 실패 " + ex);
        }

        finally
        {
            isBusy = false;
        }
    }

    private async Task<SaveDataGame> Load()
    {
        if (isBusy)
        {
            Debug.LogWarning("[GameSaveManager] busy 상태");
            return null;
        }

        isBusy = true;

        try
        {
            return await Task.Run(() =>
            {
                string bakPath = saveFilePath + ".bak";
                string tmpPath = saveFilePath + ".tmp";

                try
                {
                    if (File.Exists(tmpPath))
                    {
                        File.Delete(tmpPath);
                    }
                }

                catch (Exception ex)
                {
                    Debug.LogWarning($"[GameSaveManager] tmp 삭제 실패: {ex.Message}");
                }

                string pathToRead = null;
                if (File.Exists(saveFilePath)) pathToRead = saveFilePath;
                else if (File.Exists(bakPath)) pathToRead = bakPath;

                if (pathToRead == null) return null;

                string finalJson = File.ReadAllText(pathToRead);
                SaveDataJson saveDataJson = JsonUtility.FromJson<SaveDataJson>(finalJson);

                if (saveDataJson == null || string.IsNullOrEmpty(saveDataJson.saveFile)) return null;

                // 복호화
                string decryptFile = aes.Decrypt(saveDataJson.saveFile);
                if (string.IsNullOrEmpty(decryptFile)) return null;

                SaveData saveData = JsonUtility.FromJson<SaveData>(decryptFile);
                if (saveData == null) return null;

                SaveDataGame saveDataGame = new SaveDataGame();

                saveDataGame.saveData = saveData;
                saveDataGame.saveUTC = saveDataJson.saveUTC;

                return saveDataGame;
            });
        }

        catch (Exception ex)
        {
            Debug.LogError("[GameSaveManager] 로드 실패" + ex);
            return null;
        }

        finally
        {
            isBusy = false;
        }
    }
    #endregion

    #region Public API - Initialize / Save / Load
    public void InitializeNewGame(GameLevel startLevel, int startDay = 1)
    {
        SaveData data = new SaveData();

        data.level = startLevel;
        data.day = startDay;

        data.score = new int[MAX_DAYS];
        data.correct = new int[MAX_DAYS];
        data.choice = new string[MAX_DAYS];

        data.collectItemFlag = 0;
        data.totalPlaySeconds = 0;

        Current = data;

        BeginPlaySession();
    }

    public async Task SaveCurrent()
    {
        if (Current == null)
        {
            Debug.LogWarning("[GameSaveManager] Current가 null입니다. 게임 시작/로드 전이라 저장 불가.");
            return;
        }

        try
        {
            OnWriteSaveData?.Invoke(Current);
        }

        catch (Exception ex)
        {
            Debug.LogError("[GameSaveManager] OnWriteSaveData 예외: " + ex);
            return;
        }

        await Save(Current);
    }

    public async Task LoadCurrent()
    {
        SaveDataGame loaded = await Load();
        if (loaded != null && loaded.saveData != null)
        {
            Current = loaded.saveData;
            BeginPlaySession();
            InventoryManager.Instance.SetItemFlag(Current.collectItemFlag);
        }
    }

    public async Task<SaveDataGame> LoadPreview()
    {
        return await Load();
    }

    #endregion

    #region 플레이 타임 계산
    private void BeginPlaySession()
    {
        playSessionStartRealtime = Time.realtimeSinceStartup;
        playSessionStarted = true;
    }


    private void AccumulatePlayTime(SaveData data)
    {
        if (data == null) return;

        if (!playSessionStarted)
        {
            BeginPlaySession();
            return;
        }

        float delta = Time.realtimeSinceStartup - playSessionStartRealtime;
        if (delta < 0f) delta = 0f;

        data.totalPlaySeconds += (long)delta;
        playSessionStartRealtime = Time.realtimeSinceStartup;
    }
    #endregion
}
