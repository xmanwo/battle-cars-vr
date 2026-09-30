using UnityEngine;
using UnityEngine.UI;

public class EnemyWorldHealthBar : MonoBehaviour
{
    public CarHealth targetHealth;
    public Transform targetRoot;
    public Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);

    public Image fillImage;
    public Camera lookCamera;

    void Start()
    {
        if (lookCamera == null)
        {
            lookCamera = Camera.main;
        }
    }

    void LateUpdate()
    {
        if (targetHealth == null || targetRoot == null)
        {
            gameObject.SetActive(false);
            return;
        }

        if (targetHealth.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        transform.position = targetRoot.position + worldOffset;

        if (lookCamera != null)
        {
            transform.forward = lookCamera.transform.forward;
        }

        if (fillImage != null && targetHealth.maxHealth > 0)
        {
            float percent = (float)targetHealth.currentHealth / targetHealth.maxHealth;
            fillImage.fillAmount = Mathf.Clamp01(percent);
        }
    }
}