using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Affichage d'UN slot du menu de sélection (8 instances). Aucune logique : le menu le pilote.</summary>
public class PlayerSlotUI : MonoBehaviour
{
    [SerializeField] private GameObject emptyRoot;     // "Appuyez pour rejoindre"
    [SerializeField] private GameObject joinedRoot;    // contenu quand un joueur occupe le slot
    [SerializeField] private Image colorImage;         // aperçu de la couleur choisie
    [SerializeField] private TMP_Text titleText;       // "J1"
    [SerializeField] private TMP_Text inputText;       // "Clavier ZQSD", "Manette 2 - Stick droit"
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject arrowsRoot;    // flèches gauche / droite, visibles tant que non prêt
    [SerializeField] private GameObject readyBadge;    // coche "PRÊT"
    [SerializeField] private string notReadyLabel = "Choisis ta couleur";
    [SerializeField] private string readyLabel = "PRÊT";

    public void ShowEmpty(int slotNumber)
    {
        if (emptyRoot != null) emptyRoot.SetActive(true);
        if (joinedRoot != null) joinedRoot.SetActive(false);
    }

    public void ShowJoined(int slotNumber, Color color, string inputLabel, bool ready)
    {
        if (emptyRoot != null) emptyRoot.SetActive(false);
        if (joinedRoot != null) joinedRoot.SetActive(true);

        if (colorImage != null) colorImage.color = color;
        if (titleText != null) titleText.text = "J" + slotNumber;
        if (inputText != null) inputText.text = inputLabel;
        if (statusText != null) statusText.text = ready ? readyLabel : notReadyLabel;
        if (arrowsRoot != null) arrowsRoot.SetActive(!ready);
        if (readyBadge != null) readyBadge.SetActive(ready);
    }
}