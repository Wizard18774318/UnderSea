using UnityEngine;

public class RoadmapNavigator : MonoBehaviour
{
    [SerializeField] private Transform[] levelPositions;
    [SerializeField] private LevelLauncher[] levelLaunchers;
    [SerializeField] private int startingIndex;
    [SerializeField, Min(0f)] private float moveLerpSpeed = 10f;
    [SerializeField] private bool loopNavigation;

    private int currentIndex;
    private Vector3 targetPosition;

    public Transform CurrentLevel => levelPositions != null && levelPositions.Length > 0 ? levelPositions[currentIndex] : null;
    public int CurrentIndex => currentIndex;

    private void Awake()
    {
        if (levelPositions == null || levelPositions.Length == 0)
        {
            Debug.LogWarning($"{nameof(RoadmapNavigator)} on {name} has no level positions assigned.");
            enabled = false;
            return;
        }

        if (levelLaunchers != null && levelLaunchers.Length > 0 && levelLaunchers.Length != levelPositions.Length)
        {
            Debug.LogWarning($"{nameof(RoadmapNavigator)} on {name} has {levelPositions.Length} positions but {levelLaunchers.Length} launchers. Index-based launching may fail.");
        }

        startingIndex = Mathf.Clamp(startingIndex, 0, levelPositions.Length - 1);
        currentIndex = startingIndex;
        targetPosition = levelPositions[currentIndex].position;
        transform.position = targetPosition;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            MoveToIndex(currentIndex + 1);
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            MoveToIndex(currentIndex - 1);
        }

        if (moveLerpSpeed <= 0f)
        {
            transform.position = targetPosition;
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveLerpSpeed);
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            LaunchCurrentLevel();
        }
    }

    private void MoveToIndex(int nextIndex)
    {
        if (levelPositions == null || levelPositions.Length == 0)
        {
            return;
        }

        if (loopNavigation)
        {
            nextIndex = (nextIndex % levelPositions.Length + levelPositions.Length) % levelPositions.Length;
        }
        else
        {
            nextIndex = Mathf.Clamp(nextIndex, 0, levelPositions.Length - 1);
        }

        if (nextIndex == currentIndex)
        {
            return;
        }

        currentIndex = nextIndex;
        targetPosition = levelPositions[currentIndex].position;
    }

    private void LaunchCurrentLevel()
    {
        if (levelLaunchers == null || levelLaunchers.Length == 0)
        {
            Debug.LogWarning($"{nameof(RoadmapNavigator)} on {name} has no level launchers assigned.");
            return;
        }

        if (currentIndex < 0 || currentIndex >= levelLaunchers.Length)
        {
            Debug.LogWarning($"{nameof(RoadmapNavigator)} current index {currentIndex} is outside launcher array range.");
            return;
        }

        var launcher = levelLaunchers[currentIndex];
        if (launcher == null)
        {
            Debug.LogWarning($"{nameof(RoadmapNavigator)} launcher at index {currentIndex} is not set.");
            return;
        }

        launcher.Launch();
    }
}
