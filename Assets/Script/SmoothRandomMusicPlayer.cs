using UnityEngine;
using System.Collections;

public class SmoothRandomMusicPlayer : MonoBehaviour
{
    [Header("Music Clips")]
    public AudioClip[] musicClips;

    [Header("Settings")]
    [Range(0f, 1f)] public float masterVolume = 0.5f;
    public float fadeDuration = 2f;

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource currentSource;
    private int lastIndex = -1;

    void Awake()
    {
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        SetupSource(sourceA);
        SetupSource(sourceB);

        currentSource = sourceA;
    }

    void Start()
    {
        if (musicClips != null && musicClips.Length > 0)
            StartCoroutine(PlayRandomLoop());
    }

    void SetupSource(AudioSource source)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.volume = 0f;
        source.spatialBlend = 0f;
    }

    IEnumerator PlayRandomLoop()
    {
        while (true)
        {
            int nextIndex;

            do
            {
                nextIndex = Random.Range(0, musicClips.Length);
            }
            while (nextIndex == lastIndex && musicClips.Length > 1);

            lastIndex = nextIndex;

            AudioSource nextSource = currentSource == sourceA ? sourceB : sourceA;
            nextSource.clip = musicClips[nextIndex];
            nextSource.volume = 0f;
            nextSource.Play();

            yield return StartCoroutine(FadeCross(currentSource, nextSource, fadeDuration));

            currentSource.Stop();
            currentSource.volume = 0f;
            currentSource = nextSource;

            float waitTime = Mathf.Max(currentSource.clip.length - fadeDuration, 0.1f);
            yield return new WaitForSeconds(waitTime);
        }
    }

    IEnumerator FadeCross(AudioSource from, AudioSource to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);

            if (from != null)
                from.volume = Mathf.Lerp(masterVolume, 0f, k);

            if (to != null)
                to.volume = Mathf.Lerp(0f, masterVolume, k);

            yield return null;
        }

        if (from != null)
            from.volume = 0f;

        if (to != null)
            to.volume = masterVolume;
    }
}