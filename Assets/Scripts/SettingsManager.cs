using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    public TMP_InputField RowsInput;
    public TMP_InputField ColumnsInput;
    public TMP_InputField GroupSizeAInput;
    public TMP_InputField GroupSizeBInput;
    public TMP_InputField GroupSizeCInput;
    public TMP_InputField ColorsInput;
    public Button ApplyButton;

    private BoardManager boardManager;

    void Start()
    {
        boardManager = FindFirstObjectByType<BoardManager>();

        // Load previous settings or set defaults
        RowsInput.text = PlayerPrefs.GetInt("Rows", 10).ToString();
        ColumnsInput.text = PlayerPrefs.GetInt("Columns", 12).ToString();
        GroupSizeAInput.text = PlayerPrefs.GetInt("GroupSizeA", 2).ToString();
        GroupSizeBInput.text = PlayerPrefs.GetInt("GroupSizeB", 3).ToString();
        GroupSizeCInput.text = PlayerPrefs.GetInt("GroupSizeC", 5).ToString();
        ColorsInput.text = PlayerPrefs.GetInt("Colors", 6).ToString();

        ApplyButton.onClick.AddListener(ApplySettings);
    }

    public void ApplySettings()
    {
        int newRows = ValidateInput(RowsInput.text, 2, 10, 10);
        int newColumns = ValidateInput(ColumnsInput.text, 2, 10, 12);
        int newGroupSizeA = ValidateInput(GroupSizeAInput.text, 2, 10, 2);
        int newGroupSizeB = ValidateInput(GroupSizeBInput.text, newGroupSizeA + 1, 10, 3);
        int newGroupSizeC = ValidateInput(GroupSizeCInput.text, newGroupSizeB + 1, 10, 5);
        int newColors = ValidateInput(ColorsInput.text, 1, 6, 6);

        Debug.Log($"New Settings -> Rows: {newRows}, Columns: {newColumns}, Colors: {newColors}, GroupSizeA: {newGroupSizeA}, GroupSizeB: {newGroupSizeB}, GroupSizeC: {newGroupSizeC}");

        // Save settings
        PlayerPrefs.SetInt("Rows", newRows);
        PlayerPrefs.SetInt("Columns", newColumns);
        PlayerPrefs.SetInt("GroupSizeA", newGroupSizeA);
        PlayerPrefs.SetInt("GroupSizeB", newGroupSizeB);
        PlayerPrefs.SetInt("GroupSizeC", newGroupSizeC);
        PlayerPrefs.SetInt("Colors", newColors);
        PlayerPrefs.Save();

        boardManager.UpdateSettings(newRows, newColumns, newColors, newGroupSizeA, newGroupSizeB, newGroupSizeC);
        // Apply changes
        boardManager.RestartGame();
    }

    private int ValidateInput(string input, int min, int max, int defaultValue)
    {
        if (string.IsNullOrEmpty(input))
            return defaultValue; // Use default if input is empty

        if (int.TryParse(input, out int value))
        {
            return Mathf.Clamp(value, min, max); // Clamp between min and max
        }

        return defaultValue; // Use default if parsing fails
    }
}
