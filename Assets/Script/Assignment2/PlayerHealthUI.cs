using UnityEngine;
using UnityEngine.UI;

public class PlayerHealthUI : MonoBehaviour
{
    public CarHealth playerHealth;
    public Image fillImage;

    void Update()
    {
        if (playerHealth == null)
            RebindToActivePlayer();

        if (playerHealth == null || fillImage == null) return;

        float percent = 0f;
        if (playerHealth.maxHealth > 0)
        {
            percent = (float)playerHealth.currentHealth / playerHealth.maxHealth;
        }

        percent = Mathf.Clamp01(percent);
        fillImage.fillAmount = percent;
    }

    public void SetPlayerHealth(CarHealth health)
    {
        playerHealth = health;
        Update();
    }

    void RebindToActivePlayer()
    {
        CarPlayerInput[] players = FindObjectsByType<CarPlayerInput>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null || !players[i].isPlayerCar || !players[i].isActiveAndEnabled)
                continue;

            playerHealth = players[i].GetComponent<CarHealth>();
            if (playerHealth != null)
                return;
        }
    }
}
