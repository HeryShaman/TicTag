using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    [SerializeField] private PlayerSpawner spawner;
    [SerializeField] private List<HotPotatoCharacter> players = new List<HotPotatoCharacter>();
    [SerializeField] private PauseMenu pauseMenu;

    [Header("Timing")]
    [SerializeField] private float preStartDuration = 2f;
    // Si aucun joueur n'a la bombe, elle est attribuée au hasard après ce délai (aucun affichage).
    [SerializeField] private float bombAssignDelay = 3f;
    [SerializeField] private float hitPauseDuration = 0.25f;

    [Header("Start rules")]
    [SerializeField] private bool assignFirstBombImmediately = true;

    [Header("Music")]
    [SerializeField] private MusicPlayer music;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private AudioClip gameOverMusic;   // optionnel : si vide, la musique de jeu continue
    [SerializeField, Min(0f)] private float musicFadeIn = 1f;
    [SerializeField, Range(0f, 1f)] private float pausedMusicVolume = 0.35f;

    [Header("Scenes")]
    [SerializeField] private string menuSceneName = "MainMenu";

    public GameState CurrentState { get; private set; } = GameState.Setup;
    public bool IsPaused => _isPaused;

    private float _preStartTimer;
    private float _bombAssignTimer;

    // La partie se termine quand il reste <= _endThreshold joueurs vivants
    // (1 en multi ; 0 si la scène est lancée avec un seul joueur pour les tests).
    private int _endThreshold = 1;

    private bool _isPaused;
    private bool _isHitPausing;
    private Coroutine _hitPauseRoutine;

    private void Awake()
    {
        Instance = this;

        if (players == null)
        {
            players = new List<HotPotatoCharacter>();
        }

        if (spawner != null)
        {
            // Joueurs issus du menu de sélection (GameSession).
            players.Clear();
            players.AddRange(spawner.SpawnAll());
        }
        else if (players.Count == 0)
        {
            players.AddRange(HotPotatoCharacter.AllCharacters);
        }

        _endThreshold = players.Count <= 1 ? 0 : 1;
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

    // =====================================================================
    // Pause / scènes
    // =====================================================================

    public void TogglePause()
    {
        if (_isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        if (_isPaused) return;

        _isPaused = true;
        ApplyTimeScale();

        if (pauseMenu != null) pauseMenu.Show();
        if (music != null) music.SetDuck(pausedMusicVolume);
    }

    public void ResumeGame()
    {
        if (!_isPaused) return;

        _isPaused = false;
        ApplyTimeScale();

        if (pauseMenu != null) pauseMenu.Hide();
        if (music != null) music.SetDuck(1f);
    }

    // Recharge la scène : GameSession est conservée, ce sont donc les mêmes joueurs, index et couleurs.
    public void RestartGame()
    {
        _isPaused = false;
        _isHitPausing = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        _isPaused = false;
        _isHitPausing = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuSceneName);
    }

    // Un seul endroit décide du timeScale : pause et hit-pause ne s'écrasent plus.
    private void ApplyTimeScale()
    {
        Time.timeScale = (_isPaused || _isHitPausing) ? 0f : 1f;
    }

    // =====================================================================
    // Déroulement de la partie
    // =====================================================================

    private void StartPreStart()
    {
        CurrentState = GameState.PreStart;

        _preStartTimer = preStartDuration;

        if (gameCamera != null)
        {
            gameCamera.StartIntro();
        }

        if (music != null && gameMusic != null)
        {
            music.Play(gameMusic, musicFadeIn);
        }
    }

    private void Update()
    {
        // La pause reste disponible dans tous les états (y compris GameOver : recommencer / quitter).
        if (InputReader.GetAnyPausePressed())
        {
            TogglePause();
        }

        if (_isPaused || CurrentState == GameState.GameOver)
        {
            return;
        }

        // Sécurité : s'il ne reste plus assez de joueurs vivants, on termine la partie.
        if (CurrentState != GameState.PreStart)
        {
            if (GetAlivePlayers().Count <= _endThreshold)
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
                    StartBombAssignTimer();
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
        _bombAssignTimer -= Time.deltaTime;

        if (_bombAssignTimer <= 0f)
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
            StartBombAssignTimer();
        }
    }

    // Timer interne et invisible : à son terme, la bombe est donnée à un joueur au hasard.
    private void StartBombAssignTimer()
    {
        if (CurrentState == GameState.GameOver)
        {
            return;
        }

        if (GetAlivePlayers().Count <= _endThreshold)
        {
            EndGame();
            return;
        }

        CurrentState = GameState.WaitingForNextBomb;
        _bombAssignTimer = bombAssignDelay;
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

        // Nouvelle bombe : durée initiale (la réduction par touche repart de zéro).
        alivePlayers[randomIndex].ReceiveBomb();

        CurrentState = GameState.Playing;
    }

    public void OnPlayerExploded(HotPotatoCharacter explodedPlayer, bool hadBomb)
    {
        TriggerHitPause();

        List<HotPotatoCharacter> alivePlayers = GetAlivePlayers();

        if (alivePlayers.Count <= _endThreshold)
        {
            EndGame();
            return;
        }

        if (hadBomb)
        {
            StartBombAssignTimer();
        }
        else
        {
            // Cas de sécurité : si un joueur explose sans bombe
            // mais qu'aucune bombe n'est active, on relance le timer.
            if (!AnyAlivePlayerHasBomb())
            {
                StartBombAssignTimer();
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

        if (music != null && gameOverMusic != null)
        {
            music.Play(gameOverMusic, 0.5f);
        }
    }

    // =====================================================================
    // Hit pause
    // =====================================================================

    private void TriggerHitPause()
    {
        if (_hitPauseRoutine != null)
        {
            StopCoroutine(_hitPauseRoutine);
        }

        _isHitPausing = true;
        ApplyTimeScale();

        _hitPauseRoutine = StartCoroutine(HitPauseRoutine());
    }

    private IEnumerator HitPauseRoutine()
    {
        yield return new WaitForSecondsRealtime(hitPauseDuration);

        _isHitPausing = false;
        _hitPauseRoutine = null;
        ApplyTimeScale();
    }
}