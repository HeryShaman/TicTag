using UnityEngine;
using TMPro;

/// <summary>
/// Hiérarchie attendue :
///   Player (Rigidbody, Collider, HotPotatoCharacter, InputReader, PlayerFeedbackController)
///    └ Orientation  (yaw : rotation vers la direction)         -> orientationRoot
///       └ Lean      (pivot aux PIEDS : inclinaison + balancier) -> leanRoot
///          └ Model  (mesh + Animator)
/// </summary>
public class PlayerFeedbackController : MonoBehaviour
{
    // =====================================================================
    // Types utilitaires
    // =====================================================================

    /// <summary>Repère de référence pour placer un VFX.</summary>
    public enum VfxSpace
    {
        PlayerRoot,   // suit la position du joueur, ne tourne pas avec lui
        FacingRoot    // suit la position ET l'orientation (yaw) du joueur
    }

    /// <summary>VFX en boucle (walk, panic) : instance de ParticleSystem présente dans le prefab.</summary>
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

    /// <summary>
    /// Son d'un joueur avec protection anti-saturation :
    /// un cooldown propre, et une "voix" dédiée (un seul exemplaire à la fois si interruptPrevious).
    /// </summary>
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

        /// <summary>Lecture sur la voix du joueur (coupée si le joueur est détruit).</summary>
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

        /// <summary>Lecture indépendante du joueur (explosion : le joueur est détruit juste après).</summary>
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

    // =====================================================================
    // Orientation / Lean / Balancier
    // =====================================================================

    [Header("Orientation 3D")]
    [SerializeField] private Transform orientationRoot;
    [SerializeField] private Transform leanRoot;

    [Tooltip("Degrés/seconde. Valeur élevée = orientation quasi instantanée.")]
    [SerializeField] private float rotationSpeed = 1080f;
    [SerializeField] private float minMoveMagnitude = 0.05f;
    [Tooltip("Vitesse minimale pour s'orienter selon la vitesse quand il n'y a pas d'input (glisse, knockback).")]
    [SerializeField] private float minFaceSpeed = 0.5f;

    [Header("Lean / Spring")]
    [SerializeField] private float maxLeanAngle = 18f;
    [SerializeField] private float leanStiffness = 140f;
    [SerializeField] private float leanDamping = 20f;
    [SerializeField] private float maxLeanVelocity = 500f;

    [Header("Balancier (bob vue du dessus)")]
    [Tooltip("Cycles de balancier par seconde à pleine vitesse (1 cycle = 2 pas).")]
    [SerializeField] private float bobFrequency = 2.2f;
    [Tooltip("Roulis gauche/droite en degrés.")]
    [SerializeField] private float bobRollAngle = 3f;
    [Tooltip("Petite oscillation avant/arrière en degrés.")]
    [SerializeField] private float bobPitchAngle = 1.5f;
    [Tooltip("Rebond vertical en unités.")]
    [SerializeField] private float bobHeight = 0.04f;
    [Tooltip("Vitesse d'apparition / disparition du balancier.")]
    [SerializeField] private float bobBlendSpeed = 8f;

    // =====================================================================
    // Animation (états de l'Animator joués par CrossFade : aucune transition à créer)
    // =====================================================================

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
    [Tooltip("Durée pendant laquelle l'animation Push est maintenue après un tag.")]
    [SerializeField, Min(0.05f)] private float pushDuration = 0.35f;
    [Tooltip("Vitesse horizontale au-delà de laquelle le joueur est considéré en mouvement.")]
    [SerializeField, Min(0f)] private float moveSpeedThreshold = 0.3f;

    // =====================================================================
    // VFX
    // =====================================================================

    [Header("VFX - Boucles")]
    [SerializeField] private LoopVfx walkVfx = new LoopVfx();
    [SerializeField] private LoopVfx panicVfx = new LoopVfx();
    [Tooltip("Un porteur de bombe à moins de cette distance déclenche les panic particles chez un joueur sans bombe.")]
    [SerializeField, Min(0f)] private float dangerRadius = 3f;

    [Header("VFX - Bombe (objet optionnel)")]
    [SerializeField] private GameObject bombActiveVFX;

    [Header("VFX - Explosion")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private VfxSpace explosionSpace = VfxSpace.PlayerRoot;
    [SerializeField] private Vector3 explosionLocalPosition;
    [SerializeField] private Vector3 explosionLocalEulerAngles;
    [SerializeField, Min(0.1f)] private float explosionLifetime = 5f;

    // =====================================================================
    // Audio
    // =====================================================================

    [Header("Audio")]
    [Tooltip("AudioSource modèle : sert à copier Mixer Group, Spatial Blend (mettre 0 = 2D) et Priority.")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Sfx walkSfx = new Sfx { cooldown = 0.12f };
    [SerializeField] private Sfx panicSfx = new Sfx { cooldown = 0.3f };
    [SerializeField] private Sfx tagSfx = new Sfx { cooldown = 0.2f };
    [SerializeField] private Sfx clickSfx = new Sfx { cooldown = 0.05f };
    [SerializeField] private Sfx explosionSfx = new Sfx { cooldown = 0f, interruptPrevious = false };

    [Header("Audio - Cadences")]
    [Tooltip("Intervalle entre deux pas à vitesse faible.")]
    [SerializeField, Min(0.05f)] private float stepIntervalSlow = 0.45f;
    [Tooltip("Intervalle entre deux pas à vitesse max.")]
    [SerializeField, Min(0.05f)] private float stepIntervalFast = 0.25f;
    [Tooltip("Intervalle du Panic SFX pendant un déplacement en panique.")]
    [SerializeField, Min(0.05f)] private float panicSfxInterval = 0.6f;
    [Tooltip("Intervalle du clic de bombe, calé sur le timer du personnage.")]
    [SerializeField, Min(0.05f)] private float clickInterval = 1f;

    // =====================================================================
    // UI
    // =====================================================================

    [Header("Bomb Timer UI")]
    [SerializeField] private TMP_Text bombTimerText;

    // =====================================================================
    // État interne
    // =====================================================================

    private HotPotatoCharacter _owner;

    private float _leanAngle;
    private float _leanVelocity;
    private Vector3 _leanBasePosition;
    private float _bobPhase;
    private float _bobBlend;

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
        if (leanRoot != null) _leanBasePosition = leanRoot.localPosition;

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
        Lean(moveInput, speed01, deltaTime);

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
    // Visuel : orientation, lean, balancier
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

    private void Lean(Vector2 moveInput, float speed01, float deltaTime)
    {
        if (leanRoot == null) return;

        // Pas de ressort stable même si le framerate chute.
        float dt = Mathf.Min(deltaTime, 1f / 30f);

        // --- Inclinaison vers l'avant (le Lean est enfant de l'Orientation : "avant" = direction de déplacement)
        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);
        float lean01 = Mathf.Clamp01(Mathf.Max(inputMagnitude, speed01));
        float targetLean = lean01 * maxLeanAngle;

        float springAcceleration = leanStiffness * (targetLean - _leanAngle) - leanDamping * _leanVelocity;
        _leanVelocity += springAcceleration * dt;
        _leanVelocity = Mathf.Clamp(_leanVelocity, -maxLeanVelocity, maxLeanVelocity);
        _leanAngle += _leanVelocity * dt;

        // --- Balancier léger, proportionnel à la vitesse
        float bobTarget = _isMoving ? speed01 : 0f;
        _bobBlend = Mathf.MoveTowards(_bobBlend, bobTarget, bobBlendSpeed * dt);

        const float Tau = Mathf.PI * 2f;
        _bobPhase = (_bobPhase + Tau * bobFrequency * speed01 * dt) % Tau;

        float roll = Mathf.Sin(_bobPhase) * bobRollAngle * _bobBlend;
        float pitchBob = Mathf.Cos(_bobPhase * 2f) * bobPitchAngle * _bobBlend;
        float vertical = Mathf.Abs(Mathf.Sin(_bobPhase)) * bobHeight * _bobBlend;

        leanRoot.localRotation = Quaternion.Euler(_leanAngle + pitchBob, 0f, roll);
        leanRoot.localPosition = _leanBasePosition + Vector3.up * vertical;
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