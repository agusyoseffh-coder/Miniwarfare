using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Transform cam;
    public Team team = Team.Blue;
    public float damage = 25f, fireRate = 8f, range = 120f, reloadTime = 1.5f;
    public int magSize = 30;

    public int Ammo { get; private set; }
    public bool Reloading { get; private set; }
    float nextShot;

    void OnEnable() { Ammo = magSize; Reloading = false; }

    public void Fire()
    {
        if (Reloading || Time.time < nextShot) return;
        if (Ammo <= 0) { Reload(); return; }

        nextShot = Time.time + 1f / fireRate;
        Ammo--;

        if (Physics.Raycast(cam.position, cam.forward, out RaycastHit hit, range))
        {
            var h = hit.collider.GetComponentInParent<Health>();
            if (h != null && h.team != team) h.Damage(damage);
        }
    }

    public void Reload()
    {
        if (Reloading || Ammo == magSize) return;
        StartCoroutine(ReloadRoutine());
    }

    IEnumerator ReloadRoutine()
    {
        Reloading = true;
        yield return new WaitForSeconds(reloadTime);
        Ammo = magSize;
        Reloading = false;
    }
}
