using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        MainMenu,
        Scanning,
        Placing,
        Playing,
        GameOver
    }

    public GameObject _boardPrefab;
    public GameObject _dartPrefab;

    public GameObject Board { get; private set; }
    public GameObject Dart { get; private set; }

    public int PlayerScore { get; set; } = 0;

    private int _playerDarts = 8;
    public int PlayerDarts
    {
        get
        {
            return _playerDarts;
        }
        set
        {
            _playerDarts = value;

            if (_playerDarts == 0)
                gameOver = true;
        }
    }

    private readonly float scanningTime = 15.0f;

    private bool gameOver = false;

    GameState _currentState = GameState.Scanning;

    public GameState CurrentState
    {
        get { return _currentState; }
        set
        {
            _currentState = value;
            OnStateChanged();
        }
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else if (Instance != this)
            Destroy(gameObject);
    }

    void Start()
    {
        CurrentState = GameState.Scanning;
    }

    void OnStateChanged()
    {
        StopAllCoroutines();

        switch (_currentState)
        {
            case GameState.MainMenu:
                StartCoroutine(MainMenuStateRoutine());
                break;
            case GameState.Scanning:
                StartCoroutine(ScanningStateRoutine());
                break;
            case GameState.Placing:
                StartCoroutine(PlacingStateRoutine());
                break;
            case GameState.Playing:
                StartCoroutine(PlayingStateRoutine());
                break;
            case GameState.GameOver:
                StartCoroutine(GameOverStateRoutine());
                break;
        }
    }

    IEnumerator MainMenuStateRoutine()
    {
        GazeManager.Instance.RaycastLayerMask = LayerMask.GetMask("UI");

        // Show the main menu
        // Buttons to start game, options, leaderboards and quit the game
        yield return null;
    }

    IEnumerator ScanningStateRoutine()
    {
        // Activate SpatialMappingManager to scan the surfaces with SurfaceObserver
        // and visualize the spatial mapping meshes
        GazeManager.Instance.RaycastLayerMask = LayerMask.GetMask("SpatialSurface", "Hologram", "UI");

        Debug.Log("Drawing spatial mapping started: Move around the room");
        SpatialMappingManager.Instance.IsObserving = true;
        SpatialMappingManager.Instance.SurfacesVisible = true;

        yield return new WaitForSeconds(scanningTime);

        CurrentState = GameState.Placing;
    }

    IEnumerator PlacingStateRoutine()
    {
        // Stop the SurfaceObserver
        // Spawn the board and place it on a surface by following player's gaze 

        GazeManager.Instance.RaycastLayerMask = LayerMask.GetMask("SpatialSurface", "UI");

        SpatialMappingManager.Instance.IsObserving = false;
        Debug.Log("Drawing spatial mapping ended: Place the board");

        Board = Instantiate(_boardPrefab, Camera.main.transform.position + (Camera.main.transform.forward * 1.5f), Quaternion.identity);

        var placeable = Board.GetComponent<Placeable>();

        if (placeable == null)
            placeable = Board.AddComponent<Placeable>();

        while (!placeable.Placed)
        {
            yield return null;
        }

        Destroy(placeable);
        Board.AddComponent<WorldAnchor>();

        CurrentState = GameState.Playing;
    }

    IEnumerator PlayingStateRoutine()
    {
        // Hide the spatial mapping mesh
        // Display player score and darts
        // Spawn the dart for the player

        Debug.Log("Hiding Spatial Mapping meshes: Enjoy playing!");
        SpatialMappingManager.Instance.SurfacesVisible = false;
        GazeManager.Instance.RaycastLayerMask = LayerMask.GetMask("SpatialSurface", "Hologram", "UI");
        //GestureManager.Instance.RegisterInteractionManager();

        while (!gameOver)
        {
            if (Dart == null || Dart.GetComponent<Throwable>().HasLanded == true)
                Dart = SpawnDart();
            yield return null;
        }

        CurrentState = GameState.GameOver;
    }

    IEnumerator GameOverStateRoutine()
    {
        // Display overall score and ask for a name
        // Show menu with buttons to play again or return to main menu

        while (CurrentState == GameState.GameOver)
            yield return null;
    }

    private GameObject SpawnDart()
    {
        GameObject Dart = Instantiate(_dartPrefab, Camera.main.transform.position + (Camera.main.transform.forward * 1.0f), Quaternion.Euler(Camera.main.transform.right));
        var throwable = Dart.GetComponent<Throwable>();

        if (throwable == null)
            throwable = Dart.AddComponent<Throwable>();

        return Dart;
    }
}
