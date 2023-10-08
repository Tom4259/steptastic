using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if !UNITY_EDITOR
//using Debug = Sisus.Debugging.Debug;
#endif

public class UniformUI : MonoBehaviour
{
    public TMP_FontAsset TitleFont;
    public TMP_FontAsset TextFont;

    [Space]
    public float TitleSize = 60;
    public float TextSize = 30;
}
