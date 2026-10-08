using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private HotPotatoCharacter playerPrefab;
    [SerializeField] private Transform[] spawnPoints = new Transform[InputReader.PlayerCount];
    [SerializeField] private PlayerPalette palette;
    [SerializeField] private Transform playersParent;

    // Utilisé uniquement si la scène de jeu est lancée directement (aucune session issue du menu).
    [SerializeField, Range(1, InputReader.PlayerCount)] private int fallbackPlayerCount = 2;

    public List<HotPotatoCharacter> SpawnAll()
    {
        var spawned = new List<HotPotatoCharacter>();

        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] playerPrefab manquant.", this);
            return spawned;
        }

        IReadOnlyList<GameSession.PlayerEntry> entries =
            GameSession.HasPlayers ? GameSession.Players : BuildFallbackEntries();

        foreach (GameSession.PlayerEntry entry in entries)
        {
            Transform point = (entry.slotIndex >= 0 && entry.slotIndex < spawnPoints.Length)
                ? spawnPoints[entry.slotIndex]
                : null;

            if (point == null)
                Debug.LogWarning($"[PlayerSpawner] Pas de spawn point pour le slot {entry.slotIndex}.", this);

            Vector3 position = point != null ? point.position : transform.position;
            Quaternion rotation = point != null ? point.rotation : Quaternion.identity;

            HotPotatoCharacter character = Instantiate(playerPrefab, position, rotation, playersParent);
            character.name = $"Player {entry.slotIndex + 1}";

            // Index de contrôle : c'est lui, et non l'ordre d'arrivée, qui désigne les touches/le stick à lire.
            InputReader reader = character.GetComponent<InputReader>();
            if (reader == null) reader = character.GetComponentInChildren<InputReader>();
            if (reader != null) reader.SetPlayerIndex(entry.inputIndex);
            else Debug.LogError($"[PlayerSpawner] Pas d'InputReader sur {character.name}.", character);

            PlayerColorApplier applier = character.GetComponent<PlayerColorApplier>();
            if (applier == null) applier = character.gameObject.AddComponent<PlayerColorApplier>();
            if (palette != null) applier.Apply(palette.GetColor(entry.colorIndex));

            spawned.Add(character);
        }

        return spawned;
    }

    private List<GameSession.PlayerEntry> BuildFallbackEntries()
    {
        var list = new List<GameSession.PlayerEntry>();

        for (int i = 0; i < fallbackPlayerCount; i++)
        {
            list.Add(new GameSession.PlayerEntry { slotIndex = i, inputIndex = i, colorIndex = i });
        }

        return list;
    }
}