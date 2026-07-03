using System;
using UnityEngine;

public class SkillInventory : SingletonPersistent<SkillInventory>
{
    private const string KeyPrefix = "skill_charges_";

    public int GetCharges(SkillType type) =>
        PlayerPrefs.GetInt(KeyPrefix + type, 0);

    public void AddCharges(SkillType type, int amount)
    {
        if (amount <= 0) return;
        int next = GetCharges(type) + amount;
        PlayerPrefs.SetInt(KeyPrefix + type, next);
        PlayerPrefs.Save();
        OnChargesChanged?.Invoke(type, next);
    }

    public bool TryConsume(SkillType type)
    {
        int current = GetCharges(type);
        if (current <= 0) return false;
        PlayerPrefs.SetInt(KeyPrefix + type, current - 1);
        PlayerPrefs.Save();
        OnChargesChanged?.Invoke(type, current - 1);
        return true;
    }

    public event Action<SkillType, int> OnChargesChanged;
}