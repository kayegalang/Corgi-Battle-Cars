using UnityEngine;
using UnityEngine.UI;
using _Cars.ScriptableObjects;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Archetypes")]
    [SerializeField] private CarStats[] carTypes;      // Tank, Speedster, Aerialist, Bruiser
    [SerializeField] private Sprite[] carSprites;      // Matching order with carTypes

    [Header("Preview")]
    [SerializeField] private Image previewImage;

    [Header("Bars (Image type = Filled)")]
    [SerializeField] private Image hpBar;
    [SerializeField] private Image speedBar;
    [SerializeField] private Image jumpBar;
    [SerializeField] private Image accelBar;

    private int index;

    private const string SelectedIndexKey = "SelectedCarTypeIndex";

    private void Start()
    {
        index = PlayerPrefs.GetInt(SelectedIndexKey, 0);
        index = Mathf.Clamp(index, 0, carTypes.Length - 1);
        Apply();
    }

    public void Next()
    {
        index = (index + 1) % carTypes.Length;
        Apply();
        Save();
    }

    public void Prev()
    {
        index = (index - 1 + carTypes.Length) % carTypes.Length;
        Apply();
        Save();
    }

    private void Apply()
    {
        var stats = carTypes[index];

        if (previewImage != null && carSprites != null && index < carSprites.Length)
            previewImage.sprite = carSprites[index];

        if (hpBar != null) hpBar.fillAmount = stats.HealthStat / 100f;
        if (speedBar != null) speedBar.fillAmount = stats.SpeedStat / 100f;
        if (jumpBar != null) jumpBar.fillAmount = stats.JumpForceStat / 100f;
        if (accelBar != null) accelBar.fillAmount = stats.AccelerationStat / 100f;
    }

    private void Save()
    {
        PlayerPrefs.SetInt(SelectedIndexKey, index);
        PlayerPrefs.Save();
    }

    // For your main engineer later:
    public int GetSelectedIndex() => index;
    public CarStats GetSelectedStats() => carTypes[index];
}