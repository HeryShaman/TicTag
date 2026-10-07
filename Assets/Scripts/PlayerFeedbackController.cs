using UnityEngine;
using TMPro;

public class PlayerFeedbackController : MonoBehaviour
{
    public enum VfxSpace
    {
        PlayerRoot,   // suit la position du joueur, ne tourne pas avec lui
        FacingRoot    // suit la position ET l'orientation (yaw) du joueur
    }

    [System.Serializable]
    public class LoopVfx
    {
        public ParticleSystem particles;
        public VfxSpace space = VfxSpace.FacingRoot;
        [Tooltip("Position locale par rapport au repère choisi.")]
        public Vector3 localPosition;
        [Tooltip("Orientation locale (angles d'Euler) par rapport au repère choisi.")]
        public Vector3 localEulerAngles;

        private bool _playing;

        public void Init(Transform root, Transform facing)
        {
            if (particles == null) return;

            Transform parent = (space == VfxSpace.FacingRoot && facing != null) ? facing : root;
            Transform t = particles.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(localEulerAngles);

            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _playing = false;
        }

        public void Set(bool play)
        {
            if (particles == null || play == _playing) return;
            _playing = play;

            if (play) particles.Play(true);
            else particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    [System.Serializable]
    public class Sfx
    {
        [Tooltip("Plusieurs clips = variation aléatoire (sans répétition immédiate).")]
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 1f;
        public Vector2 pitchRange = new Vector2(0.95f, 1.05f);
        [Tooltip("Délai minimal entre deux lectures de CE son pour CE joueur.")]
        [Min(0f)] public float cooldown = 0.1f;
        [Tooltip("Un nouveau son coupe le précédent de même type (évite l'empilement).")]
        public bool interruptPrevious = true;

        private AudioSource _source;
        private AudioSource _template;
        private float _nextAllowedTime;
        private int _lastIndex = -1;

        public void Init(Transform parent, AudioSource template)
        {
            _template = template;

            GameObject go = new GameObject("SFX_Voice");
            go.transform.SetParent(parent, false);
            _source = go.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            CopySettings(_source);
        }

        private void CopySettings(AudioSource dst)
        {
            if (_template != null)
            {
                dst.outputAudioMixerGroup = _template.outputAudioMixerGroup;
                dst.spatialBlend = _template.spatialBlend;
                dst.priority = _template.priority;
            }
            else
            {
                dst.spatialBlend = 0f; // 2D
            }
        }

        private AudioClip PickClip()
        {
            if (clips == null || clips.Length == 0) return null;

            int i = Random.Range(0, clips.Length);
            if (clips.Length > 1 && i == _lastIndex) i = (i + 1) % clips.Length;
            _lastIndex = i;
            return clips[i];
        }

        public bool TryPlay()
        {
            if (_source == null || Time.time < _nextAllowedTime) return false;

            AudioClip clip = PickClip();
            if (clip == null) return false;

            _nextAllowedTime = Time.time + cooldown;
            _source.pitch = Random.Range(pitchRange.x, pitchRange.y);

            if (interruptPrevious)
            {
                _source.Stop();
                _source.clip = clip;
                _source.volume = volume;
                _source.Play();
            }
            else
            {
                _source.PlayOneShot(clip, volume);
            }
            return true;
        }

        public bool TryPlayDetached(Vector3 position)
        {
            if (Time.time < _nextAllowedTime) return false;

            AudioClip clip = PickClip();
            if (clip == null) return false;

            _nextAllowedTime = Time.time + cooldown;

            GameObject go = new GameObject("SFX_Detached");
            go.transform.position = position;
            AudioSource src = go.AddComponent<AudioSource>();
            CopySettings(src);

            float pitch = Random.Range(pitchRange.x, pitchRange.y);
            src.clip = clip;
            src.volume = volume;
            src.pitch = pitch;
            src.Play();

            Object.Destroy(go, clip.length / Mathf.Max(0.01f, Mathf.Abs(pitch)) + 0.1f);
            return true;
        }
    }

 
    // Orientation / Lean / Balancier


    [Header("Orientation 3D")]
    [SerializeField] private Transform orientationRoot;

    [SerializeField] private float rotationSpeed = 1080f;
    [SerializeField] private float minMoveMagnitude = 0.05f;
    [SerializeField] private float minFaceSpeed = 0.5f;


    [Header("Panic Pulse")]
    [SerializeField] private Transform pulseRoot;
    [SerializeField] private float pulseMinScale = 0.8f;
    [SerializeField] private float pulseMaxScale = 1.2f;
    [SerializeField, Min(0.1f)] private float pulseFrequencyStart = 2f;
    [SerializeField, Min(0.1f)] private float pulseFrequencyEnd = 5f;
    [SerializeField, Min(0.1f)] private float pulseBlendSpeed = 6f;

    // Animation (états de l'Animator joués par CrossFade : aucune transition à créer)
 
    [Header("Animation (noms des états dans l'Animator Controller)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string moveState = "Move";
    [SerializeField] private string idleBombState = "IdleBomb";
    [SerializeField] private string moveBombState = "MoveBomb";
    [SerializeField] private string idlePanicState = "IdlePanic";
    [SerializeField] private string movePanicState = "MovePanic";
    [SerializeField] private string pushState = "Push";
    [SerializeField] private string deathState = "Death";
    [SerializeField, Min(0f)] private float crossFadeTime = 0.1f;
    [SerializeField, Min(0.05f)] private float pushDuration = 0.35f;
    [SerializeField, Min(0f)] private float moveSpeedThreshold = 0.3f;

    // VFX

    [Header("VFX - Boucles")]
    [SerializeField] private LoopVfx walkVfx = new LoopVfx();
    [SerializeField] private LoopVfx panicVfx = new LoopVfx();
    [SerializeField, Min(0f)] private float dangerRadius = 3f;

    [Header("VFX - Bombe (objet optionnel)")]
    [SerializeField] private GameObject bombActiveVFX;

    [Header("VFX - Explosion")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private VfxSpace explosionSpace = VfxSpace.PlayerRoot;
    [SerializeField] private Vector3 explosionLocalPosition;
    [SerializeField] private Vector3 explosionLocalEulerAngles;
    [SerializeField, Min(0.1f)] private float explosionLifetime = 5f;

    // Audio

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Sfx walkSfx = new Sfx { cooldown = 0.12f };
    [SerializeField] private Sfx panicSfx = new Sfx { cooldown = 0.3f };
    [SerializeField] private Sfx tagSfx = new Sfx { cooldown = 0.2f };
    [SerializeField] private Sfx clickSfx = new Sfx { cooldown = 0.05f };
    [SerializeField] private Sfx explosionSfx = new Sfx { cooldown = 0f, interruptPrevious = false };

    [Header("Audio - Cadences")]
    [SerializeField, Min(0.05f)] private float stepIntervalSlow = 0.45f;
    [SerializeField, Min(0.05f)] private float stepIntervalFast = 0.25f;
    [SerializeField, Min(0.05f)] private float panicSfxInterval = 0.6f;
    [SerializeField, Min(0.05f)] private float clickInterval = 1f;

    // UI

    [Header("Bomb Timer UI")]

    [SerializeField] private TMP_Text bombTimerText;
    [SerializeField] private Transform timerAnchor;
    [SerializeField] private Vector3 timerLocalOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private Vector3 timerWorldOffset = Vector3.zero;
    [SerializeField] private bool followHeight = false;
    [SerializeField] private float timerHeight = 2.5f;
    [SerializeField] private bool matchCameraRotation = true;
    [SerializeField] private Vector3 timerFixedEuler = new Vector3(90f, 0f, 0f);

    // État interne

    private HotPotatoCharacter _owner;
    private Camera _cam;

    private float _leanAngle;
    private float _leanVelocity;
    private Vector3 _leanBasePosition;
    private float _bobPhase;
    private float _bobBlend;

    private const float TwoPi = Mathf.PI * 2f;
    private Vector3 _pulseBaseScale = Vector3.one;
    private float _pulsePhase;
    private float _pulseBlend;
    private float _pulseScale = 1f;
    private bool _wasPanic;
    private float _panicStartBombTime = 1f;

    private bool _isMoving;
    private bool _isDead;

    private int _hIdle, _hMove, _hIdleBomb, _hMoveBomb, _hIdlePanic, _hMovePanic, _hPush, _hDeath;
    private int _currentStateHash;
    private float _pushTimer;

    private float _stepTimer;
    private float _panicSfxTimer;
    private int _lastClickTick = -1;
    private int _lastDisplayedBombTimer = -1;

    // =====================================================================
    // Initialisation
    // =====================================================================

    private void Awake()
    {
        _owner = GetComponentInParent<HotPotatoCharacter>();

        if (orientationRoot == null) orientationRoot = transform;
        if (pulseRoot != null) _pulseBaseScale = pulseRoot.localScale;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.applyRootMotion = false;   // le Rigidbody pilote la position

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        _hIdle = Hash(idleState);
        _hMove = Hash(moveState);
        _hIdleBomb = Hash(idleBombState);
        _hMoveBomb = Hash(moveBombState);
        _hIdlePanic = Hash(idlePanicState);
        _hMovePanic = Hash(movePanicState);
        _hPush = Hash(pushState);
        _hDeath = Hash(deathState);
        ValidateAnimatorStates();

        walkVfx.Init(transform, orientationRoot);
        panicVfx.Init(transform, orientationRoot);

        walkSfx.Init(transform, audioSource);
        panicSfx.Init(transform, audioSource);
        tagSfx.Init(transform, audioSource);
        clickSfx.Init(transform, audioSource);
        explosionSfx.Init(transform, audioSource);
    }

    // LateUpdate : s'exécute après UpdateFeedback (lean/bob) ET après l'Animator,
    // donc l'ancre de la tête est dans sa pose finale de la frame.
    private void LateUpdate()
    {
        // Appliqué ici pour l'emporter sur un éventuel curve de scale dans l'Animator.
        if (!_isDead && pulseRoot != null)
            pulseRoot.localScale = _pulseBaseScale * _pulseScale;

        UpdateTimerTextPlacement();
    }

    private void UpdatePulse(bool panic, float bombTime, float deltaTime)
    {
        if (panic)
        {
            if (!_wasPanic)
            {
                _wasPanic = true;
                _panicStartBombTime = Mathf.Max(0.01f, bombTime);
                _pulsePhase = 0f;   // sin(0) = 0 -> échelle 1 à l'entrée, pas de saut
            }

            // La cadence accélère de pulseFrequencyStart à pulseFrequencyEnd jusqu'à l'explosion.
            float progress = 1f - Mathf.Clamp01(bombTime / _panicStartBombTime);
            float frequency = Mathf.Lerp(pulseFrequencyStart, pulseFrequencyEnd, progress);
            _pulsePhase = (_pulsePhase + TwoPi * frequency * deltaTime) % TwoPi;
        }
        else
        {
            _wasPanic = false;
        }

        _pulseBlend = Mathf.MoveTowards(_pulseBlend, panic ? 1f : 0f, pulseBlendSpeed * deltaTime);

        float wave = 0.5f + 0.5f * Mathf.Sin(_pulsePhase);
        float pulseScale = Mathf.Lerp(pulseMinScale, pulseMaxScale, wave);
        _pulseScale = Mathf.Lerp(1f, pulseScale, _pulseBlend);
    }

    private void UpdateTimerTextPlacement()
    {
        if (bombTimerText == null) return;

        // Position : on suit la tête (donc l'inclinaison et le balancier) mais uniquement en position.
        Vector3 head;
        if (timerAnchor != null) head = timerAnchor.position;
        else head = transform.TransformPoint(timerLocalOffset);

        float y = followHeight ? head.y : transform.position.y + timerHeight;

        Transform t = bombTimerText.transform;
        t.position = new Vector3(head.x, y, head.z) + timerWorldOffset;

        // Rotation : jamais héritée du personnage.
        if (matchCameraRotation)
        {
            if (_cam == null) _cam = Camera.main;
            t.rotation = _cam != null ? _cam.transform.rotation : Quaternion.Euler(timerFixedEuler);
        }
        else
        {
            t.rotation = Quaternion.Euler(timerFixedEuler);
        }
    }

    private static int Hash(string stateName) => Animator.StringToHash(stateName);

    private void ValidateAnimatorStates()
    {
        if (animator == null) return;

        string[] names = { idleState, moveState, idleBombState, moveBombState,
                           idlePanicState, movePanicState, pushState, deathState };

        foreach (string stateName in names)
        {
            if (!animator.HasState(0, Hash(stateName)))
                Debug.LogWarning($"[{name}] État Animator introuvable (layer 0) : \"{stateName}\"", this);
        }
    }

    // =====================================================================
    // API appelée par HotPotatoCharacter
    // =====================================================================

    public void UpdateFeedback(
        Vector2 moveInput,
        Vector3 velocity,
        float maxSpeed,
        bool hasBomb,
        bool isPanic,
        bool isGrounded,
        float bombTime,
        float bombDuration,
        float deltaTime
    )
    {
        if (_isDead) return;

        Vector3 flatVelocity = new Vector3(velocity.x, 0f, velocity.z);
        float speed = flatVelocity.magnitude;
        float speed01 = maxSpeed <= 0f ? 0f : Mathf.Clamp01(speed / maxSpeed);

        UpdateMovingFlag(speed);
        Orient(moveInput, flatVelocity, deltaTime);
        UpdatePulse(hasBomb && isPanic, bombTime, deltaTime);

        bool walking = _isMoving && isGrounded;
        bool danger = !hasBomb && IsBombHolderNearby();
        bool panicFx = (hasBomb && isPanic) || danger;

        walkVfx.Set(walking);
        panicVfx.Set(panicFx);

        UpdateAnimation(hasBomb, isPanic, deltaTime);
        UpdateLoopSfx(hasBomb, isPanic, walking, speed01, deltaTime);
        UpdateBombTimer(hasBomb, bombTime, bombDuration);

        if (bombActiveVFX != null)
            bombActiveVFX.SetActive(hasBomb);
    }

    public void OnBombReceived()
    {
        _lastClickTick = -1;   // force le clic immédiat dès le prochain UpdateFeedback

        if (bombActiveVFX != null)
            bombActiveVFX.SetActive(true);
    }

    /// <summary>Appelé quand CE joueur tag un autre joueur : animation Push + Tag SFX.</summary>
    public void OnBombTransferred()
    {
        if (_isDead) return;

        tagSfx.TryPlay();

        _pushTimer = pushDuration;
        PlayState(_hPush);

        _lastClickTick = -1;

        if (bombActiveVFX != null)
            bombActiveVFX.SetActive(false);
    }

    public void OnExplosion()
    {
        if (_isDead) return;
        _isDead = true;

        // Fin du battement : on rend la main à l'animation de mort.
        _pulseScale = 1f;
        _pulseBlend = 0f;
        if (pulseRoot != null) pulseRoot.localScale = _pulseBaseScale;

        walkVfx.Set(false);
        panicVfx.Set(false);

        SpawnExplosionVfx();
        explosionSfx.TryPlayDetached(transform.position);

        PlayState(_hDeath);

        if (bombActiveVFX != null)
            bombActiveVFX.SetActive(false);

        if (bombTimerText != null)
            bombTimerText.text = string.Empty;
    }

    // =====================================================================
    // Visuel : orientation
    // =====================================================================

    private void UpdateMovingFlag(float speed)
    {
        // Hystérésis : évite le clignotement idle/move autour du seuil.
        float threshold = _isMoving ? moveSpeedThreshold * 0.6f : moveSpeedThreshold;
        _isMoving = speed > threshold;
    }

    private void Orient(Vector2 moveInput, Vector3 flatVelocity, float deltaTime)
    {
        if (orientationRoot == null) return;

        // Priorité à l'intention du joueur (input), sinon à la vitesse réelle (glisse, knockback).
        Vector3 direction = new Vector3(moveInput.x, 0f, moveInput.y);

        if (direction.sqrMagnitude < minMoveMagnitude * minMoveMagnitude)
        {
            if (flatVelocity.sqrMagnitude < minFaceSpeed * minFaceSpeed) return;
            direction = flatVelocity;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        orientationRoot.rotation = Quaternion.RotateTowards(
            orientationRoot.rotation,
            targetRotation,
            rotationSpeed * deltaTime
        );
    }

    // =====================================================================
    // Animation
    // =====================================================================

    private void UpdateAnimation(bool hasBomb, bool isPanic, float deltaTime)
    {
        if (animator == null) return;

        // L'animation Push est prioritaire pendant pushDuration.
        if (_pushTimer > 0f)
        {
            _pushTimer -= deltaTime;
            if (_pushTimer > 0f) return;
            _currentStateHash = 0; // force la ré-évaluation
        }

        int target;
        if (!hasBomb)
            target = _isMoving ? _hMove : _hIdle;
        else if (!isPanic)
            target = _isMoving ? _hMoveBomb : _hIdleBomb;
        else
            target = _isMoving ? _hMovePanic : _hIdlePanic;

        if (target != _currentStateHash)
            PlayState(target);
    }

    private void PlayState(int stateHash)
    {
        if (animator == null) return;

        animator.CrossFadeInFixedTime(stateHash, crossFadeTime);
        _currentStateHash = stateHash;
    }

    // =====================================================================
    // VFX
    // =====================================================================

    private bool IsBombHolderNearby()
    {
        if (_owner == null) return false;

        var all = HotPotatoCharacter.AllCharacters;
        Vector3 myPos = _owner.transform.position;
        float r2 = dangerRadius * dangerRadius;

        for (int i = 0; i < all.Count; i++)
        {
            HotPotatoCharacter other = all[i];
            if (other == null || other == _owner || !other.IsAlive || !other.HasBomb) continue;

            Vector3 d = other.transform.position - myPos;
            d.y = 0f;
            if (d.sqrMagnitude <= r2) return true;
        }
        return false;
    }

    private void SpawnExplosionVfx()
    {
        if (explosionVFX == null) return;

        Transform reference = (explosionSpace == VfxSpace.FacingRoot && orientationRoot != null)
            ? orientationRoot
            : transform;

        Vector3 position = reference.TransformPoint(explosionLocalPosition);
        Quaternion rotation = reference.rotation * Quaternion.Euler(explosionLocalEulerAngles);

        GameObject instance = Instantiate(explosionVFX, position, rotation);
        Destroy(instance, explosionLifetime);
    }

    // =====================================================================
    // Audio
    // =====================================================================

    private void UpdateLoopSfx(bool hasBomb, bool isPanic, bool walking, float speed01, float deltaTime)
    {
        if (_stepTimer > 0f) _stepTimer -= deltaTime;
        if (_panicSfxTimer > 0f) _panicSfxTimer -= deltaTime;

        // Pas : plus rapides quand le joueur va vite.
        if (walking && _stepTimer <= 0f)
        {
            walkSfx.TryPlay();
            _stepTimer = Mathf.Lerp(stepIntervalSlow, stepIntervalFast, speed01);
        }

        // Panic SFX : uniquement en déplacement avec la bombe en état panique.
        if (hasBomb && isPanic && _isMoving && _panicSfxTimer <= 0f)
        {
            panicSfx.TryPlay();
            _panicSfxTimer = panicSfxInterval;
        }
    }

    // =====================================================================
    // Timer de bombe : texte + clic synchronisé
    // =====================================================================

    private void UpdateBombTimer(bool hasBomb, float bombTime, float bombDuration)
    {
        if (!hasBomb)
        {
            _lastClickTick = -1;

            if (bombTimerText != null && _lastDisplayedBombTimer != -1)
            {
                bombTimerText.text = string.Empty;
                _lastDisplayedBombTimer = -1;
            }
            return;
        }

        // Clic : un au contact (tick 0), puis un par clickInterval, calé sur le temps écoulé de la bombe.
        float elapsed = Mathf.Max(0f, bombDuration - bombTime);
        int tick = Mathf.FloorToInt(elapsed / Mathf.Max(0.05f, clickInterval) + 0.0001f);

        if (tick != _lastClickTick)
        {
            _lastClickTick = tick;
            clickSfx.TryPlay();
        }

        if (bombTimerText == null) return;

        int displayedValue = Mathf.CeilToInt(Mathf.Max(0f, bombTime));
        if (displayedValue != _lastDisplayedBombTimer)
        {
            bombTimerText.text = displayedValue.ToString();
            _lastDisplayedBombTimer = displayedValue;
        }
    }
}