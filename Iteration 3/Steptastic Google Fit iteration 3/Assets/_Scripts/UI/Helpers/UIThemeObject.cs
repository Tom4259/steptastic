using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIThemeObject : MonoBehaviour
{
    private UIThemeManager manager;

    public Color lightMode;
    public Color darkMode;

    private TMP_Text text;
    private Image img;

    private void Start()
    {
        manager = FindObjectOfType<UIThemeManager>();

        if (TryGetComponent<TMP_Text>(out TMP_Text c))
        {
            text = c;
        }
        else if (TryGetComponent<Image>(out Image i))
        {
            img = i;
        }
    }

    private void Update()
    {
        bool isDarkMode = manager.IsDarkMode();

        if (text != null)
        {
            text.color = isDarkMode ? darkMode : lightMode;
        }
        else if(img != null)
        {
            img.color = isDarkMode ? darkMode : lightMode;
        }
    }
}
