using System.Collections;
using UnityEngine;

public class GridItem : MonoBehaviour
{
    // Constants
    private const float SwapDuration = 0.3f;
    private const float DragThreshold = 0.1f;

    // Cached references and state
    private Renderer _itemRenderer;
    private Vector3 _initialPosition;
    private GridManager _gridManager;
    private bool _isDragging = false;
    private static bool _isSwapping = false;
    private Vector3 _mouseStartPosition;

    void Start()
    {
        _itemRenderer = GetComponent<Renderer>();
        _initialPosition = transform.position;
        _gridManager = FindObjectOfType<GridManager>();
    }

    void OnMouseDown()
    {
        // Block input if grid is falling or already swapping
        if (_gridManager == null || _gridManager.isFalling || _isSwapping) return;

        _isDragging = true;
        _mouseStartPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _mouseStartPosition.z = 0; // Keep positions in 2D
        _itemRenderer.sortingOrder = 10; // Bring to front while dragging
    }

    void OnMouseDrag()
    {
        if (!_isDragging) return;

        Vector3 mouseCurrentPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseCurrentPosition.z = 0;

        if (Vector3.Distance(_mouseStartPosition, mouseCurrentPosition) <= DragThreshold) return;

        // Convert mouse position to grid coordinates
        int selectedColumn = Mathf.RoundToInt(_initialPosition.x / _gridManager.spacing.x);
        int selectedRow = Mathf.RoundToInt(_initialPosition.y / _gridManager.spacing.y);
        int currentColumn = Mathf.RoundToInt(mouseCurrentPosition.x / _gridManager.spacing.x);
        int currentRow = Mathf.RoundToInt(mouseCurrentPosition.y / _gridManager.spacing.y);

        if (IsNeighbor(selectedColumn, selectedRow, currentColumn, currentRow))
        {
            // Check if the neighbor is valid for swapping
            StartCoroutine(SwapWithNeighbor(selectedColumn, selectedRow, currentColumn, currentRow));
            _isDragging = false;
        }
    }

    void OnMouseUp()
    {
        if (!_isDragging) return;

        // Snap back if no swap occurred
        transform.position = _initialPosition;
        _itemRenderer.sortingOrder = 0;
        _isDragging = false;
    }

    private bool IsNeighbor(int selectedColumn, int selectedRow, int currentColumn, int currentRow)
    {
        // Neighbor if 1 tile apart horizontally or vertically
        return (Mathf.Abs(selectedColumn - currentColumn) == 1 && selectedRow == currentRow) ||
               (Mathf.Abs(selectedRow - currentRow) == 1 && selectedColumn == currentColumn);
    }

    private IEnumerator SwapWithNeighbor(int selectedColumn, int selectedRow, int currentColumn, int currentRow)
    {
        _isSwapping = true;

        // Get references to tiles being swapped
        GameObject selectedPrefab = _gridManager.grid[selectedColumn, selectedRow];
        GameObject currentPrefab = _gridManager.grid[currentColumn, currentRow];

        if (selectedPrefab == null || currentPrefab == null)
        {
            _isSwapping = false;
            yield break;
        }

        // Update grid logic
        _gridManager.grid[selectedColumn, selectedRow] = currentPrefab;
        _gridManager.grid[currentColumn, currentRow] = selectedPrefab;

        // Calculate new positions for swapping animation
        Vector3 newSelectedPosition = _gridManager.CalculatePosition(currentColumn, currentRow);
        Vector3 newCurrentPosition = _gridManager.CalculatePosition(selectedColumn, selectedRow);

        // Animate the swap
        float elapsedTime = 0f;
        Vector3 initialSelectedPosition = selectedPrefab.transform.position;
        Vector3 initialCurrentPosition = currentPrefab.transform.position;

        while (elapsedTime < SwapDuration)
        {
            if (selectedPrefab == null || currentPrefab == null)
            {
                _isSwapping = false;
                yield break;
            }

            selectedPrefab.transform.position = Vector3.Lerp(initialSelectedPosition, newSelectedPosition, elapsedTime / SwapDuration);
            currentPrefab.transform.position = Vector3.Lerp(initialCurrentPosition, newCurrentPosition, elapsedTime / SwapDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // Finalize positions
        if (selectedPrefab != null) selectedPrefab.transform.position = newSelectedPosition;
        if (currentPrefab != null) currentPrefab.transform.position = newCurrentPosition;

        // Check matches, revert if none
        if (_gridManager.CheckForMatches())
        {
            if (selectedPrefab != null) selectedPrefab.GetComponent<GridItem>().UpdateInitialPosition(newSelectedPosition);
            if (currentPrefab != null) currentPrefab.GetComponent<GridItem>().UpdateInitialPosition(newCurrentPosition);
        }
        else
        {
            _gridManager.grid[selectedColumn, selectedRow] = selectedPrefab;
            _gridManager.grid[currentColumn, currentRow] = currentPrefab;

            elapsedTime = 0f;
            while (elapsedTime < SwapDuration)
            {
                if (selectedPrefab == null || currentPrefab == null)
                {
                    _isSwapping = false;
                    yield break;
                }

                selectedPrefab.transform.position = Vector3.Lerp(newSelectedPosition, initialSelectedPosition, elapsedTime / SwapDuration);
                currentPrefab.transform.position = Vector3.Lerp(newCurrentPosition, initialCurrentPosition, elapsedTime / SwapDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (selectedPrefab != null) selectedPrefab.transform.position = initialSelectedPosition;
            if (currentPrefab != null) currentPrefab.transform.position = initialCurrentPosition;
        }

        _itemRenderer.sortingOrder = 0;
        _isSwapping = false;
    }

    public void UpdateInitialPosition(Vector3 newPosition)
    {
        _initialPosition = newPosition;
    }
}