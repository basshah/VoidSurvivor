using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StarshotSpell : Spell
{
    public GameObject projectilePrefab;
    public float targetRange = 7.5f;
    public bool onlyTargetOnScreen = true;

    [Header("Magic Bolt Feel")]
    public float burstInterval = 0.08f;
    public float evoBurstInterval = 0.06f;
    public float evoMinCycleTime = 0.22f;
    public bool leadMovingTargets = true;
    public float closeRangeDirectAim = 1.8f;

    bool isFiring;
    float nextAllowedFireTime;

    void OnDisable()
    {
        isFiring = false;
    }

    protected override bool TryFire()
    {
        if (isFiring) return false;
        if (IsEvolved && Time.time < nextAllowedFireTime) return false;

        Vector2 origin = transform.position;
        Enemy firstTarget = Enemy.Nearest(origin, targetRange, onlyTargetOnScreen);
        if (firstTarget == null) return false;

        StartCoroutine(FireBurstRoutine(Current, Damage));
        return true;
    }

    IEnumerator FireBurstRoutine(SpellLevelStats s, float dmg)
    {
        isFiring = true;

        int count = Mathf.Max(1, s.count);
        float gap = IsEvolved ? evoBurstInterval : burstInterval;
        var targetedThisBurst = new HashSet<Enemy>();

        for (int i = 0; i < count; i++)
        {
            Vector2 origin = transform.position;
            Enemy target = Enemy.NearestUnvisited(origin, targetRange, targetedThisBurst, onlyTargetOnScreen);

            // Eyni düşmənə ikinci güllə getməsin: yeni hədəf yoxdursa dalğanı bitir
            if (target == null) break;

            targetedThisBurst.Add(target);
            Vector2 dir = CalculateAimDirection(origin, target, s.speed);

            var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
            var proj = go.GetComponent<Projectile>();
            proj.Launch(dir, s.speed, dmg, s.pierce, s.duration, data.id, s.size);

            if (i < count - 1 && gap > 0f)
            {
                yield return new WaitForSeconds(gap);
            }
        }

        if (IsEvolved)
        {
            nextAllowedFireTime = Time.time + evoMinCycleTime;
        }

        isFiring = false;
    }

    Vector2 CalculateAimDirection(Vector2 origin, Enemy target, float projectileSpeed)
    {
        Vector2 targetPos = target.transform.position;
        Vector2 toTarget = targetPos - origin;
        float dist = toTarget.magnitude;

        if (leadMovingTargets && dist > closeRangeDirectAim && projectileSpeed > 0.1f)
        {
            var enemyRb = target.GetComponent<Rigidbody2D>();
            if (enemyRb != null)
            {
                float flightTime = Mathf.Clamp(dist / projectileSpeed, 0f, 0.35f);
                Vector2 predictedPos = targetPos + enemyRb.linearVelocity * flightTime;
                Vector2 toPredicted = predictedPos - origin;
                if (toPredicted.sqrMagnitude > 0.0001f)
                {
                    return toPredicted.normalized;
                }
            }
        }

        return dist > 0.0001f ? toTarget / dist : Vector2.right;
    }
}