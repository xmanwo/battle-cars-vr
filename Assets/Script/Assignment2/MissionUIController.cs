using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

public class MissionUIController : MonoBehaviour
{
    [Header("Refs")]
    public SimpleMissionManager mission;
    public CarHealth playerHealth;

    [Header("Mission Text")]
    public TMP_Text flowerText;
    public TMP_Text enemyText;

    [Header("Start Panel")]
    public GameObject startPanel;
    public TMP_Text startText;

    [Header("Win Panel")]
    public GameObject winPanel;
    public TMP_Text winText;

    [Header("Mission Complete Panel")]
    public GameObject missionCompletePanel;
    public TMP_Text missionCompleteText;

    [Header("Lose Panel")]
    public GameObject losePanel;
    public TMP_Text loseText;

    [Header("Game State")]
    public bool gameStarted = false;
    public bool freezeOnWin = true;
    public bool freezeOnLose = true;

    private bool missionCompleteShown = false;

    void Start()
    {
        Time.timeScale = 0f;
        gameStarted = false;

        if (startPanel != null) startPanel.SetActive(true);
        if (winPanel != null) winPanel.SetActive(false);
        if (missionCompletePanel != null) missionCompletePanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);

        if (startText != null)
        {
            startText.text =
                "MISSION\n\n" +
                "Collect all flowers\n" +
                "Defeat all enemies\n" +
                "Reach the exit\n\n" +

                "Xbox Controller:\n" +
                "Left Stick: Steer\n" +
                "RT: Accelerate\n" +
                "LT: Brake / Reverse\n" +
                "Y: Drop bomb\n" +
                "X: Self-right / Start\n\n" +

                "Keyboard & Mouse:\n" +
                "Mouse: Aim\n" +
                "Left Mouse Button: Shoot\n" +
                "E: Drop bomb\n" +
                "W: Accelerate\n" +
                "A / D: Steer\n" +
                "S: Brake / Reverse\n" +
                "R: Self-right\n\n" +

                "Press Xbox X or SPACE to start";
        }
    }

    void Update()
    {
        UpdateMissionText();
        HandleStartInput();
        HandleMissionCompletePopup();
        HandleWinState();
        HandleLoseState();
    }

    void UpdateMissionText()
    {
        if (mission == null) return;

        if (flowerText != null)
        {
            flowerText.text = "Collect Flowers: " + mission.flowersCollected + "/" + mission.flowersNeeded;
        }

        if (enemyText != null)
        {
            enemyText.text = "Defeat Enemies: " + mission.enemiesKilled + "/" + mission.totalEnemies;
        }
    }

    void HandleStartInput()
    {
        if (gameStarted) return;

        bool keyboardStartSpace = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        bool xboxStartX = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame; // Xbox X

        if (keyboardStartSpace || xboxStartX)
        {
            gameStarted = true;
            Time.timeScale = 1f;

            if (startPanel != null)
                startPanel.SetActive(false);
        }
    }

    void HandleMissionCompletePopup()
    {
        if (mission == null) return;
        if (missionCompleteShown) return;

        if (mission.missionUnlocked)
        {
            missionCompleteShown = true;

            if (missionCompletePanel != null)
                missionCompletePanel.SetActive(true);

            if (missionCompleteText != null)
            {
                missionCompleteText.text = "Mission complete.\nProceed to the extraction point.";
            }
        }
    }

    void HandleWinState()
    {
        if (mission == null) return;
        if (!mission.playerWon) return;

        if (winPanel != null && !winPanel.activeSelf)
        {
            winPanel.SetActive(true);
        }

        if (winText != null)
        {
            winText.text = "YOU WIN\n\nPress R or Xbox X to restart";
        }

        if (freezeOnWin)
        {
            Time.timeScale = 0f;
        }

        bool keyboardRestart = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool xboxRestart = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame; // Xbox X

        if (keyboardRestart || xboxRestart)
        {
            RestartScene();
        }
    }

    void HandleLoseState()
    {
        if (playerHealth == null) return;
        if (!playerHealth.IsDead) return;

        if (losePanel != null && !losePanel.activeSelf)
        {
            losePanel.SetActive(true);
        }

        if (loseText != null)
        {
            loseText.text = "YOU DIED\n\nPress R or Xbox X to restart";
        }

        if (freezeOnLose)
        {
            Time.timeScale = 0f;
        }

        bool keyboardRestart = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool xboxRestart = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame; // Xbox X

        if (keyboardRestart || xboxRestart)
        {
            RestartScene();
        }
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}