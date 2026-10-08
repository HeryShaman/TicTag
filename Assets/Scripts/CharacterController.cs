using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HotPotatoCharacter : MonoBehaviour
{
    public enum MovementState { NoBomb, BombNormal, BombPanic }

    [Serializable]
    public class MovementProfile
    {
        [Min(0f)] public float speed = 6f;
        [Min(0f)] public float acceleration = 50f;
        [Min(0f)] public float friction = 45f;

        public MovementProfile() { }
        public MovementProfile(float speed, float acceleration, float friction)
        {
            this.speed = speed;
            this.acceleration = acceleration;
            this.friction = friction;
        }
    }

    [Header("Player")]
    [Tooltip("Le playerIndex est porté par l'InputReader (même GameObject).")]
    [SerializeField] private InputReader inputReader;

    [Header("Movement")]
    [SerializeField] private MovementProfile NormalSpeed = new MovementProfile(6f, 50f, 45f);
    [SerializeField] private MovementProfile BombSpeed = new MovementProfile(7.5f, 45f, 30f);
    [SerializeField] private MovementProfile PanicSpeed = new MovementProfile(9.5f, 25f, 14f);

    [Header("Gravity / Ground")]
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private float groundCheckDistance = 0.1f;

    [Header("Bomb")]
    [SerializeField] private bool startWithBomb = false;
    [SerializeField] private float bombDuration = 10f;
    [SerializeField] private float transferCooldown = 0.4f;
    [SerializeField, Range(0.05f, 0.95f)] private float panicThreshold = 0.5f;

    [Header("Bomb Reduce")]
    [SerializeField, Min(0f)] private float durationPenaltyPerTag = 1f;
    [SerializeField, Min(0.5f)] private float minBombDuration = 3f;

    [Header("Speed Boost")]
    [SerializeField, Min(0f)] private float receiveBoostDuration = 0.5f;
    [SerializeField, Min(1f)] private float receiveBoostSpeedMultiplier = 1.35f;
    [SerializeField, Min(1f)] private float receiveBoostAccelerationMultiplier = 1.5f;

    [Header("Knockback")]
    [SerializeField, Min(0f)] private float knockbackSpeed = 14f;
    [SerializeField, Min(0f)] private float knockbackControlLockTime = 0.25f;
    [SerializeField, Range(0f, 1f)] private float controlDuringKnockback = 0.15f;

    [Header("Death")]
    [SerializeField, Min(0.05f)] private float destroyDelayAfterDeath = 1.5f;

    [Header("Feedback")]
    [SerializeField] private PlayerFeedbackController feedback;

    // ---------- Registre statique (utilisé par le feedback pour détecter le porteur de bombe proche) ----------
    private static readonly List<HotPotatoCharacter> _all = new List<HotPotatoCharacter>(8);
    public static IReadOnlyList<HotPotatoCharacter> AllCharacters => _all;

    // ---------- État public ----------
    public bool IsAlive { get; private set; } = true;
    public bool HasBomb { get; private set; }
    public bool IsPanic { get; private set; }
    public bool IsGrounded { get; private set; }
    public float BombTime { get; private set; }
    public float BombDuration => _currentBombDuration;
    public float InitialBombDuration => bombDuration;
    public int PlayerIndex => inputReader != null ? inputReader.PlayerIndex : -1;
    public MovementState State => !HasBomb ? MovementState.NoBomb
                                : IsPanic ? MovementState.BombPanic
                                : MovementState.BombNormal;

    /// <summary>Déclenché une fois à l'entrée en panique (utile pour caméra, UI...). Le feedback lit IsPanic directement.</summary>
    public event Action<HotPotatoCharacter> PanicStarted;

    // ---------- Interne ----------
    private Rigidbody _rb;
    private Collider _col;
    private readonly Collider[] _groundHits = new Collider[8];

    private Vector2 _moveInput;
    private Vector3 _horizontalVelocity;   // vitesse XZ résultante (pour le feedback)
    private Vector3 _commandedVelocity;    // vitesse XZ envoyée au Rigidbody au dernier FixedUpdate
    private float _verticalVelocity;

    private float _lastTransferTime = -999f;
    private float _currentBombDuration;    // durée de départ de la bombe actuelle (diminue à chaque touche)
    private float _boostTimer;
    private float _knockbackTimer;
    private bool _hasPendingKnockback;
    private Vector3 _pendingKnockback;

    private MovementProfile CurrentProfile =>
        !HasBomb ? NormalSpeed : (IsPanic ? PanicSpeed : BombSpeed);

    private float CurrentMaxSpeed =>
        CurrentProfile.speed * (_boostTimer > 0f ? receiveBoostSpeedMultiplier : 1f);

    // =====================================================================
    // Cycle de vie
    // =====================================================================

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
        if (_col == null) _col = GetComponentInChildren<Collider>();

        _currentBombDuration = bombDuration;

        // Gravité gérée par le script (vitesse de chute plafonnée, Y = 0 au sol).
        _rb.useGravity = false;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
        _rb.linearDamping = 0f;                       // aucune friction cachée
        _rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (inputReader == null)
            inputReader = GetComponentInParent<InputReader>();

        if (inputReader == null)
            Debug.LogError($"[{name}] Aucun InputReader trouvé : ajoute-en un sur ce GameObject.", this);

        if (feedback == null)
            feedback = GetComponentInChildren<PlayerFeedbackController>();

        if (groundMask.value == 0)
            Debug.LogWarning($"[{name}] groundMask est vide : le personnage tombera indéfiniment.", this);
    }

    private void OnEnable()
    {
        if (!_all.Contains(this)) _all.Add(this);
    }

    private void OnDisable()
    {
        _all.Remove(this);
    }

    private void Start()
    {
        if (startWithBomb)
            ReceiveBomb(false);
    }

    private void Update()
    {
        if (!IsAlive) return;

        bool gameOver = GameManager.Instance != null &&
                        GameManager.Instance.CurrentState == GameManager.GameState.GameOver;

        _moveInput = (!gameOver && inputReader != null)
            ? inputReader.GetMove()
            : Vector2.zero;

        if (!gameOver && HasBomb)
        {
            TickBomb(Time.deltaTime);
            if (!IsAlive) return;   // a explosé pendant le tick
        }

        if (feedback != null)
        {
            feedback.UpdateFeedback(
                _moveInput,
                _horizontalVelocity,
                CurrentMaxSpeed,
                HasBomb,
                IsPanic,
                IsGrounded,
                BombTime,
                _currentBombDuration,
                Time.deltaTime
            );
        }
    }

    private void FixedUpdate()
    {
        if (!IsAlive)
        {
            _rb.linearVelocity = Vector3.zero;
            return;
        }

        float dt = Time.fixedDeltaTime;

        // On repart de la vitesse réelle : les collisions (murs, joueurs) sont ainsi respectées
        // et le personnage ne "pousse" pas contre un mur avec une vitesse fantôme.
        Vector3 rbVel = _rb.linearVelocity;
        Vector3 velocity = new Vector3(rbVel.x, 0f, rbVel.z);

        if (_hasPendingKnockback)
        {
            velocity = _pendingKnockback;
            _knockbackTimer = knockbackControlLockTime;
            _hasPendingKnockback = false;
        }

        if (_boostTimer > 0f) _boostTimer -= dt;
        if (_knockbackTimer > 0f) _knockbackTimer -= dt;

        velocity = ApplyHorizontalMovement(velocity, dt);
        _verticalVelocity = ComputeVerticalVelocity(dt);

        _commandedVelocity = velocity;
        _horizontalVelocity = velocity;
        _rb.linearVelocity = new Vector3(velocity.x, _verticalVelocity, velocity.z);
    }

    // =====================================================================
    // Mouvement
    // =====================================================================

    private Vector3 ApplyHorizontalMovement(Vector3 velocity, float dt)
    {
        MovementProfile p = CurrentProfile;
        bool boosted = _boostTimer > 0f;

        float maxSpeed = p.speed * (boosted ? receiveBoostSpeedMultiplier : 1f);
        float accel = p.acceleration * (boosted ? receiveBoostAccelerationMultiplier : 1f);
        if (_knockbackTimer > 0f) accel *= controlDuringKnockback;

        Vector3 input = Vector3.ClampMagnitude(new Vector3(_moveInput.x, 0f, _moveInput.y), 1f);

        if (input.sqrMagnitude > 0.0001f)
        {
            // Input tenu : accélération vers la vitesse cible (gère aussi les virages).
            velocity = Vector3.MoveTowards(velocity, input * maxSpeed, accel * dt);
        }
        else
        {
            // Input relâché : la friction freine jusqu'à l'arrêt.
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, p.friction * dt);
        }

        return velocity;
    }

    private float ComputeVerticalVelocity(float dt)
    {
        IsGrounded = CheckGrounded();

        // Au sol : Y reste strictement à 0, aucune accumulation.
        if (IsGrounded && _verticalVelocity <= 0f)
            return 0f;

        // En l'air : gravité plafonnée.
        float v = _verticalVelocity + Physics.gravity.y * gravityScale * dt;
        return Mathf.Max(v, -maxFallSpeed);
    }

    private bool CheckGrounded()
    {
        if (_col == null) return false;

        Bounds b = _col.bounds;
        Vector3 origin = new Vector3(
            b.center.x,
            b.min.y + groundCheckRadius - groundCheckDistance,
            b.center.z);

        int count = Physics.OverlapSphereNonAlloc(
            origin, groundCheckRadius, _groundHits, groundMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = _groundHits[i];
            if (hit == null) continue;
            if (hit.attachedRigidbody == _rb) continue;   // ignore soi-même
            return true;
        }
        return false;
    }

    // =====================================================================
    // Bombe
    // =====================================================================

    private void TickBomb(float dt)
    {
        BombTime -= dt;

        if (!IsPanic && BombTime <= _currentBombDuration * panicThreshold)
            EnterPanic();

        if (BombTime <= 0f)
            Explode();
    }

    private void EnterPanic()
    {
        IsPanic = true;
        PanicStarted?.Invoke(this);
    }

    /// <param name="grantBoost">false pour la bombe initiale, true pour un transfert ou une attribution.</param>
    /// <param name="duration">Durée de départ du timer. Valeur &lt;= 0 : durée initiale (nouvelle bombe).</param>
    public void ReceiveBomb(bool grantBoost = true, float duration = -1f)
    {
        if (!IsAlive) return;

        _currentBombDuration = duration > 0f ? duration : bombDuration;

        HasBomb = true;
        IsPanic = false;
        BombTime = _currentBombDuration;

        // Empêche de redonner instantanément la bombe à celui qui vient de la donner.
        _lastTransferTime = Time.time;

        if (grantBoost)
            _boostTimer = receiveBoostDuration;

        if (feedback != null)
            feedback.OnBombReceived();
    }

    /// <summary>Impose une vitesse horizontale (direction normalisée en XZ) et réduit le contrôle brièvement.</summary>
    public void ApplyKnockback(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;

        _pendingKnockback = direction.normalized * knockbackSpeed;
        _hasPendingKnockback = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryTransferBomb(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryTransferBomb(other.gameObject);
    }

    private void TryTransferBomb(GameObject other)
    {
        if (!IsAlive || !HasBomb) return;
        if (Time.time < _lastTransferTime + transferCooldown) return;
        if (other == null) return;

        HotPotatoCharacter target = other.GetComponentInParent<HotPotatoCharacter>();
        if (target == null || target == this || !target.IsAlive) return;

        // Direction calculée AVANT de perdre la bombe.
        Vector3 knockDir = GetHeadingDirection(target.transform.position);

        // Le timer se réinitialise chez le receveur, mais 1 s plus court à chaque touche (plancher minBombDuration).
        float nextDuration = Mathf.Min(
            _currentBombDuration,
            Mathf.Max(minBombDuration, _currentBombDuration - durationPenaltyPerTag));

        HasBomb = false;
        IsPanic = false;
        _boostTimer = 0f;
        _lastTransferTime = Time.time;

        target.ReceiveBomb(true, nextDuration);   // boost de vitesse + timer réinitialisé et réduit
        target.ApplyKnockback(knockDir);

        if (feedback != null)
            feedback.OnBombTransferred();   // animation push + Tag SFX
    }

    /// <summary>Direction vers laquelle ce joueur se dirigeait au moment du contact.</summary>
    private Vector3 GetHeadingDirection(Vector3 fallbackTargetPosition)
    {
        Vector3 v = _commandedVelocity; v.y = 0f;
        if (v.sqrMagnitude > 0.25f)
            return v.normalized;

        Vector3 input = new Vector3(_moveInput.x, 0f, _moveInput.y);
        if (input.sqrMagnitude > 0.01f)
            return input.normalized;

        Vector3 toTarget = fallbackTargetPosition - transform.position; toTarget.y = 0f;
        return toTarget.sqrMagnitude > 0.0001f ? toTarget.normalized : Vector3.forward;
    }

    private void Explode()
    {
        if (!IsAlive) return;

        bool hadBomb = HasBomb;

        IsAlive = false;
        HasBomb = false;
        IsPanic = false;

        if (GameManager.Instance != null)
            GameManager.Instance.OnPlayerExploded(this, hadBomb);

        if (feedback != null)
            feedback.OnExplosion();

        foreach (Collider col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        _rb.linearVelocity = Vector3.zero;

        // Laisse le temps à l'animation de mort d'être visible.
        Destroy(gameObject, destroyDelayAfterDeath);
    }

    public void MoveOnConveyor(Vector3 direction) {
        transform.position += direction;
    }

    // =====================================================================
    // Debug
    // =====================================================================

    private void OnDrawGizmosSelected()
    {
        Collider c = _col != null ? _col : GetComponent<Collider>();
        if (c == null) return;

        Bounds b = c.bounds;
        Vector3 origin = new Vector3(b.center.x, b.min.y + groundCheckRadius - groundCheckDistance, b.center.z);
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, groundCheckRadius);
    }
}