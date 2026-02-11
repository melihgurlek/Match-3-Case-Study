using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    public int rows = 10;
    public int columns = 12;
    public int colors = 6;
    public int groupSize_1;
    public int groupSize_2;
    public int groupSize_3;

    public GameObject[] tilePrefabs;

    private GameObject[,] board;

    void Start()
    {
        SetGroupSizes();
        InitializeBoard();
        StartCoroutine(DelayedIconUpdate());
    }

    private void SetGroupSizes()
    {
        if (groupSize_1 <= 0) groupSize_1 = 2;
        if (groupSize_2 <= 0) groupSize_2 = 3;
        if (groupSize_3 <= 0) groupSize_3 = 5;
        if (colors <= 0) colors = 6;

        groupSize_1 = Mathf.Clamp(groupSize_1, 1, 10);
        groupSize_2 = Mathf.Clamp(groupSize_2, 1, 10);
        groupSize_3 = Mathf.Clamp(groupSize_3, 1, 10);

        rows = Mathf.Clamp(rows, 2, 10);
        columns = Mathf.Clamp(columns, 2, 10);

        groupSize_2 = Mathf.Max(groupSize_1, groupSize_2);
        groupSize_3 = Mathf.Max(groupSize_2, groupSize_3);

        TileController.SetGroupSizeThresholds(groupSize_1, groupSize_2, groupSize_3);
    }

    private IEnumerator DelayedIconUpdate()
    {
        yield return new WaitForSeconds(0.2f);
        UpdateGroupIcons();
    }

    void InitializeBoard()
    {
        Camera.main.transform.position = new Vector3(columns / 2f - 0.5f, -rows / 2f + 0.5f, -10);
        Camera.main.orthographicSize = rows / 2f;

        board = new GameObject[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                SpawnTileAt(i, j, Random.Range(0, colors));
            }
        }
    }

    void SpawnTileAt(int i, int j, int colorIndex)
    {
        // Use ObjectPooler instead of Instantiate
        GameObject tile = ObjectPooler.Instance.SpawnFromPool(tilePrefabs[colorIndex], new Vector2(j, -i), Quaternion.identity);
        tile.transform.parent = this.transform;

        TileController tileController = tile.GetComponent<TileController>();
        if (tileController != null)
        {
            tileController.colorIndex = colorIndex;
        }

        SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.sortingOrder = rows - i;
        }

        board[i, j] = tile;
    }
    List<GameObject> FindGroup(int startRow, int startCol, int colorIndex, bool[,] visited)
    {
        List<GameObject> group = new List<GameObject>();

        // Use a Queue for Iterative BFS (Prevents Stack Overflow)
        Queue<Vector2Int> searchQueue = new Queue<Vector2Int>();

        // Add starting node
        if (IsValidTile(startRow, startCol, colorIndex, visited))
        {
            searchQueue.Enqueue(new Vector2Int(startRow, startCol));
            visited[startRow, startCol] = true;
        }

        while (searchQueue.Count > 0)
        {
            Vector2Int current = searchQueue.Dequeue();
            int r = current.x;
            int c = current.y;

            group.Add(board[r, c]);

            // Check Neighbors (Up, Down, Left, Right)
            CheckNeighbor(r + 1, c, colorIndex, visited, searchQueue);
            CheckNeighbor(r - 1, c, colorIndex, visited, searchQueue);
            CheckNeighbor(r, c + 1, colorIndex, visited, searchQueue);
            CheckNeighbor(r, c - 1, colorIndex, visited, searchQueue);
        }

        return group;
    }

    void CheckNeighbor(int r, int c, int colorIndex, bool[,] visited, Queue<Vector2Int> queue)
    {
        if (IsValidTile(r, c, colorIndex, visited))
        {
            visited[r, c] = true;
            queue.Enqueue(new Vector2Int(r, c));
        }
    }

    bool IsValidTile(int r, int c, int targetColor, bool[,] visited)
    {
        if (r < 0 || r >= rows || c < 0 || c >= columns) return false;
        if (visited[r, c]) return false;
        if (board[r, c] == null) return false;

        TileController tc = board[r, c].GetComponent<TileController>();
        return tc != null && tc.colorIndex == targetColor;
    }
    // ---------------------------------------------------------

    void RemoveGroup(List<GameObject> group)
    {
        int lowestRow = rows;

        foreach (GameObject tile in group)
        {
            int row = Mathf.RoundToInt(-tile.transform.position.y);
            if (row < lowestRow) lowestRow = row;

            ObjectPooler.Instance.ReturnToPool(tile);
        }

        FillBoard();
    }

    void FillBoard()
    {
        float slideDuration = 0.15f;
        float newSlideDuration = 0.05f;
        StartCoroutine(FillBoardRoutine(slideDuration, newSlideDuration));
    }

    private IEnumerator FillBoardRoutine(float slideDuration, float newSlideDuration)
    {
        yield return StartCoroutine(CascadeTiles(slideDuration));
        yield return StartCoroutine(CascadeTiles(slideDuration)); // Run twice to catch stragglers
        yield return StartCoroutine(SpawnNewTiles(newSlideDuration));
        AssignTilePriorities();
        UpdateGroupIcons();
    }

    private IEnumerator CascadeTiles(float slideDuration)
    {
        List<TileController> movingTiles = new List<TileController>();

        for (int j = 0; j < columns; j++)
        {
            int emptyRow = -1;
            for (int i = rows - 1; i >= 0; i--)
            {
                if (board[i, j] == null || !board[i, j].activeSelf) // Check activeSelf for pooling safety
                {
                    if (emptyRow == -1) emptyRow = i;
                }
                else if (emptyRow != -1)
                {
                    board[emptyRow, j] = board[i, j];
                    board[i, j] = null;

                    TileController tileController = board[emptyRow, j].GetComponent<TileController>();
                    if (tileController != null)
                    {
                        movingTiles.Add(tileController);
                        tileController.StartCoroutine(tileController.SlideToPosition(new Vector2(j, -emptyRow), slideDuration));
                    }
                    emptyRow--;
                }
            }
        }
        yield return new WaitUntil(() => movingTiles.TrueForAll(t => t == null || !t.isMoving));
    }

    private IEnumerator SpawnNewTiles(float newSlideDuration)
    {
        for (int j = 0; j < columns; j++)
        {
            for (int i = rows - 1; i >= 0; i--)
            {
                if (board[i, j] == null || !board[i, j].activeSelf)
                {
                    int randomColor = Random.Range(0, colors);

                    GameObject newTile = ObjectPooler.Instance.SpawnFromPool(tilePrefabs[randomColor], new Vector2(j, 1), Quaternion.identity);
                    newTile.transform.parent = this.transform;

                    TileController tileController = newTile.GetComponent<TileController>();
                    if (tileController != null)
                    {
                        tileController.colorIndex = randomColor;
                        board[i, j] = newTile;
                        yield return tileController.SlideToPosition(new Vector2(j, -i), newSlideDuration);
                    }
                }
            }
        }
    }

    void AssignTilePriorities()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (board[i, j] != null)
                {
                    SpriteRenderer renderer = board[i, j].GetComponent<SpriteRenderer>();
                    if (renderer != null) renderer.sortingOrder = rows - i;
                }
            }
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            int col = Mathf.RoundToInt(mousePosition.x);
            int row = Mathf.RoundToInt(-mousePosition.y);

            if (row >= 0 && row < rows && col >= 0 && col < columns)
            {
                if (board[row, col] == null) return;

                bool[,] visited = new bool[rows, columns];
                TileController clickedTile = board[row, col].GetComponent<TileController>();
                if (clickedTile == null) return;

                int clickedColorIndex = clickedTile.colorIndex;
                List<GameObject> group = FindGroup(row, col, clickedColorIndex, visited);

                if (group.Count >= 2)
                {
                    RemoveGroup(group);
                    if (CheckDeadlock()) ShuffleBoard();
                }
            }
        }
    }

    void UpdateGroupIcons()
    {
        bool[,] visited = new bool[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (board[i, j] != null && !visited[i, j])
                {
                    // FindGroup now uses BFS
                    List<GameObject> group = FindGroup(i, j, board[i, j].GetComponent<TileController>().colorIndex, visited);
                    foreach (var tile in group)
                    {
                        TileController tileController = tile.GetComponent<TileController>();
                        if (tileController != null) tileController.UpdateIcon(group.Count);
                    }
                }
            }
        }
    }

    private bool CheckDeadlock()
    {
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (board[i, j] == null) continue;
                int colorIndex = board[i, j].GetComponent<TileController>().colorIndex;
                if (i + 1 < rows && board[i + 1, j] != null && board[i + 1, j].GetComponent<TileController>().colorIndex == colorIndex)
                    return false;
                if (j + 1 < columns && board[i, j + 1] != null && board[i, j + 1].GetComponent<TileController>().colorIndex == colorIndex)
                    return false;
            }
        }
        return true;
    }

    void ShuffleBoard()
    {
        if (!CheckDeadlock()) return;

        List<GameObject> tiles = new List<GameObject>();
        foreach (GameObject tile in board)
        {
            if (tile != null) tiles.Add(tile);
        }

        WeightedShuffle(tiles);

        int index = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (board[i, j] != null)
                {
                    board[i, j] = tiles[index];
                    board[i, j].transform.position = new Vector2(j, -i);
                    index++;
                }
            }
        }
        UpdateGroupIcons();
    }

    private void WeightedShuffle(List<GameObject> tiles)
    {
        System.Random rng = new System.Random();
        for (int i = tiles.Count - 1; i > 0; i--)
        {
            int swapIndex = rng.Next(Mathf.Max(0, i - 2), i + 1);
            (tiles[i], tiles[swapIndex]) = (tiles[swapIndex], tiles[i]);
        }
    }

    public void RestartGame()
    {
        foreach (Transform child in transform)
        {
            ObjectPooler.Instance.ReturnToPool(child.gameObject);
        }

        rows = PlayerPrefs.GetInt("Rows", 10);
        columns = PlayerPrefs.GetInt("Columns", 12);
        int groupSizeA = PlayerPrefs.GetInt("GroupSizeA", 2);
        int groupSizeB = PlayerPrefs.GetInt("GroupSizeB", 3);
        int groupSizeC = PlayerPrefs.GetInt("GroupSizeC", 5);
        int colors = PlayerPrefs.GetInt("Colors", 6);

        TileController.SetGroupSizeThresholds(groupSizeA, groupSizeB, groupSizeC);
        colors = Mathf.Clamp(colors, int.MinValue, 6);

        InitializeBoard();
        StartCoroutine(DelayedIconUpdate());
    }

    public void UpdateSettings(int newRows, int newColumns, int newColors, int newGroupSizeA, int newGroupSizeB, int newGroupSizeC)
    {
        rows = newRows;
        columns = newColumns;
        colors = newColors;
        groupSize_1 = newGroupSizeA;
        groupSize_2 = newGroupSizeB;
        groupSize_3 = newGroupSizeC;
        TileController.SetGroupSizeThresholds(groupSize_1, groupSize_2, groupSize_3);
        RestartGame();
    }
}