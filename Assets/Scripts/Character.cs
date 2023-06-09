using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField]
    protected float maxHP = 100;

    protected float currentHP;

    protected virtual void Start()
    {
        currentHP = maxHP;
    }

    public virtual void RecieveDamage(float damage)
    {
        currentHP -= damage;

        if (currentHP < 0)
            Death();
    }

    public virtual void Death() { }
}