using UnityEngine;
using TMPro;

public class PlayerFeedbackController : MonoBehaviour
{
    [Header("Orientation 3D")]
    [SerializeField] private Transform orientationRoot;
    [SerializeField] private Transform leanRoot;

    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float minMoveMagnitude = 0.05f;

    [Header("Lean / Spring")]
    [SerializeField] private float maxLeanAngle = 18f;
    [SerializeField] private float leanStiffness = 140f;
    [SerializeField] private float leanDamping = 20f;
    [SerializeField] private float maxLeanVelocity = 500f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private string bombParameter = "HasBomb";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip bombReceivedClip;
    [SerializeField] private AudioClip bombPassedClip;
    [SerializeField] private AudioClip explosionClip;

    [Header("VFX")]
    [SerializeField] private GameObject bombActiveVFX;
    [SerializeField] private GameObject explosionVFX;

    [Header("Bomb Timer UI")]
    [SerializeField] private TMP_Text bombTimerText;

    private float _leanAngle;
    private float _leanVelocity;

    private int _speedHash;
    private int _bombHash;
    private bool _hasSpeedParameter;
    private bool _hasBombParameter;

    private int _lastDisplayedBombTimer = -1;

    private void Awake()
    {
        if (orientationRoot == null)
        {
            orientationRoot = transform;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        string speedName = string.IsNullOrEmpty(speedParameter) ? "Speed" : speedParameter;
        string bombName = string.IsNullOrEmpty(bombParameter) ? "HasBomb" : bombParameter;

        _speedHash = Animator.StringToHash(speedName);
        _bombHash = Animator.StringToHash(bombName);

        CacheAnimatorParameters(speedName, bombName);
    }

    private void CacheAnimatorParameters(string speedName, string bombName)
    {
        if (animator == null)
        {
            return;
        }

        _hasSpeedParameter = false;
        _hasBombParameter = false;

        for (int i = 0; i < animator.parameterCount; i++)
        {
            AnimatorControllerParameter parameter = animator.GetParameter(i);

            if (parameter.name == speedName)
            {
                _hasSpeedParameter = true;
            }

            if (parameter.name == bombName)
            {
                _hasBombParameter = true;
            }
        }
    }

    public void UpdateFeedback(
        Vector2 moveInput,
        Vector3 velocity,
        float maxSpeed,
        bool hasBomb,
        float bombTime,
        float bombDuration,
        float deltaTime
    )
    {
        Orient(moveInput, velocity, deltaTime);
        Lean(moveInput, velocity, maxSpeed, deltaTime);
        UpdateAnimator(velocity, maxSpeed, hasBomb);

        if (bombActiveVFX != null)
        {
            bombActiveVFX.SetActive(hasBomb);
        }

        UpdateBombTimerUI(hasBomb, bombTime);
    }

    private void Orient(Vector2 moveInput, Vector3 velocity, float deltaTime)
    {
        if (orientationRoot == null)
        {
            return;
        }

        Vector3 direction = velocity;

        if (direction.sqrMagnitude < minMoveMagnitude * minMoveMagnitude)
        {
            direction = new Vector3(moveInput.x, 0f, moveInput.y);
        }

        direction.y = 0f;

        if (direction.sqrMagnitude < minMoveMagnitude * minMoveMagnitude)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        orientationRoot.rotation = Quaternion.RotateTowards(
            orientationRoot.rotation,
            targetRotation,
            rotationSpeed * deltaTime
        );
    }

    private void Lean(Vector2 moveInput, Vector3 velocity, float maxSpeed, float deltaTime)
    {
        if (leanRoot == null)
        {
            return;
        }

        float inputMagnitude = Mathf.Clamp01(moveInput.magnitude);

        float velocityMagnitude = maxSpeed <= 0f
            ? 0f
            : Mathf.Clamp01(velocity.magnitude / maxSpeed);

        float speed01 = Mathf.Clamp01(Mathf.Max(inputMagnitude, velocityMagnitude));

        float targetLean = speed01 * maxLeanAngle;

        float springAcceleration = leanStiffness * (targetLean - _leanAngle) - leanDamping * _leanVelocity;

        _leanVelocity += springAcceleration * deltaTime;
        _leanVelocity = Mathf.Clamp(_leanVelocity, -maxLeanVelocity, maxLeanVelocity);

        _leanAngle += _leanVelocity * deltaTime;

        leanRoot.localRotation = Quaternion.Euler(_leanAngle, 0f, 0f);
    }

    private void UpdateAnimator(Vector3 velocity, float maxSpeed, bool hasBomb)
    {
        if (animator == null)
        {
            return;
        }

        float speed01 = maxSpeed <= 0f
            ? 0f
            : Mathf.Clamp01(velocity.magnitude / maxSpeed);

        if (_hasSpeedParameter)
        {
            animator.SetFloat(_speedHash, speed01);
        }

        if (_hasBombParameter)
        {
            animator.SetBool(_bombHash, hasBomb);
        }
    }

    private void UpdateBombTimerUI(bool hasBomb, float bombTime)
    {
        if (bombTimerText == null)
        {
            return;
        }

        if (!hasBomb)
        {
            if (_lastDisplayedBombTimer != -1)
            {
                bombTimerText.text = string.Empty;
                _lastDisplayedBombTimer = -1;
            }

            return;
        }

        int displayedValue = Mathf.CeilToInt(Mathf.Max(0f, bombTime));

        if (displayedValue != _lastDisplayedBombTimer)
        {
            bombTimerText.text = displayedValue.ToString();
            _lastDisplayedBombTimer = displayedValue;
        }
    }

    public void OnBombReceived()
    {
        if (audioSource != null && bombReceivedClip != null)
        {
            audioSource.PlayOneShot(bombReceivedClip);
        }

        if (bombActiveVFX != null)
        {
            bombActiveVFX.SetActive(true);
        }
    }

    public void OnBombTransferred()
    {
        if (audioSource != null && bombPassedClip != null)
        {
            audioSource.PlayOneShot(bombPassedClip);
        }

        if (bombActiveVFX != null)
        {
            bombActiveVFX.SetActive(false);
        }
    }

    public void OnExplosion()
    {
        if (audioSource != null && explosionClip != null)
        {
            audioSource.PlayOneShot(explosionClip);
        }

        if (explosionVFX != null)
        {
            Instantiate(explosionVFX, transform.position, Quaternion.identity);
        }

        if (bombActiveVFX != null)
        {
            bombActiveVFX.SetActive(false);
        }
    }
}