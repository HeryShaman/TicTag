using UnityEngine;

public class ConveyorBelt : MonoBehaviour {
    public float amount = 0.1f;

    private void OnTriggerStay(Collider other) {
        HotPotatoCharacter hotPotatoCharacter = other.GetComponent<HotPotatoCharacter>();

        if (hotPotatoCharacter != null) {
            hotPotatoCharacter.MoveOnConveyor(transform.right * amount);
        }
    }
}