using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using LitJson;
using System.Threading.Tasks;

public class LoadingScreen : MonoBehaviour
{
    public TMP_Text loadingText;
    public string loadingLinesFile = "/Text/loadingLines";
    public float lineRefreshRate = 0.8f;

    private List<string> loadingLines = new List<string>();

    private int currentLineIndex;

    private void Start()
    {
        JsonData json = JsonMapper.ToObject(Resources.Load<TextAsset>(loadingLinesFile).ToString());

        for (int i = 0; i < json.Count; i++)
        {
            loadingLines.Add(json[i].ToString());
        }

        RefreshLoadingLine();
    }

    private async void RefreshLoadingLine()
    {
        while (true)
        {
            loadingText.text = GetNewLine();

            await Task.Delay((int)lineRefreshRate * 1000);
        }
    }

    private string GetNewLine()
    {
        int index = Random.Range(0, loadingLines.Count);

        while(index == currentLineIndex)
        {
            index = Random.Range(0, loadingLines.Count);
        }

        currentLineIndex = index;

        return loadingLines[index];
    }
}
