using UnityEngine;

public class ConveyorBelt : MonoBehaviour {
    public float amount = 0.1f;

    private void OnTriggerStay(Collider other) {
        int dir = Mathf.RoundToInt(transform.eulerAngles.y / 90f) % 4;
        HotPotatoCharacter hotPotatoCharacter = other.GetComponent<HotPotatoCharacter>();

        if (hotPotatoCharacter != null) {
            hotPotatoCharacter.ForceMove(dir, amount);
        }
    }
}
