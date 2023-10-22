using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[ExecuteInEditMode]
public class UIThemeObject : MonoBehaviour
{
    private UIThemeManager manager;

    public Color lightMode = Color.white;
    public Color darkMode = new Color(0.1215686f, 0.1215686f, 0.1215686f);

    private TMP_Text text;
    private Image img;


    private bool isDark;

    private void Start()
    {
        manager = FindObjectOfType<UIThemeManager>();

        manager.onDarkMode += SetDarkMode;
        manager.onLightMode += SetLightMode;

        if (TryGetComponent<TMP_Text>(out TMP_Text c))
        {
            text = c;
        }
        else if (TryGetComponent<Image>(out Image i))
        {
            img = i;
        }
    }


    private void SetDarkMode()
    {
        isDark = true;

        UpdateUI();
    }

    private void SetLightMode()
    {
        isDark = false;

        UpdateUI();
    }


    private void UpdateUI()
    {
        if (text != null)
        {
            text.color = isDark ? darkMode : lightMode;
        }
        else if (img != null)
        {
            img.color = isDark ? darkMode : lightMode;
        }
    }
}