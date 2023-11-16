using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

[ExecuteInEditMode]
public class UniformUI : MonoBehaviour
{
    public Color blockColour;

    [Space]
    public UniformUI[] uniformUis;
    public TMP_Text[] textElements;
    public Image[] imageElements;

    //keeps the colour scheme of UI blocks the same
    private void Update()
    {
        try
        {
            for (int i = 0; i < uniformUis.Length; i++)
            {
                uniformUis[i].blockColour = blockColour;
            }
        }
        catch(NullReferenceException) { }

        try
        {
            for (int i = 0; i < textElements.Length; i++)
            {
                textElements[i].color = blockColour;
            }
        }
        catch(NullReferenceException) { }

        try
        {
            if (imageElements.Length > 0)
            {
                for (int i = 0; i < imageElements.Length; i++)
                {
                    imageElements[i].color = blockColour;
                }
            }
        }
        catch (NullReferenceException) { }
    }
}