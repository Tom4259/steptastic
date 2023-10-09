using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

[ExecuteInEditMode]
public class UniformUI : MonoBehaviour
{
    public Color blockColour;

    public TMP_Text[] ignoredTextElements;
    public Image[] ignoredImageElements;

    private List<TMP_Text> textElements = new List<TMP_Text>();
    private List<Image> imageElements = new List<Image>();

    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            if (transform.GetChild(i).TryGetComponent<TMP_Text>(out TMP_Text txt))
            {
                if (!ignoredTextElements.Contains(txt))
                {
                    textElements.Add(txt);
                }
            }
            else if (transform.GetChild(i).TryGetComponent<Image>(out Image img))
            {
                if (!ignoredImageElements.Contains(img))
                {
                    imageElements.Add(img);
                }
            }
        }
    }

    private void Update()
    {
        for (int i = 0; i < textElements.Count; i++)
        {
            textElements[i].color = blockColour;
        }
        for (int i = 0; i < imageElements.Count; i++)
        {
            imageElements[i].color = blockColour;
        }
    }
}
