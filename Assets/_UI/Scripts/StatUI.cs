using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class StatUI
{
    [Tooltip("UI image that displays the stat sprite")]
    public Image image;

    [Tooltip("0 = worst, 5 = best")]
    public Sprite[] levels = new Sprite[6];

    public void Set(float normalizedValue)
    {
        if (image == null || levels == null || levels.Length == 0)
            return;

        int index = Mathf.RoundToInt(Mathf.Clamp01(normalizedValue) * (levels.Length - 1));
        index = Mathf.Clamp(index, 0, levels.Length - 1);

        image.sprite = levels[index];
    }
}