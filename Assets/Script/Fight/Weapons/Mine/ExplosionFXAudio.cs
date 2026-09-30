using UnityEngine;

public class ExplosionFXAudio : MonoBehaviour
{
    [Header("Audio")]
    public AudioClip explosionSound;
    public AudioSource audioSource;

    void OnEnable()
    {
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound, 1f);
        }
    }
}