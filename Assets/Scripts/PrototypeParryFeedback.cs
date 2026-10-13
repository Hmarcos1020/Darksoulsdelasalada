using UnityEngine;

/// <summary>Sonidos provisionales de resultado; escucha eventos sin decidir defensa ni daño.</summary>
[RequireComponent(typeof(AudioSource))]
public sealed class PrototypeParryFeedback : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float volume = 0.12f;
    private PrototypePlayerParry parry;
    private PrototypeEnemyParry enemyParry;
    private AudioSource source;
    private AudioClip success, failure;
    private void Awake()
    {
        parry = GetComponent<PrototypePlayerParry>(); source = GetComponent<AudioSource>();
        enemyParry = GetComponent<PrototypeEnemyParry>();
        source.playOnAwake = false; source.spatialBlend = 0f;
        success = CreateTone("Parry acierto provisional", enemyParry != null ? 850f : 1050f, enemyParry != null ? 1200f : 1500f);
        failure = CreateTone("Parry fallo provisional", 180f, 120f);
    }
    private void OnEnable()
    {
        if (parry == null) parry = GetComponent<PrototypePlayerParry>();
        if (enemyParry == null) enemyParry = GetComponent<PrototypeEnemyParry>();
        if (parry != null) { parry.Succeeded += OnSuccess; parry.Failed += OnFailure; }
        if (enemyParry != null) enemyParry.Succeeded += OnSuccess;
    }
    private void OnDisable()
    {
        if (parry != null) { parry.Succeeded -= OnSuccess; parry.Failed -= OnFailure; }
        if (enemyParry != null) enemyParry.Succeeded -= OnSuccess;
        if (source != null) source.Stop();
    }
    private void OnSuccess() { if (source != null && success != null) source.PlayOneShot(success, volume); }
    private void OnFailure() { if (source != null && failure != null) source.PlayOneShot(failure, volume); }
    private static AudioClip CreateTone(string name, float first, float second)
    {
        const int sampleRate = 44100, count = 5292;
        var samples = new float[count];
        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = Mathf.Min(1f, t / 0.005f) * (1f - (float)i / count);
            samples[i] = 0.35f * envelope * (Mathf.Sin(2f * Mathf.PI * first * t) + Mathf.Sin(2f * Mathf.PI * second * t));
        }
        var clip = AudioClip.Create(name, count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
    private void OnDestroy() { if (success != null) Destroy(success); if (failure != null) Destroy(failure); }
}
