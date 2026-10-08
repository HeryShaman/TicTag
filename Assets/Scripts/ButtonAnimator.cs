using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// À ajouter sur un bouton UI : échelle au survol / sélection / clic + "coup de volant"
/// (la rotation dépasse sa cible puis se stabilise grâce à un ressort peu amorti).
/// Fonctionne aussi quand Time.timeScale = 0 (menu pause).
/// </summary>
public class ButtonAnimator : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler,
    ISelectHandler, IDeselectHandler
{
    [Header("Échelle")]
    [SerializeField] private float hoverScale = 1.1f;
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField, Min(1f)] private float scaleSpeed = 18f;

    [Header("Coup de volant (rotation en degrés, axe Z)")]
    [SerializeField] private float hoverAngle = 6f;      // positif = sens anti-horaire
    [SerializeField] private float pressedAngle = -9f;   // négatif = sens horaire
    [SerializeField, Min(1f)] private float stiffness = 320f;
    [SerializeField, Min(0f)] private float damping = 14f;   // plus bas = plus de rebond

    private Vector3 _baseScale = Vector3.one;
    private Quaternion _baseRotation = Quaternion.identity;

    private float _angle;
    private float _angleVelocity;

    private bool _hover;
    private bool _selected;
    private bool _pressed;

    private void Awake()
    {
        _baseScale = transform.localScale;
        _baseRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        _hover = false;
        _selected = false;
        _pressed = false;
        _angle = 0f;
        _angleVelocity = 0f;

        transform.localScale = _baseScale;
        transform.localRotation = _baseRotation;
    }

    private void Update()
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);

        // --- Échelle (lissage indépendant du framerate)
        float scaleTarget = _pressed ? pressedScale : (_hover || _selected ? hoverScale : 1f);
        float k = 1f - Mathf.Exp(-scaleSpeed * dt);
        transform.localScale = Vector3.Lerp(transform.localScale, _baseScale * scaleTarget, k);

        // --- Rotation : ressort sous-amorti = la rotation dépasse la cible puis revient (coup de volant)
        float angleTarget = _pressed ? pressedAngle : (_hover || _selected ? hoverAngle : 0f);
        float acceleration = stiffness * (angleTarget - _angle) - damping * _angleVelocity;
        _angleVelocity += acceleration * dt;
        _angle += _angleVelocity * dt;

        transform.localRotation = _baseRotation * Quaternion.Euler(0f, 0f, _angle);
    }

    public void OnPointerEnter(PointerEventData eventData) { _hover = true; }
    public void OnPointerExit(PointerEventData eventData) { _hover = false; _pressed = false; }
    public void OnPointerDown(PointerEventData eventData) { _pressed = true; }
    public void OnPointerUp(PointerEventData eventData) { _pressed = false; }
    public void OnSelect(BaseEventData eventData) { _selected = true; }
    public void OnDeselect(BaseEventData eventData) { _selected = false; }
}