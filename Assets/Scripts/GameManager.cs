using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        Setup,
        PreStart,
        Playing,
        WaitingForNextBomb,
        GameOver
    }

    [Header("References")]
    [SerializeField] private CameraBehaviour gameCamera;
    [SerializeField] private List<HotPotatoCharacter> players = new List<HotPotatoCharacter>();
    [SerializeField] private TMP_Text gameTimerText;

    [Header("Timing")]
    [SerializeField] private float preStartDuration = 2f;
    [SerializeField] private float nextBombDelay = 60f;
    [SerializeField] private float hitPauseDuration = 0.25f;

    [Header("Start rules")]
    [SerializeField] private bool assignFirstBombImmediately = true;

    public GameState CurrentState { get; private set; } = GameState.Setup;

    private float _preStartTimer;
    private float _nextBombTimer;

    private Coroutine _hitPauseRoutine;
    private bool _isHitPausing = false;
    private float _timeScaleBeforeHitPause = 1f;

    private void Awake()
    {
        Instance = this;

        if (players == null)
        {
            players = new List<HotPotatoCharacter>();
        }

        if (players.Count == 0)
        {
            players.AddRange(Object.FindObjectsOfType<HotPotatoCharacter>());
        }
    }

    private void Start()
    {
        StartPreStart();
    }

    private void OnDisable()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
        }
    }

    public void RegisterPlayer(HotPotatoCharacter player)
    {
        if (player == null)
        {
            return;
        }

        if (!players.Contains(player))
        {
            players.Add(player);
        }
    }

    private void StartPreStart()
    {
        CurrentState = GameState.PreStart;

        _preStartTimer = preStartDuration;

        if (gameCamera != null)
        {
            gameCamera.StartIntro();
        }

        // Affiche 1:00 au lieu de 60 secondes.
        SetGlobalTimerText(nextBombDelay);
    }

    private void Update()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        // Sécurité : si jamais il ne reste plus qu'un joueur vivant ou moins, on termine la partie.
        if (CurrentState != GameState.PreStart)
        {
            if (GetAlivePlayers().Count <= 1)
            {
                EndGame();
                return;
            }
        }

        switch (CurrentState)
        {
            case GameState.PreStart:
                UpdatePreStart();
                break;

            case GameState.WaitingForNextBomb:
                UpdateWaitingForNextBomb();
                break;

            case GameState.Playing:
                // Sécurité si jamais aucune bombe n'est active alors que le jeu est censé jouer.
                if (!AnyAlivePlayerHasBomb())
                {
                    StartNextBombTimer();
                }
                break;
        }
    }

    private void UpdatePreStart()
    {
        _preStartTimer -= Time.deltaTime;

        if (_preStartTimer <= 0f)
        {
            StartPlaying();
        }
    }

    private void UpdateWaitingForNextBomb()
    {
        _nextBombTimer -= Time.deltaTime;

        SetGlobalTimerText(_nextBombTimer);

        if (_nextBombTimer <= 0f)
        {
            AssignBombToRandomAlivePlayer();
        }
    }

    private void StartPlaying()
    {
        if (assignFirstBombImmediately)
        {
            CurrentState = GameState.Playing;
            AssignBombToRandomAlivePlayer();
        }
        else
        {
            StartNextBombTimer();
        }
    }

    private void StartNextBombTimer()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        List<HotPotatoCharacter> alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            EndGame();
            return;
        }

        CurrentState = GameState.WaitingForNextBomb;

        _nextBombTimer = nextBombDelay;

        SetGlobalTimerText(_nextBombTimer);
    }

    private void AssignBombToRandomAlivePlayer()
    {
        List<HotPotatoCharacter> alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count == 0)
        {
            EndGame();
            return;
        }

        int randomIndex = Random.Range(0, alivePlayers.Count);

        alivePlayers[randomIndex].ReceiveBomb();

        CurrentState = GameState.Playing;

        // Pendant qu'une bombe est active sur un joueur,
        // on peut laisser vide le timer global de prochaine bombe.
        SetGlobalTimerText(string.Empty);
    }

    public void OnPlayerExploded(HotPotatoCharacter explodedPlayer, bool hadBomb)
    {
        TriggerHitPause();

        List<HotPotatoCharacter> alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= 1)
        {
            EndGame();
            return;
        }

        if (hadBomb)
        {
            StartNextBombTimer();
        }
        else
        {
            // Cas de sécurité : si un joueur explose sans bombe
            // mais qu'aucune bombe n'est active, on relance le timer.
            if (!AnyAlivePlayerHasBomb())
            {
                StartNextBombTimer();
            }
        }
    }

    private bool AnyAlivePlayerHasBomb()
    {
        if (players == null)
        {
            return false;
        }

        players.RemoveAll(p => p == null);

        foreach (HotPotatoCharacter player in players)
        {
            if (player != null && player.gameObject.activeInHierarchy && player.IsAlive && player.HasBomb)
            {
                return true;
            }
        }

        return false;
    }

    private List<HotPotatoCharacter> GetAlivePlayers()
    {
        List<HotPotatoCharacter> alivePlayers = new List<HotPotatoCharacter>();

        if (players == null)
        {
            return alivePlayers;
        }

        players.RemoveAll(p => p == null);

        foreach (HotPotatoCharacter player in players)
        {
            if (player != null && player.gameObject.activeInHierarchy && player.IsAlive)
            {
                alivePlayers.Add(player);
            }
        }

        return alivePlayers;
    }

    private void EndGame()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        CurrentState = GameState.GameOver;

        List<HotPotatoCharacter> alivePlayers = GetAlivePlayers();

        Transform lastPlayerTransform = alivePlayers.Count > 0
            ? alivePlayers[0].transform
            : null;

        if (gameCamera != null)
        {
            gameCamera.StartOutro(lastPlayerTransform);
        }

        SetGlobalTimerText(string.Empty);
    }

    private void SetGlobalTimerText(string text)
    {
        if (gameTimerText != null)
        {
            gameTimerText.text = text;
        }
    }

    private void SetGlobalTimerText(float time)
    {
        if (gameTimerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(time));

        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        gameTimerText.text = $"{minutes}:{seconds:00}";
    }

    private void TriggerHitPause()
    {
        if (_hitPauseRoutine != null)
        {
            StopCoroutine(_hitPauseRoutine);
        }

        if (!_isHitPausing)
        {
            _timeScaleBeforeHitPause = Time.timeScale;
        }

        _isHitPausing = true;
        Time.timeScale = 0f;

        _hitPauseRoutine = StartCoroutine(HitPauseRoutine());
    }

    private IEnumerator HitPauseRoutine()
    {
        yield return new WaitForSecondsRealtime(hitPauseDuration);

        Time.timeScale = _timeScaleBeforeHitPause;
        _isHitPausing = false;
        _hitPauseRoutine = null;
    }
}