using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationBarManager : MonoBehaviour
{
    public NavItem[] navItems;
    public int defaultIndex = 0; // Home = 0

    void Start()
    {
        // Set Home là default
        for (int i = 0; i < navItems.Length; i++)
            navItems[i].SetActive(i == defaultIndex, instant: true);
    }

    public void SelectItem(int index)
    {
        for (int i = 0; i < navItems.Length; i++)
            navItems[i].SetActive(i == index);
    }
}
