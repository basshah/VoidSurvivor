using System.Collections.Generic;
using UnityEngine;

public static class DamageTracker
{
    public class Entry
    {
        public string id;
        public string name;
        public Color color;
        public double total;
    }

    static readonly Dictionary<string, Entry> map = new Dictionary<string, Entry>();
    static readonly List<Entry> list = new List<Entry>();
    public static IReadOnlyList<Entry> All => list;

    public static void Reset()
    {
        map.Clear();
        list.Clear();
        Register("starshot", "Starshot", new Color(0.3f, 0.9f, 1f));
        Register("moonlets", "Moonlets", new Color(0.8f, 0.8f, 1f));
        Register("pulsar", "Pulsar", new Color(0.75f, 0.45f, 1f));
        Register("supernova", "Supernova", new Color(1f, 0.6f, 0.2f));
    }

    static void Register(string id, string name, Color color)
    {
        var e = new Entry { id = id, name = name, color = color };
        map[id] = e;
        list.Add(e);
    }

    public static void Add(string id, float amount)
    {
        if (map.TryGetValue(id, out var e)) e.total += amount;
    }

    public static double Total(string id)
    {
        return map.TryGetValue(id, out var e) ? e.total : 0.0;
    }
}