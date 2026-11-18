using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RecordPanel : MonoBehaviour
{
    [Serializable]
    public class Row
    {
        public string id;          // DamageTracker-dəki sehr adı: starshot, moonlets...
        public RectTransform rect;
        public TMP_Text value;
        public RectTransform fill;
    }

    public RectTransform container; // VerticalLayoutGroup olan "Rows" obyekti
    public List<Row> rows = new List<Row>();

    void OnEnable() { Refresh(); }

    public void Refresh()
    {
        if (rows.Count == 0) return;

        var sorted = new List<Row>(rows);
        sorted.Sort((a, b) =>
        {
            int c = DamageTracker.Total(b.id).CompareTo(DamageTracker.Total(a.id));
            return c != 0 ? c : rows.IndexOf(a).CompareTo(rows.IndexOf(b));
        });

        double max = Math.Max(1.0, DamageTracker.Total(sorted[0].id));
        for (int i = 0; i < sorted.Count; i++)
        {
            var r = sorted[i];
            double total = DamageTracker.Total(r.id);
            r.rect.SetSiblingIndex(i);
            r.value.text = total.ToString("N0", CultureInfo.InvariantCulture);
            r.fill.anchorMax = new Vector2(Mathf.Clamp01((float)(total / max)), 1f);
        }

        if (container != null) LayoutRebuilder.ForceRebuildLayoutImmediate(container);
    }
}