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

    readonly Stack<Projectile> projectilePool = new Stack<Projectile>(32);
    readonly HashSet<Enemy> targetedThisBurst = new HashSet<Enemy>();
    WaitForSeconds cachedWaitGap;
    float cachedGapDuration = -1f;

    bool isFiring;
    float nextAllowedFireTime;

    void OnDisable()
    {
        StopAllCoroutines();
        isFiring = false;
    }

    protected override bool TryFire()
    {
        if (isFiring || projectilePrefab == null) return false;
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
        WaitForSeconds waitGap = GetWaitForSeconds(gap);
        targetedThisBurst.Clear();

        for (int i = 0; i < count; i++)
        {
            Vector2 origin = transform.position;
            Enemy target = Enemy.NearestUnvisited(origin, targetRange, targetedThisBurst, onlyTargetOnScreen);

            // Eyni düşmənə ikinci güllə getməsin: yeni hədəf yoxdursa dalğanı bitir
            if (target == null) break;

            targetedThisBurst.Add(target);
            Vector2 dir = CalculateAimDirection(origin, target, s.speed);

            Projectile proj = GetProjectile(origin);
            if (proj != null)
            {
                proj.Launch(dir, s.speed, dmg, s.pierce, s.duration, SpellId, s.size, this);
            }

            if (i < count - 1 && waitGap != null)
            {
                yield return waitGap;
            }
        }

        targetedThisBurst.Clear();

        if (IsEvolved)
        {
            nextAllowedFireTime = Time.time + evoMinCycleTime;
        }

        isFiring = false;
    }

    WaitForSeconds GetWaitForSeconds(float seconds)
    {
        if (seconds <= 0f) return null;
        if (cachedWaitGap == null || !Mathf.Approximately(cachedGapDuration, seconds))
        {
            cachedGapDuration = seconds;
            cachedWaitGap = new WaitForSeconds(seconds);
        }
        return cachedWaitGap;
    }

    Projectile GetProjectile(Vector2 origin)
    {
        while (projectilePool.Count > 0)
        {
            Projectile pooled = projectilePool.Pop();
            if (pooled == null) continue;
            pooled.transform.SetPositionAndRotation(origin, Quaternion.identity);
            pooled.gameObject.SetActive(true);
            return pooled;
        }

        var go = Instantiate(projectilePrefab, origin, Quaternion.identity);
        return go.GetComponent<Projectile>();
    }

    public void ReturnProjectile(Projectile proj)
    {
        if (proj == null) return;
        proj.gameObject.SetActive(false);
        projectilePool.Push(proj);
    }

    Vector2 CalculateAimDirection(Vector2 origin, Enemy target, float projectileSpeed)
    {
        Vector2 targetPos = target.Position;
        Vector2 toTarget = targetPos - origin;
        float dist = toTarget.magnitude;

        if (leadMovingTargets && dist > closeRangeDirectAim && projectileSpeed > 0.1f)
        {
            float flightTime = Mathf.Clamp(dist / projectileSpeed, 0f, 0.35f);
            Vector2 predictedPos = targetPos + target.Velocity * flightTime;
            Vector2 toPredicted = predictedPos - origin;
            if (toPredicted.sqrMagnitude > 0.0001f)
            {
                return toPredicted.normalized;
            }
        }

        return dist > 0.0001f ? toTarget / dist : Vector2.right;
    }
}