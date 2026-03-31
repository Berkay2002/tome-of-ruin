using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState State { get; private set; } = new GameState();

    [Header("References")]
    public HarmonyTable harmonyTable;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RespawnAtLastShrine()
    {
        if (string.IsNullOrEmpty(State.lastShrineScene)) return;
        SceneManager.LoadScene(State.lastShrineScene);
    }

    public void NewGame()
    {
        State.Reset();
        SceneManager.LoadScene("ZoneA");
    }
}
