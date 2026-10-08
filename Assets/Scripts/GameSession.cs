using System.Collections.Generic;
using UnityEngine;

public static class GameSession
{
    [System.Serializable]
    public struct PlayerEntry
    {
        public int slotIndex;   // ordre d'arrivée dans le menu : décide le point d'apparition (spawn n°slotIndex)
        public int inputIndex;  // zone de contrôle physique : donné à InputReader.SetPlayerIndex
        public int colorIndex;  // index dans la PlayerPalette
    }

    private static readonly List<PlayerEntry> _players = new List<PlayerEntry>();

    public static IReadOnlyList<PlayerEntry> Players => _players;
    public static bool HasPlayers => _players.Count > 0;

    public static void Clear()
    {
        _players.Clear();
    }

    public static void SetPlayers(IEnumerable<PlayerEntry> entries)
    {
        _players.Clear();
        _players.AddRange(entries);
        _players.Sort((a, b) => a.slotIndex.CompareTo(b.slotIndex));
    }

    // Si "Enter Play Mode Options" désactive le domain reload, les statiques survivraient entre deux Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlayMode()
    {
        _players.Clear();
    }
}