using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridManager : MonoBehaviour
{
    // Timing and scoring constants
    private const float StartDelay = 1.0f;
    private const float FallDuration = 0.5f;
    private const float ReshuffleDelay = 0.3f;
    private const float MatchCheckDelay = 0.5f;
    private const int MatchScore = 10;

    // Configurable grid settings
    public GameObject[] prefabs;
    public int columns;
    public int rows;
    public Vector2 spacing;
    public Vector3 pieceScale = Vector3.one;
    public TMP_Text scoreText;

    public GameObject[,] grid; // The grid of items
    public bool isFalling = false;
    public bool forceReshuffle = false;

    // Audio settings
    public AudioClip swapSound;
    public AudioClip matchSound;
    public AudioSource audioSource;

    // Private Variables
    private int _score = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        StartCoroutine(StartWithDelay());
        UpdateScoreText();
    }

    void Update()
    {
        if (forceReshuffle) // Manual reshuffle
        {
            forceReshuffle = false;
            StartCoroutine(ReshuffleBoardWithDelay());
        }
    }

    private IEnumerator StartWithDelay()
    {
        CreateGrid();
        yield return new WaitForSeconds(StartDelay);
        CheckForMatches();
    }

    void CreateGrid()
    {
        grid = new GameObject[columns, rows]; // Initialize the grid

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 position = CalculatePosition(x, y); // Calculate the position for the grid item
                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)]; // Select a random prefab
                if (prefab != null)
                {
                    GameObject gridItem = Instantiate(prefab, position, Quaternion.identity);
                    gridItem.transform.SetParent(transform);
                    gridItem.transform.localScale = pieceScale;
                    gridItem.AddComponent<GridItem>();
                    grid[x, y] = gridItem;
                }
            }
        }
    }

    public Vector3 CalculatePosition(int column, int row) // Calculate the position of piece
    {
        return new Vector3(column * spacing.x, row * spacing.y, 0);
    }

    public bool CheckForMatches()
    {
        HashSet<GameObject> matches = new HashSet<GameObject>(); // Store matched items

        FindHorizontalMatches(matches);
        FindVerticalMatches(matches);

        foreach (GameObject match in matches)
        {
            Vector2Int position = GetGridPosition(match); // Get the position of the matched item
            if (position != -Vector2Int.one)
            {
                grid[position.x, position.y] = null; // Remove the matched item from the grid
                Destroy(match); // Destroy the matched item
            }
        }

        if (matches.Count > 0 && matchSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(matchSound);
        }

        _score += matches.Count * MatchScore; // Update the score based on matches
        UpdateScoreText();

        HandleFallingAndRefill(); // Handle falling and refilling of the grid

        if (matches.Count > 0)
        {
            StartCoroutine(VerifyMatchesAfterDelay());
        }

        return matches.Count > 0;
    }

    private void FindHorizontalMatches(HashSet<GameObject> matches)
    {
        // Look for matches in rows
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns - 2; x++)
            {
                GameObject current = grid[x, y];
                GameObject next1 = grid[x + 1, y];
                GameObject next2 = grid[x + 2, y];

                if (current != null && next1 != null && next2 != null &&
                    current.name == next1.name && current.name == next2.name)
                {
                    matches.Add(current);
                    matches.Add(next1);
                    matches.Add(next2);
                }
            }
        }
    }

    private void FindVerticalMatches(HashSet<GameObject> matches)
    {
        // Look for matches in columns
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows - 2; y++)
            {
                GameObject current = grid[x, y];
                GameObject next1 = grid[x, y + 1];
                GameObject next2 = grid[x, y + 2];

                if (current != null && next1 != null && next2 != null &&
                    current.name == next1.name && current.name == next2.name)
                {
                    matches.Add(current);
                    matches.Add(next1);
                    matches.Add(next2);
                }
            }
        }
    }

    private void HandleFallingAndRefill()
    {
        // Items fall to fill empty spaces, new ones spawn
        for (int x = 0; x < columns; x++)
        {
            for (int y = 1; y < rows; y++)
            {
                if (grid[x, y] != null && grid[x, y - 1] == null)
                {
                    int fallY = y;
                    while (fallY > 0 && grid[x, fallY - 1] == null)
                    {
                        fallY--;
                    }

                    grid[x, fallY] = grid[x, y]; // Move the item down
                    grid[x, y] = null; // Clear the original position
                    StartCoroutine(FallToPosition(grid[x, fallY], CalculatePosition(x, fallY))); // Animate the fall
                }
            }

            for (int y = rows - 1; y >= 0; y--)
            {
                if (grid[x, y] == null)
                {
                    Vector3 position = CalculatePosition(x, y);
                    GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                    if (prefab != null)
                    {
                        GameObject gridItem = Instantiate(prefab, position + new Vector3(0, spacing.y * rows, 0), Quaternion.identity);
                        gridItem.transform.SetParent(transform);
                        gridItem.transform.localScale = pieceScale;
                        gridItem.AddComponent<GridItem>();
                        grid[x, y] = gridItem;
                        StartCoroutine(FallToPosition(gridItem, position));
                    }
                }
            }
        }
    }

    private IEnumerator VerifyMatchesAfterDelay()
    {
        yield return new WaitForSeconds(MatchCheckDelay);
        CheckForMatches();
    }

    private IEnumerator FallToPosition(GameObject item, Vector3 targetPosition)
    {
        // Animate item falling to a new position
        if (item == null) yield break;

        isFalling = true;
        float elapsedTime = 0f;
        Vector3 initialPosition = item.transform.position;

        while (elapsedTime < FallDuration)
        {
            if (item == null) yield break;

            item.transform.position = Vector3.Lerp(initialPosition, targetPosition, elapsedTime / FallDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (item != null)
        {
            item.transform.position = targetPosition;
            item.GetComponent<GridItem>().UpdateInitialPosition(targetPosition);
        }

        isFalling = false;

        if (swapSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(swapSound);
        }
    }

    private Vector2Int GetGridPosition(GameObject gridItem)
    {
        // Find the grid position of a specific item
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] == gridItem)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one;
    }

    private IEnumerator ReshuffleBoardWithDelay()
    {
        yield return new WaitForSeconds(ReshuffleDelay);
        ReshuffleBoard();
    }

    private void ReshuffleBoard()
    {
        // Collect all items and shuffle them
        List<GameObject> items = new List<GameObject>();

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] != null)
                {
                    items.Add(grid[x, y]);
                    grid[x, y] = null; // Clear grid references
                }
            }
        }

        ShuffleList(items); // Shuffle items

        // Reassign shuffled items to grid
        int index = 0;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (index < items.Count)
                {
                    grid[x, y] = items[index];
                    index++;
                }
            }
        }

        UpdateGridPositions(); // Ensure item positions match their grid slots

        StartCoroutine(CheckAndHandleMatchesAfterReshuffle());
    }

    private void ShuffleList(List<GameObject> list)
    {
        // Shuffle items randomly
        for (int i = 0; i < list.Count; i++)
        {
            GameObject temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void UpdateGridPositions()
    {
        // Sync item transforms with their grid positions
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y].transform.position = CalculatePosition(x, y);
                    grid[x, y].GetComponent<GridItem>().UpdateInitialPosition(CalculatePosition(x, y));
                }
            }
        }
    }

    private IEnumerator CheckAndHandleMatchesAfterReshuffle()
    {
        yield return new WaitForSeconds(MatchCheckDelay);

        // Check for matches after reshuffle
        if (CheckForMatches())
        {
            yield return new WaitForSeconds(MatchCheckDelay);
            CheckForMatches();
        }
    }

    public void ResetScore()
    {
        // Reset score and update UI
        _score = 0;
        UpdateScoreText();
        Debug.Log("Score reset to 0");
    }

    private void UpdateScoreText()
    {
        // Update score display
        if (scoreText != null)
        {
            scoreText.text = _score.ToString();
        }
    }
}
