using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class resetScrollBar : MonoBehaviour
{
    private Scrollbar SB;

    private void Start()
    {
        SB = GetComponent<Scrollbar>();

        SB.value = 1;
    }
}
