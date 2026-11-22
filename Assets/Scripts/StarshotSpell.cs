using UnityEngine;

public class StarshotSpell : Spell
{
    public GameObject projectilePrefab;
    public float targetRange = 11f;
    public float spreadAngle = 10f;

    protected override bool TryFire()
    {
        Vector2 origin = transform.position;
        Enemy target = Enemy.Nearest(origin, targetRange);
        if (target == null) return false;

        Vector2 dir = ((Vector2)target.transform.position - origin).normalized;
        SpellLevelStats s = Current;
        int n = Mathf.Max(1, s.count);
        float start = -spreadAngle * (n - 1) * 0.5f;

        for (int i = 0; i < n; i++)
        {
            Vector2 d = (Vector2)(Quaternion.Euler(0f, 0f, start + i * spreadAngle) * (Vector3)dir);
            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
            go.GetComponent<Projectile>().Launch(d, s.speed, Damage, s.pierce, s.duration, data.id, s.size);
        }
        return true;
    }
}