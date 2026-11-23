using System;
using System.Collections.Generic;
using UnityEngine;

public enum Team { Blue, Red }

public class Health : MonoBehaviour
{
    public static readonly List<Health> All = new List<Health>();

    public Team team;
    public float maxHp = 100f;
    public float hp;
    public event Action<Health> OnDeath;

    void OnEnable() { hp = maxHp; All.Add(this); }
    void OnDisable() { All.Remove(this); }

    public void Damage(float amount)
    {
        if (hp <= 0f) return;
        hp -= amount;
        if (hp <= 0f)
        {
            hp = 0f;
            OnDeath?.Invoke(this);
        }
    }
}
