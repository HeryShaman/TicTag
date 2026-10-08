using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicPlayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.6f;
    [SerializeField, Min(0.05f)] private float switchFadeOut = 0.4f;
    [SerializeField, Min(0.1f)] private float duckSpeed = 4f;

    private AudioSource _source;
    private float _fade;
    private float _fadeTarget;
    private float _fadeSpeed = 1f;
    private float _duck = 1f;
    private float _duckTarget = 1f;
    private bool _stopWhenSilent;
    private AudioClip _pendingClip;
    private float _pendingFadeIn;

    private void Awake()
    {
        _source = GetComponent<AudioSource>();
        _source.playOnAwake = false;
        _source.loop = true;
        _source.spatialBlend = 0f;
        _source.volume = 0f;
    }

    public void Play(AudioClip clip, float fadeInSeconds = 1f)
    {
        if (clip == null) return;

        // Même morceau déjà en lecture : on s'assure juste qu'il est audible.
        if (_source.isPlaying && _source.clip == clip && !_stopWhenSilent)
        {
            _pendingClip = null;
            SetFade(1f, fadeInSeconds);
            return;
        }

        // Autre morceau en lecture : fondu sortant puis démarrage du nouveau.
        if (_source.isPlaying)
        {
            _pendingClip = clip;
            _pendingFadeIn = fadeInSeconds;
            SetFade(0f, switchFadeOut);
            _stopWhenSilent = true;
            return;
        }

        StartClip(clip, fadeInSeconds);
    }

    public void Stop(float fadeOutSeconds = 1f)
    {
        _pendingClip = null;
        if (!_source.isPlaying) return;

        SetFade(0f, fadeOutSeconds);
        _stopWhenSilent = true;
    }

    // 1 = volume normal, 0.35 = atténué (menu pause)
    public void SetDuck(float multiplier)
    {
        _duckTarget = Mathf.Clamp01(multiplier);
    }

    private void StartClip(AudioClip clip, float fadeIn)
    {
        _source.clip = clip;
        _source.loop = true;
        _fade = fadeIn <= 0f ? 1f : 0f;
        SetFade(1f, fadeIn);
        _stopWhenSilent = false;
        _source.Play();
    }

    private void SetFade(float target, float seconds)
    {
        _fadeTarget = target;
        _fadeSpeed = 1f / Mathf.Max(0.01f, seconds);
    }

    private void Update()
    {
        float dt = Time.unscaledDeltaTime;

        _fade = Mathf.MoveTowards(_fade, _fadeTarget, _fadeSpeed * dt);
        _duck = Mathf.MoveTowards(_duck, _duckTarget, duckSpeed * dt);
        _source.volume = maxVolume * _fade * _duck;

        if (_stopWhenSilent && _fade <= 0f)
        {
            _source.Stop();
            _stopWhenSilent = false;

            if (_pendingClip != null)
            {
                AudioClip next = _pendingClip;
                _pendingClip = null;
                StartClip(next, _pendingFadeIn);
            }
        }
    }
}