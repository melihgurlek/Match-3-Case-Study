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

    public GameObject[] tilePrefabs; // Array of tile prefabs (one for each color)

    private GameObject[,] board;
    private Queue<GameObject> tilePool = new Queue<GameObject>();

    void Start()
    {
        SetGroupSizes();
        InitializeBoard();
        StartCoroutine(DelayedIconUpdate());  // Delay icon updates
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

    // Object pooling is much better than instantiating tiles every time
    private GameObject GetTileFromPool(int colorIndex, Vector2 position)
    {
        GameObject tile;

        if (tilePool.Count > 0)
        {
            if (Random.value < 0.3f) // ✅ 30% chance to take a random tile
            {
                int randomIndex = Random.Range(0, tilePool.Count);
                tile = tilePool.ToArray()[randomIndex]; // Take a random tile
                tilePool = new Queue<GameObject>(tilePool.Except(new[] { tile })); // Remove from pool
            }
            else
            {
                tile = tilePool.Dequeue();
            }

            tile.SetActive(true);
        }
        else
        {
            tile = Instantiate(tilePrefabs[colorIndex]);
        }

        tile.transform.position = position;
        tile.GetComponent<TileController>().colorIndex = colorIndex;
        return tile;
    }

    private void ReturnTileToPool(GameObject tile)
    {
        tile.SetActive(false);
        tile.transform.position = new Vector2(-100, -100);
        tilePool.Enqueue(tile);
    }

    public void RemoveTile(int x, int y)
    {
        if (x < 0 || x >= rows || y < 0 || y >= columns) return;
        if (board[x, y] == null) return;

        ReturnTileToPool(board[x, y]);
        board[x, y] = null;
    }

    private IEnumerator DelayedIconUpdate()
    {
        yield return new WaitForSeconds(0.2f);  // Wait until the next frame to ensure the board is fully initialized
        UpdateGroupIcons(); // Dynamically assign icons based on initial group sizes
    }

    void InitializeBoard()
    {
        // Set up the camera
        Camera.main.transform.position = new Vector3(columns / 2f - 0.5f, -rows / 2f + 0.5f, -10);
        Camera.main.orthographicSize = rows / 2f;

        // Initialize the board
        board = new GameObject[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                int randomColor = Random.Range(0, colors);
                GameObject tile = GetTileFromPool(randomColor, new Vector2(j, -i));
                tile.transform.parent = this.transform;

                // Assign color index to the tile
                TileController tileController = tile.GetComponent<TileController>();
                if (tileController != null)
                {
                    tileController.colorIndex = randomColor;
                }
                else
                {
                    Debug.LogError($"TileController is missing on prefab for color index {randomColor}");
                }

                // Set the sorting order based on the row
                SpriteRenderer renderer = tile.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sortingOrder = rows - i; // Higher rows have higher priority
                }

                board[i, j] = tile;

                Debug.Log($"Created tile at row={i}, col={j}, colorIndex={randomColor}");
            }
        }
    }

    // Uses BFS to find a group of tiles with the same color instead of recursion
    // Because recursion leads to too many merges which is not efficient enough
    List<GameObject> FindGroup(int row, int col, int colorIndex, bool[,] visited)
    {
        List<GameObject> group = new List<GameObject>();
        Queue<Vector2Int> queue = new Queue<Vector2Int>();

        queue.Enqueue(new Vector2Int(row, col));

        while (queue.Count > 0)
        {
            Vector2Int tilePos = queue.Dequeue();

            int i = tilePos.x;
            int j = tilePos.y;

            if (i < 0 || i >= rows || j < 0 || j >= columns || visited[i, j] || board[i, j] == null)
                continue;

            TileController tileController = board[i, j].GetComponent<TileController>();
            if (tileController == null || tileController.colorIndex != colorIndex)
                continue;

            visited[i, j] = true;
            group.Add(board[i, j]);

            queue.Enqueue(new Vector2Int(i + 1, j));
            queue.Enqueue(new Vector2Int(i - 1, j));
            queue.Enqueue(new Vector2Int(i, j + 1));
            queue.Enqueue(new Vector2Int(i, j - 1));
        }

        return group;
    }

    public void RemoveGroup(List<GameObject> group)
    {
        int lowestRow = rows;

        foreach (GameObject tile in group)
        {
            int row = Mathf.RoundToInt(-tile.transform.position.y);
            if (row < lowestRow)
                lowestRow = row;

            int x = Mathf.RoundToInt(-tile.transform.position.y);
            int y = Mathf.RoundToInt(tile.transform.position.x);
            RemoveTile(x, y);
        }

        FillBoard();
    }

    void FillBoard()
    {
        Debug.Log("Filling board...");
        float slideDuration = 0.15f; // Duration of the sliding animation
        float newSlideDuration = 0.05f;

        StartCoroutine(FillBoardRoutine(slideDuration, newSlideDuration));
    }

    private IEnumerator FillBoardRoutine(float slideDuration, float newSlideDuration)
    {
        // Step 1: Cascade existing tiles downward
        yield return StartCoroutine(CascadeTiles(slideDuration));
        yield return StartCoroutine(CascadeTiles(slideDuration));

        // Step 2: Fill remaining empty spaces at the top with new tiles
        yield return StartCoroutine(SpawnNewTiles(newSlideDuration));

        // Step 3: Assign priorities/z-index for proper rendering
        AssignTilePriorities();

        // Step 4: Update icons dynamically
        UpdateGroupIcons();

        Debug.Log("FillBoard complete! Board is fully updated.");
    }


    private IEnumerator CascadeTiles(float slideDuration)
    {
        List<TileController> movingTiles = new List<TileController>();

        for (int j = 0; j < columns; j++)
        {
            int emptyRow = -1;
            for (int i = rows - 1; i >= 0; i--)
            {
                if (board[i, j] == null && emptyRow == -1)
                {
                    emptyRow = i;
                }
                else if (board[i, j] != null && emptyRow != -1)
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
                if (board[i, j] == null) // Only fill gaps at the top
                {
                    int randomColor = Random.Range(0, colors);
                    // Spawn new tile above the board
                    GameObject newTile = Instantiate(tilePrefabs[randomColor], new Vector2(j, 1), Quaternion.identity);
                    newTile.transform.parent = this.transform;

                    // Assign color index
                    TileController tileController = newTile.GetComponent<TileController>();
                    if (tileController != null)
                    {
                        tileController.colorIndex = randomColor;
                        board[i, j] = newTile;

                        // Animate new tile sliding into position
                        yield return tileController.SlideToPosition(new Vector2(j, -i), newSlideDuration);
                    }
                    else
                    {
                        Debug.LogError($"TileController is missing on tile prefab at column={j}, row={i}");
                    }
                }
            }
        }

        Debug.Log("New tiles spawned.");
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
                    if (renderer != null)
                    {
                        renderer.sortingOrder = rows - i; // Higher rows get higher sorting orders
                        Debug.Log($"Assigned priority {rows - i} to tile at row={i}, col={j}");
                    }
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

            Debug.Log($"Clicked at row={row}, col={col}");


            if (row >= 0 && row < rows && col >= 0 && col < columns)
            {
                bool[,] visited = new bool[rows, columns]; // Track visited tiles
                TileController clickedTile = board[row, col].GetComponent<TileController>();
                if (clickedTile == null)
                {
                    Debug.LogError($"TileController missing on tile at row={row}, col={col}");
                    return;
                }

                int clickedColorIndex = clickedTile.colorIndex;
                List<GameObject> group = FindGroup(row, col, clickedColorIndex, visited);

                Debug.Log($"Final group size: {group.Count}");

                if (group.Count >= 2)
                {
                    RemoveGroup(group);

                    if (CheckDeadlock()) // Check for deadlock
                    {
                        ShuffleBoard();
                    }
                }
            }
        }
    }

    void UpdateGroupIcons()
    {
        Debug.Log("Updating group icons...");

        bool[,] visited = new bool[rows, columns];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (board[i, j] != null && !visited[i, j])
                {
                    List<GameObject> group = FindGroup(i, j, board[i, j].GetComponent<TileController>().colorIndex, visited);

                    foreach (var tile in group)
                    {
                        TileController tileController = tile.GetComponent<TileController>();
                        if (tileController != null)
                        {
                            tileController.UpdateIcon(group.Count);

                            // ✅ Fix: Log the actual tile's position correctly
                            Vector2Int tilePosition = new Vector2Int(Mathf.RoundToInt(tile.transform.position.x), Mathf.RoundToInt(-tile.transform.position.y));
                            Debug.Log($"Tile at {tilePosition.x},{tilePosition.y} assigned icon for group size {group.Count}");
                        }
                        else
                        {
                            Debug.LogError($"Tile at ({i},{j}) has no TileController!");
                        }
                    }
                }
            }
        }


        // Ensure single tiles revert to default icon
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (board[i, j] != null && !visited[i, j])
                {
                    TileController tileController = board[i, j].GetComponent<TileController>();
                    if (tileController != null)
                    {
                        tileController.UpdateIcon(1); // Reset to default icon
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

    // This method is different than a random shuffle because it guarantees a valid move by placing tiles next to each other
    // It then tries a weighted shuffle to prioritize tiles that are next to each other
    // As a last resort, it falls back to a random shuffle
    void ShuffleBoard()
    {
        if (!CheckDeadlock()) return; // Only shuffle if deadlock exists

        List<GameObject> tiles = new List<GameObject>();
        foreach (GameObject tile in board)
        {
            if (tile != null)
            {
                tiles.Add(tile);
            }
        }

        var colorGroups = tiles.GroupBy(t => t.GetComponent<TileController>().colorIndex)
                               .Where(g => g.Count() >= 2)
                               .ToList();

        if (colorGroups.Count == 0)
        {
            FallbackShuffle(tiles);
            return;
        }

        System.Random rng = new System.Random();
        var selectedGroup = colorGroups[rng.Next(colorGroups.Count)].ToList();
        GameObject tile1 = selectedGroup[0];
        GameObject tile2 = selectedGroup[1];
        tiles.Remove(tile1);
        tiles.Remove(tile2);

        List<Vector2Int[]> adjacentPairs = new List<Vector2Int[]>();
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (board[i, j] == null) continue;
                if (j + 1 < columns && board[i, j + 1] != null)
                    adjacentPairs.Add(new Vector2Int[] { new Vector2Int(i, j), new Vector2Int(i, j + 1) });
                if (i + 1 < rows && board[i + 1, j] != null)
                    adjacentPairs.Add(new Vector2Int[] { new Vector2Int(i, j), new Vector2Int(i + 1, j) });
            }
        }

        if (adjacentPairs.Count == 0)
        {
            FallbackShuffle(tiles, tile1, tile2);
            return;
        }

        Vector2Int[] selectedPair = adjacentPairs[rng.Next(adjacentPairs.Count)];
        Vector2Int pos1 = selectedPair[0];
        Vector2Int pos2 = selectedPair[1];

        board[pos1.x, pos1.y] = tile1;
        tile1.transform.position = new Vector2(pos1.y, -pos1.x);
        board[pos2.x, pos2.y] = tile2;
        tile2.transform.position = new Vector2(pos2.y, -pos2.x);

        WeightedShuffle(tiles);
        int index = 0;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if ((i == pos1.x && j == pos1.y) || (i == pos2.x && j == pos2.y))
                    continue;
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
            int swapIndex = rng.Next(Mathf.Max(0, i - 2), i + 1); // Restrict swaps to nearby positions
            (tiles[i], tiles[swapIndex]) = (tiles[swapIndex], tiles[i]);
        }
    }


    private void FallbackShuffle(List<GameObject> tiles, GameObject tile1 = null, GameObject tile2 = null)
    {
        if (tile1 != null) tiles.Add(tile1);
        if (tile2 != null) tiles.Add(tile2);
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

    public void RestartGame()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        // Load new settings
        rows = PlayerPrefs.GetInt("Rows", 10);
        columns = PlayerPrefs.GetInt("Columns", 12);
        int groupSizeA = PlayerPrefs.GetInt("GroupSizeA", 2);
        int groupSizeB = PlayerPrefs.GetInt("GroupSizeB", 3);
        int groupSizeC = PlayerPrefs.GetInt("GroupSizeC", 5);
        int colors = PlayerPrefs.GetInt("Colors", 6);

        // Apply group sizes to tiles
        TileController.SetGroupSizeThresholds(groupSizeA, groupSizeB, groupSizeC);

        // Apply color limit to board
        colors = Mathf.Clamp(colors, int.MinValue, 6);

        InitializeBoard(); // Recreate the board with new settings
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

        // Update TileController thresholds
        TileController.SetGroupSizeThresholds(groupSize_1, groupSize_2, groupSize_3);

        RestartGame(); // Restart with new settings
    }
}
