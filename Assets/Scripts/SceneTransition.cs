using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class SceneTransition : MonoBehaviour
{
    [System.Serializable]
    public struct Step
    {
        public float scale;
        public float duration;

        public Step(float scale, float duration)
        {
            this.scale = scale;
            this.duration = duration;
        }
    }

    // Échelle 1 = le rond couvre tout l'écran (diamètre = diagonale).
    [SerializeField] private Step[] closeSteps =
    {
        new Step(0.5f, 0.15f),
        new Step(0.2f, 0.1f),
        new Step(0.5f, 0.15f),
        new Step(0.2f, 0.1f),
        new Step(1.0f, 0.3f)
    };
    [SerializeField, Min(0.05f)] private float openDuration = 0.5f;
    [SerializeField, Min(0f)] private float minBlackTime = 0.2f;
    [SerializeField] private bool playIntroReveal = true;
    [SerializeField] private int sortingOrder = 1000;

    private static SceneTransition _instance;

    private Canvas _canvas;
    private RectTransform _circleRect;
    private Image _image;
    private float _scale;
    private bool _busy;

    public static bool IsBusy => _instance != null && _instance._busy;

    // =====================================================================
    // API
    // =====================================================================

    public static void LoadScene(string sceneName)
    {
        Ensure();
        _instance.Begin(sceneName, -1);
    }

    public static void LoadScene(int buildIndex)
    {
        Ensure();
        _instance.Begin(null, buildIndex);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        Ensure();
    }

    private static void Ensure()
    {
        if (_instance != null) return;

        GameObject go = new GameObject("SceneTransition");
        go.AddComponent<SceneTransition>();
    }

    // =====================================================================
    // Cycle de vie
    // =====================================================================

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        transform.SetParent(null, false);
        DontDestroyOnLoad(gameObject);

        BuildUI();
        SetScale(0f);
        ShowCanvas(false);
    }

    private void Start()
    {
        if (_instance == this && playIntroReveal)
        {
            _busy = true;
            StartCoroutine(IntroRoutine());
        }
    }

    // =====================================================================
    // Séquences
    // =====================================================================

    private void Begin(string sceneName, int buildIndex)
    {
        if (_busy) return;

        _busy = true;
        StartCoroutine(TransitionRoutine(sceneName, buildIndex));
    }

    // Ouverture au lancement du jeu : écran noir -> rond 1 -> 0.
    private IEnumerator IntroRoutine()
    {
        try
        {
            ShowCanvas(true);
            UpdateCircleSize();
            SetScale(1f);

            yield return null;   // laisse la scène s'initialiser sous le noir
            yield return Animate(1f, 0f, openDuration);
        }
        finally
        {
            ResetToOpen();
        }
    }

    private IEnumerator TransitionRoutine(string sceneName, int buildIndex)
    {
        try
        {
            // --- Entrée dans le chargement : fermeture
            ShowCanvas(true);
            UpdateCircleSize();
            SetScale(0f);

            for (int i = 0; i < closeSteps.Length; i++)
            {
                yield return Animate(_scale, closeSteps[i].scale, closeSteps[i].duration);
            }

            SetScale(1f);   // garantit un écran entièrement noir

            // --- Chargement pendant le noir
            AsyncOperation op = buildIndex >= 0
                ? SceneManager.LoadSceneAsync(buildIndex)
                : SceneManager.LoadSceneAsync(sceneName);

            if (op != null)
            {
                while (!op.isDone) yield return null;
            }

            yield return null;   // Awake / Start / spawns de la nouvelle scène
            yield return null;   // 2e frame : absorbe le pic de charge avant de commencer l'ouverture

            float wait = 0f;
            while (wait < minBlackTime)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            // --- Sortie du chargement : ouverture 1 -> 0
            yield return Animate(1f, 0f, openDuration);
        }
        finally
        {
            // Quoi qu'il arrive (scène introuvable, interruption...), le rond ne reste jamais fermé.
            ResetToOpen();
        }
    }

    private IEnumerator Animate(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            SetScale(to);
            yield break;
        }

        float t = 0f;
        while (t < 1f)
        {
            // Pas plafonné : un gros hitch juste après le chargement ne fait plus "sauter" l'animation.
            t += Mathf.Min(Time.unscaledDeltaTime, 0.033f) / duration;
            SetScale(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t))));
            yield return null;
        }

        SetScale(to);
    }

    private void ResetToOpen()
    {
        SetScale(0f);
        ShowCanvas(false);
        _busy = false;
    }

    // =====================================================================
    // UI générée par script
    // =====================================================================

    private void SetScale(float s)
    {
        _scale = s;
        if (_circleRect == null) return;

        _circleRect.localScale = Vector3.one * s;
        _image.enabled = s > 0.001f;   // à 0 le rond n'est plus rendu du tout
    }

    private void ShowCanvas(bool value)
    {
        if (_canvas != null) _canvas.enabled = value;
    }

    // Diamètre = diagonale de l'écran : à l'échelle 1 le rond couvre les coins.
    private void UpdateCircleSize()
    {
        float diameter = new Vector2(Screen.width, Screen.height).magnitude * 1.05f;
        _circleRect.sizeDelta = new Vector2(diameter, diameter);
    }

    private void BuildUI()
    {
        // Pas de CanvasScaler : 1 unité de canvas = 1 pixel, la taille du rond est donc exacte.
        GameObject canvasGo = new GameObject("TransitionCanvas", typeof(Canvas), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        _canvas = canvasGo.GetComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = sortingOrder;

        // Bloqueur invisible plein écran : empêche de cliquer dans l'UI pendant la transition.
        GameObject blockerGo = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
        blockerGo.transform.SetParent(canvasGo.transform, false);
        RectTransform blockerRect = (RectTransform)blockerGo.transform;
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        Image blocker = blockerGo.GetComponent<Image>();
        blocker.color = new Color(0f, 0f, 0f, 0f);
        blocker.raycastTarget = true;

        GameObject circleGo = new GameObject("BlackCircle", typeof(RectTransform), typeof(Image));
        circleGo.transform.SetParent(canvasGo.transform, false);
        _circleRect = (RectTransform)circleGo.transform;
        _circleRect.anchorMin = new Vector2(0.5f, 0.5f);
        _circleRect.anchorMax = new Vector2(0.5f, 0.5f);
        _circleRect.pivot = new Vector2(0.5f, 0.5f);
        _circleRect.anchoredPosition = Vector2.zero;

        _image = circleGo.GetComponent<Image>();
        _image.sprite = CreateCircleSprite();
        _image.color = Color.black;
        _image.raycastTarget = false;
    }

    private static Sprite CreateCircleSprite()
    {
        const int size = 256;

        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float radius = size * 0.5f;
        Color32[] pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - radius;
                float dy = y + 0.5f - radius;
                float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));   // bord anti-aliasé de 1 px
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}