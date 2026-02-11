using UnityEngine;
using System.Collections;

public class TileController : MonoBehaviour
{

    public Sprite defaultSprite; // Default icon for groups smaller than A + 1
    public Sprite iconA;         // Icon for groups larger or equal to A but smaller than B + 1
    public Sprite iconB;         // Icon for groups larger than B but smaller than C + 1
    public Sprite iconC;         // Icon for groups larger than C + 1
    public int colorIndex;       // Index representing the tile's color
    public bool isMoving = false;

    public static int groupSizeA = 2;
    public static int groupSizeB = 3;
    public static int groupSizeC = 5;
    private BoardManager boardManager;


    public static void SetGroupSizeThresholds(int a, int b, int c)
    {
        groupSizeA = a;
        groupSizeB = b;
        groupSizeC = c;
    }

    private SpriteRenderer spriteRenderer;

    void Start()
    {
        boardManager = FindFirstObjectByType<BoardManager>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer component is missing on " + gameObject.name);
            return;
        }
        spriteRenderer.sprite = defaultSprite; // Set the default sprite
    }

    // Coroutine for sliding animation
    public IEnumerator SlideToPosition(Vector2 targetPosition, float duration)
    {
        isMoving = true;
        Vector2 startPosition = transform.position;
        float elapsedTime = 0;

        while (elapsedTime < duration)
        {
            // Smooth movement using Lerp
            transform.position = Vector2.Lerp(startPosition, targetPosition, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null; // Wait for the next frame
        }

        // Snap to final position to avoid minor floating-point inaccuracies
        transform.position = targetPosition;
        isMoving = false;
    }

    // Update the icon based on the group size
    public void UpdateIcon(int groupSize)
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer is null");
            return;
        }

        Debug.Log($"Updating icon for tile at {transform.position} with group size: {groupSize}");

        if (groupSize >= groupSizeC + 1)
        {
            spriteRenderer.sprite = iconC;
        }
        else if (groupSize >= groupSizeB)
        {
            spriteRenderer.sprite = iconB;
        }
        else if (groupSize >= groupSizeA)
        {
            spriteRenderer.sprite = iconA;
        }
        else
        {
            spriteRenderer.sprite = defaultSprite;
        }

        Debug.Log($"Assigned sprite: {spriteRenderer.sprite.name}");
    }

}
