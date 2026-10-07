using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraBehaviour : MonoBehaviour
{
    public enum CameraState
    {
        Idle,
        Intro,
        Play,
        Outro
    }

    public CameraState CurrentState { get; private set; } = CameraState.Idle;

    [Header("Camera")]
    [SerializeField] private Camera cam;

    [Header("Intro")]
    [SerializeField] private float introStartSize = 5f;
    [SerializeField] private float introEndSize = 10f;
    [SerializeField] private float introDuration = 2f;
    [SerializeField] private Vector3 introPosition = new Vector3(0f, 20f, 0f);

    [Header("Outro")]
    [SerializeField] private float outroSize = 4f;
    [SerializeField] private float outroDuration = 1.5f;
    [SerializeField] private Vector3 offsetFromLastPlayer = new Vector3(0f, 20f, -10f);

    private float _timer;
    private float _outroStartSize;
    private Vector3 _outroStartPosition;
    private Transform _outroTarget;

    private void Awake()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
        }

        cam.orthographic = true;
    }

    public void StartIntro()
    {
        CurrentState = CameraState.Intro;

        _timer = 0f;

        cam.orthographicSize = introStartSize;
        transform.position = introPosition;
    }

    public void StartOutro(Transform lastPlayer)
    {
        CurrentState = CameraState.Outro;

        _timer = 0f;
        _outroTarget = lastPlayer;
        _outroStartSize = cam.orthographicSize;
        _outroStartPosition = transform.position;
    }

    private void Update()
    {
        if (CurrentState == CameraState.Intro)
        {
            UpdateIntro();
        }
        else if (CurrentState == CameraState.Outro)
        {
            UpdateOutro();
        }
    }

    private void UpdateIntro()
    {
        _timer += Time.deltaTime;

        float t = Mathf.Clamp01(_timer / Mathf.Max(0.001f, introDuration));
        float smooth = Mathf.SmoothStep(0f, 1f, t);

        cam.orthographicSize = Mathf.Lerp(introStartSize, introEndSize, smooth);

        if (t >= 1f)
        {
            CurrentState = CameraState.Play;
        }
    }

    private void UpdateOutro()
    {
        _timer += Time.deltaTime;

        float t = Mathf.Clamp01(_timer / Mathf.Max(0.001f, outroDuration));
        float smooth = Mathf.SmoothStep(0f, 1f, t);

        if (_outroTarget != null)
        {
            Vector3 targetPosition = _outroTarget.position + offsetFromLastPlayer;

            transform.position = Vector3.Lerp(
                _outroStartPosition,
                targetPosition,
                smooth
            );
        }

        cam.orthographicSize = Mathf.Lerp(
            _outroStartSize,
            outroSize,
            smooth
        );
    }
}