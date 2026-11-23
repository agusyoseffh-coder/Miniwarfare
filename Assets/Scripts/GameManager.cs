using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    public Transform environment, blueSpawn, redSpawn;
    public Health player;
    public Weapon playerWeapon;
    public int killsToWin = 30;

    int blueKills, redKills;
    string winText;
    GUIStyle label, center;

    void Awake()
    {
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Time.timeScale = 1f;
        BuildNavMesh();
    }

    void Start()
    {
        foreach (var h in new List<Health>(Health.All)) h.OnDeath += HandleDeath;
    }

    void BuildNavMesh()
    {
        var sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(environment, ~0, NavMeshCollectGeometry.PhysicsColliders,
            0, new List<NavMeshBuildMarkup>(), sources);
        var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0), sources,
            new Bounds(Vector3.zero, new Vector3(120f, 30f, 120f)), Vector3.zero, Quaternion.identity);
        NavMesh.AddNavMeshData(data);
    }

    void HandleDeath(Health h)
    {
        if (winText != null) return;
        if (h.team == Team.Red) blueKills++; else redKills++;

        if (blueKills >= killsToWin) Win("BLUE TEAM WINS (YOU)");
        else if (redKills >= killsToWin) Win("RED TEAM WINS");
        else StartCoroutine(Respawn(h));
    }

    void Win(string text)
    {
        winText = text;
        Time.timeScale = 0f;
    }

    IEnumerator Respawn(Health h)
    {
        var go = h.gameObject;
        go.SetActive(false);
        yield return new WaitForSeconds(3f);
        if (winText != null) yield break;

        Transform sp = h.team == Team.Blue ? blueSpawn : redSpawn;
        Vector3 p = sp.position + new Vector3(Random.Range(-6f, 6f), 0f, Random.Range(-3f, 3f));
        var cc = go.GetComponent<CharacterController>();
        var agent = go.GetComponent<NavMeshAgent>();

        if (cc != null) cc.enabled = false;
        go.transform.position = p + Vector3.up * (agent != null ? 1f : 0.1f);
        go.SetActive(true);
        if (cc != null) cc.enabled = true;
        if (agent != null) agent.Warp(p);
    }

    void OnGUI()
    {
        if (label == null)
        {
            int fs = Mathf.RoundToInt(Screen.height * 0.04f);
            label = new GUIStyle(GUI.skin.label) { fontSize = fs, fontStyle = FontStyle.Bold };
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
        }
        float W = Screen.width, H = Screen.height;

        GUI.Label(new Rect(0, 8, W, H * 0.08f), "BLUE " + blueKills + "  -  " + redKills + " RED   (first to " + killsToWin + ")", center);
        GUI.Label(new Rect(W * 0.5f - 20, H * 0.5f - 20, 40, 40), "+", center);

        if (player != null && playerWeapon != null)
        {
            GUI.Label(new Rect(16, 8, W * 0.3f, H * 0.08f), "HP " + Mathf.CeilToInt(player.hp), label);
            string ammo = playerWeapon.Reloading ? "RELOADING" : playerWeapon.Ammo + "/" + playerWeapon.magSize;
            GUI.Label(new Rect(W * 0.72f, 8, W * 0.28f, H * 0.08f), ammo, label);

            if (!player.gameObject.activeSelf)
                GUI.Label(new Rect(0, 0, W, H), "RESPAWNING...", center);
        }

        if (winText != null)
        {
            GUI.Label(new Rect(0, H * 0.3f, W, H * 0.15f), winText, center);
            if (GUI.Button(new Rect(W * 0.35f, H * 0.5f, W * 0.3f, H * 0.15f), "PLAY AGAIN"))
                SceneManager.LoadScene(0);
        }
    }
}
