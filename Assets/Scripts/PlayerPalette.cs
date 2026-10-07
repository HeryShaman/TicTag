using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPalette", menuName = "HotPotato/Player Palette")]
public class PlayerPalette : ScriptableObject
{
    [SerializeField] private Color[] colors =
    {
        new Color(0.20f, 0.45f, 1.00f), // 0 bleu
        new Color(0.95f, 0.25f, 0.25f), // 1 rouge
        new Color(0.30f, 0.85f, 0.35f), // 2 vert
        new Color(1.00f, 0.85f, 0.20f), // 3 jaune
        new Color(0.65f, 0.35f, 0.95f), // 4 violet
        new Color(1.00f, 0.55f, 0.15f), // 5 orange
        new Color(0.20f, 0.90f, 0.90f), // 6 cyan
        new Color(1.00f, 0.50f, 0.80f)  // 7 rose
    };

    public int Count => colors != null ? colors.Length : 0;

    public Color GetColor(int index)
    {
        if (Count == 0) return Color.white;
        return colors[Mathf.Clamp(index, 0, Count - 1)];
    }
}