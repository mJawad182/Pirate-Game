using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Main game manager for the Pirate Escape Room.
/// Coordinates all systems: cameras, PLC, water, storm, ships, and puzzles.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("System References")]
    [Tooltip("Multi-camera manager")]
    public MultiCameraManager cameraManager;
    
    [Tooltip("PLC UDP listener")]
    public PLCUDPListener plcListener;
    
    [Tooltip("Storm controller")]
    public StormController stormController;
    
    [Tooltip("Ship interaction handler")]
    public ShipInteraction shipInteraction;
    
    [Header("Ship Reference")]
    [Tooltip("Main ship/player object")]
    public GameObject shipObject;
    
    [Header("Game State")]
    [Tooltip("Current game state")]
    public GameState currentState = GameState.Initializing;
    
    [Tooltip("Time limit for escape room (seconds, 0 = no limit)")]
    public float timeLimit = 0f;
    
    [Header("Puzzle System")]
    [Tooltip("List of puzzles in the escape room")]
    public List<EscapeRoomPuzzle> puzzles = new List<EscapeRoomPuzzle>();
    
    [Header("Audio")]
    [Tooltip("Background music audio source")]
    public AudioSource backgroundMusic;
    
    [Header("UI")]
    [Tooltip("Main game UI canvas")]
    public GameObject gameUI;
    
    [Tooltip("Game over/win UI")]
    public GameObject gameOverUI;
    
    [Header("Debug")]
    public bool debugMode = true;
    
    private float gameStartTime;
    private float elapsedTime;
    private bool gameWon = false;
    private bool gameLost = false;
    
    // Singleton instance
    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        InitializeGame();
    }
    
    /// <summary>
    /// Initialize all game systems
    /// </summary>
    void InitializeGame()
    {
        gameStartTime = Time.time;
        currentState = GameState.Playing;
        
        // Find components if not assigned
        if (cameraManager == null)
            cameraManager = FindObjectOfType<MultiCameraManager>();
        
        if (plcListener == null)
            plcListener = FindObjectOfType<PLCUDPListener>();
        
        if (stormController == null)
            stormController = FindObjectOfType<StormController>();
        
        if (shipInteraction == null)
            shipInteraction = FindObjectOfType<ShipInteraction>();
        
        if (shipObject == null)
        {
            shipObject = GameObject.FindGameObjectWithTag("Player");
            if (shipObject == null)
                shipObject = GameObject.Find("Ship");
        }
        
        // Setup camera manager with ship reference
        if (cameraManager != null && shipObject != null)
        {
            cameraManager.shipTransform = shipObject.transform;
        }
        
        // Setup PLC listener events
        if (plcListener != null)
        {
            plcListener.OnSimulatedKeyPressed += HandlePLCKeyPress;
        }
        
        // Setup storm events
        if (stormController != null)
        {
            stormController.OnStormArrived += OnStormArrived;
        }
        
        // Setup ship interaction events
        if (shipInteraction != null)
        {
            shipInteraction.OnShipInteracted += OnShipInteracted;
        }
        
        // Initialize puzzles
        InitializePuzzles();
        
        // Start background music
        if (backgroundMusic != null)
        {
            backgroundMusic.Play();
        }
        
        if (debugMode)
        {
            Debug.Log("Game Manager initialized. All systems ready.");
        }
    }
    
    /// <summary>
    /// Initialize all puzzles
    /// </summary>
    void InitializePuzzles()
    {
        foreach (var puzzle in puzzles)
        {
            if (puzzle != null)
            {
                puzzle.Initialize();
                puzzle.OnPuzzleSolved += OnPuzzleSolved;
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"Initialized {puzzles.Count} puzzles");
        }
    }
    
    void Update()
    {
        if (currentState == GameState.Playing)
        {
            UpdateGameTime();
            CheckGameConditions();
        }
        
        // Handle debug commands
        if (debugMode)
        {
            HandleDebugInput();
        }
    }
    
    /// <summary>
    /// Update game time
    /// </summary>
    void UpdateGameTime()
    {
        elapsedTime = Time.time - gameStartTime;
        
        // Check time limit
        if (timeLimit > 0 && elapsedTime >= timeLimit)
        {
            OnTimeLimitReached();
        }
    }
    
    /// <summary>
    /// Check win/lose conditions
    /// </summary>
    void CheckGameConditions()
    {
        if (gameWon || gameLost) return;
        
        // Check if all puzzles are solved
        bool allPuzzlesSolved = true;
        foreach (var puzzle in puzzles)
        {
            if (puzzle != null && !puzzle.IsSolved)
            {
                allPuzzlesSolved = false;
                break;
            }
        }
        
        if (allPuzzlesSolved && puzzles.Count > 0)
        {
            WinGame();
        }
    }
    
    /// <summary>
    /// Handle PLC key press events
    /// </summary>
    void HandlePLCKeyPress(KeyCode keyCode)
    {
        if (debugMode)
        {
            Debug.Log($"PLC Key Press: {keyCode}");
        }
        
        // Route key press to appropriate systems
        // Puzzles can listen to these events or check PLCUDPListener directly
    }
    
    /// <summary>
    /// Called when a puzzle is solved
    /// </summary>
    void OnPuzzleSolved(EscapeRoomPuzzle puzzle)
    {
        if (debugMode)
        {
            Debug.Log($"Puzzle solved: {puzzle.puzzleName}");
        }
        
        // Check if all puzzles are solved
        CheckGameConditions();
    }
    
    /// <summary>
    /// Called when a ship is interacted with
    /// </summary>
    void OnShipInteracted(InteractableShip ship)
    {
        if (debugMode)
        {
            Debug.Log($"Ship interacted: {ship.shipName}");
        }
        
        // Check if this ship interaction solves a puzzle
        foreach (var puzzle in puzzles)
        {
            if (puzzle != null && puzzle.CheckShipInteraction(ship))
            {
                puzzle.Solve();
            }
        }
    }
    
    /// <summary>
    /// Called when storm arrives
    /// </summary>
    void OnStormArrived()
    {
        if (debugMode)
        {
            Debug.LogWarning("Storm has arrived! Time is running out!");
        }
        
        // Storm arrival might trigger a puzzle or time pressure
        // You can add logic here to increase difficulty or trigger events
    }
    
    /// <summary>
    /// Called when time limit is reached
    /// </summary>
    void OnTimeLimitReached()
    {
        LoseGame("Time limit reached!");
    }
    
    /// <summary>
    /// Win the game
    /// </summary>
    public void WinGame()
    {
        if (gameWon) return;
        
        gameWon = true;
        currentState = GameState.Won;
        
        if (debugMode)
        {
            Debug.Log("GAME WON! Congratulations!");
        }
        
        ShowGameOverUI(true);
    }
    
    /// <summary>
    /// Lose the game
    /// </summary>
    public void LoseGame(string reason = "")
    {
        if (gameLost) return;
        
        gameLost = true;
        currentState = GameState.Lost;
        
        if (debugMode)
        {
            Debug.Log($"GAME LOST! Reason: {reason}");
        }
        
        ShowGameOverUI(false, reason);
    }
    
    /// <summary>
    /// Show game over UI
    /// </summary>
    void ShowGameOverUI(bool won, string message = "")
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
            // Update UI text with win/lose message
        }
    }
    
    /// <summary>
    /// Restart the game
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// Handle debug input
    /// </summary>
    void HandleDebugInput()
    {
        // Press R to restart
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
        
        // Press P to solve all puzzles (debug)
        if (Input.GetKeyDown(KeyCode.P))
        {
            foreach (var puzzle in puzzles)
            {
                if (puzzle != null && !puzzle.IsSolved)
                {
                    puzzle.Solve();
                }
            }
        }
    }
    
    /// <summary>
    /// Get elapsed game time
    /// </summary>
    public float GetElapsedTime()
    {
        return elapsedTime;
    }
    
    /// <summary>
    /// Get remaining time (if time limit is set)
    /// </summary>
    public float GetRemainingTime()
    {
        if (timeLimit > 0)
            return Mathf.Max(0, timeLimit - elapsedTime);
        return -1;
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (plcListener != null)
        {
            plcListener.OnSimulatedKeyPressed -= HandlePLCKeyPress;
        }
        
        if (stormController != null)
        {
            stormController.OnStormArrived -= OnStormArrived;
        }
        
        if (shipInteraction != null)
        {
            shipInteraction.OnShipInteracted -= OnShipInteracted;
        }
        
        foreach (var puzzle in puzzles)
        {
            if (puzzle != null)
            {
                puzzle.OnPuzzleSolved -= OnPuzzleSolved;
            }
        }
    }
}

/// <summary>
/// Game state enumeration
/// </summary>
public enum GameState
{
    Initializing,
    Playing,
    Paused,
    Won,
    Lost
}

/// <summary>
/// Base class for escape room puzzles
/// </summary>
[System.Serializable]
public class EscapeRoomPuzzle
{
    [Header("Puzzle Info")]
    public string puzzleName = "Puzzle";
    public string puzzleDescription = "";
    
    [Header("Puzzle Requirements")]
    [Tooltip("Ship name that must be interacted with (optional)")]
    public string requiredShipName = "";
    
    [Tooltip("Key that must be pressed (optional)")]
    public KeyCode requiredKey = KeyCode.None;
    
    [Tooltip("PLC command that must be received (optional)")]
    public string requiredPLCCommand = "";
    
    [Header("Puzzle State")]
    public bool IsSolved { get; private set; }
    
    /// <summary>
    /// Event triggered when puzzle is solved
    /// </summary>
    public System.Action<EscapeRoomPuzzle> OnPuzzleSolved;
    
    /// <summary>
    /// Initialize the puzzle
    /// </summary>
    public virtual void Initialize()
    {
        IsSolved = false;
    }
    
    /// <summary>
    /// Check if ship interaction solves this puzzle
    /// </summary>
    public virtual bool CheckShipInteraction(InteractableShip ship)
    {
        if (IsSolved) return false;
        
        if (!string.IsNullOrEmpty(requiredShipName))
        {
            return ship.shipName == requiredShipName;
        }
        
        return false;
    }
    
    /// <summary>
    /// Solve the puzzle
    /// </summary>
    public virtual void Solve()
    {
        if (IsSolved) return;
        
        IsSolved = true;
        OnPuzzleSolved?.Invoke(this);
    }
    
    /// <summary>
    /// Reset the puzzle
    /// </summary>
    public virtual void Reset()
    {
        IsSolved = false;
    }
}



