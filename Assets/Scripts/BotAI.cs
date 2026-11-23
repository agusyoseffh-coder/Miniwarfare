using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent), typeof(Health))]
public class BotAI : MonoBehaviour
{
    public float sightRange = 45f, fireInterval = 0.5f, damage = 8f, accuracy = 0.35f, keepDistance = 10f;

    NavMeshAgent agent;
    Health me, target;
    Collider targetCol;
    float nextThink, nextShot;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        me = GetComponent<Health>();
        agent.enabled = true; // enabled here so the NavMesh already exists
    }

    void Update()
    {
        if (agent == null || !agent.isOnNavMesh) return;

        if (Time.time >= nextThink)
        {
            nextThink = Time.time + 0.4f;
            FindTarget();
            if (target != null) agent.SetDestination(target.transform.position);
        }
        if (target == null || !target.gameObject.activeInHierarchy) return;

        Vector3 eye = transform.position;
        Vector3 tp = targetCol != null ? targetCol.bounds.center : target.transform.position;
        float d = Vector3.Distance(eye, tp);
        agent.isStopped = d < keepDistance;

        if (d < sightRange && Time.time >= nextShot)
        {
            nextShot = Time.time + fireInterval;
            Vector3 dir = tp - eye;
            if (Physics.Raycast(eye, dir, out RaycastHit hit, sightRange) &&
                hit.collider.GetComponentInParent<Health>() == target)
            {
                Vector3 flat = new Vector3(dir.x, 0f, dir.z);
                if (flat.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(flat);
                if (Random.value < accuracy) target.Damage(damage);
            }
        }
    }

    void FindTarget()
    {
        float best = float.MaxValue;
        target = null;
        foreach (var h in Health.All)
        {
            if (h.team == me.team) continue;
            float d = (h.transform.position - transform.position).sqrMagnitude;
            if (d < best) { best = d; target = h; }
        }
        targetCol = target != null ? target.GetComponent<Collider>() : null;
    }
}
