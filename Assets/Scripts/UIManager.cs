using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI Panels")]
    public GameObject startScreen;
    public GameObject playerDeathScreen;
    public GameObject bossDefeatedScreen;
    public GameObject gameplayUI; // HUD elements during gameplay



    [Header("Game Objects")]
    public GameObject player;
    public GameObject boss;
    public GameObject levelObjects; // Parent object containing environment, enemies, etc.

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Start with the main menu
        ShowStartScreen();
    }

    public void StartGame()
    {
        HideAllScreens();
        gameplayUI.SetActive(true);

        // Enable player, boss, and level objects
        EnableGameplay(true);
        MusicManager.instance.PlayBossFightMusic();


        // Reset game state
        ResetGame();

        // Make sure time is running
        Time.timeScale = 1f;
    }

    public void ShowPlayerDeathScreen()
    {
        MusicManager.instance.PlayDeathSound();
        HideAllScreens();
        playerDeathScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Optionally pause the game
        Time.timeScale = 0f;
    }

    public void ShowBossDefeatedScreen()
    {
        HideAllScreens();
        bossDefeatedScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Optionally pause the game
        Time.timeScale = 0f;
    }

    public void ShowStartScreen()
    {
        HideAllScreens();
        startScreen.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnableGameplay(false);
    
        if (MusicManager.instance != null)
        {
            Debug.Log("Calling PlayMainMenuMusic()");
            MusicManager.instance.PlayMainMenuMusic();
        }
        else
        {
            Debug.LogError("MusicManager instance not found!");
        }

        Time.timeScale = 1f;
    }

    private void EnableGameplay(bool enable)
    {
        if (player != null) player.SetActive(enable);
        if (boss != null) boss.SetActive(enable);
        if (levelObjects != null) levelObjects.SetActive(enable);
    }

    private void ResetGame()
    {
        // Reset player
        if (player != null)
        {
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.ResetPlayer();
            }

            // Reset player position
            player.transform.position = new Vector3(0, 1, 0); // Adjust as needed
        }

        // Reset boss
        if (boss != null)
        {
            EnemyHealth bossHealth = boss.GetComponent<EnemyHealth>();
            if (bossHealth != null)
            {
                bossHealth.ResetEnemy();
            }

            // Re-enable boss components if they were disabled
            BossStateMachine stateMachine = boss.GetComponent<BossStateMachine>();
            if (stateMachine != null) stateMachine.enabled = true;

            BossPersonalitySystem personality = boss.GetComponent<BossPersonalitySystem>();
            if (personality != null) personality.enabled = true;

            // Re-enable colliders
            foreach (Collider col in boss.GetComponentsInChildren<Collider>())
            {
                col.enabled = true;
            }
        }
    }

    private void HideAllScreens()
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (playerDeathScreen != null) playerDeathScreen.SetActive(false);
        if (bossDefeatedScreen != null) bossDefeatedScreen.SetActive(false);
        if (gameplayUI != null) gameplayUI.SetActive(false);
    }

    // Quit game (for standalone builds)
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}