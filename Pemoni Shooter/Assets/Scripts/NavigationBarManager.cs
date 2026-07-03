using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigationBarManager : MonoBehaviour
{
    public static NavigationBarManager Instance { get; private set; }
    public NavItem[] navItems;
    public int defaultIndex = 0; // Home = 0
    private MainMenuUI mainMenuUI;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Set Home là default
        for (int i = 0; i < navItems.Length; i++)
        {
            navItems[i].SetActive(i == defaultIndex, instant: true);
            if(navItems[i].tab != null)
            {
                navItems[i].tab.SetActive(i == defaultIndex);
            }
        }
        mainMenuUI = FindObjectOfType<MainMenuUI>();
    }


    private void HideOtherPanel()
    {
        for (int i = 0; i < navItems.Length; i++)
        {
            NavItem navItem = navItems[i];
            if (navItem.tab.activeSelf)
            {
                navItem.tab.SetActive(false);
            }
        }
    }

    public void SelectItem(int index)
    {
        for (int i = 0; i < navItems.Length; i++)
        {
            navItems[i].SetActive(i == index);
            if (navItems[i].tab != null) 
            {
                if(mainMenuUI != null)
                {
                    mainMenuUI.CloseAllPanel();
                }
                navItems[i].tab.SetActive(i==index);
            }
        }

    }
}
