using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Michsky.MUIP;

public class ProfilePanel : MonoBehaviour
{
    private SetupManager setupManager;

    public RectTransform avatarSelectionMarker;
    public CustomInputField nicknameInput;

    [Space]
    public ModalWindowManager enterNicknameWindow;
    public ModalWindowManager selectAvatarWindow;

    [Space(30)]
    public ButtonManager continueButton;

    private int selectedAvatarIndex = -1;

    private void Start()
    {
        setupManager = GetComponentInParent<SetupManager>();
    }


    //updates the avater indicator to show the selected choice
    public void SetNewAvatarIndex(int index)
    {
        avatarSelectionMarker.gameObject.SetActive(true);

        selectedAvatarIndex = index;

        if(index == 0)
        {
            avatarSelectionMarker.anchoredPosition = new Vector2(100, -175);
        }
        else if (index == 1)
        {
            avatarSelectionMarker.anchoredPosition = new Vector2(300, -175);
        }
        else if (index == 2)
        {
            avatarSelectionMarker.anchoredPosition = new Vector2(500, -175);
        }
        else if (index == 3)
        {
            avatarSelectionMarker.anchoredPosition = new Vector2(100, -375);
        }
        else if (index == 4)
        {
            avatarSelectionMarker.anchoredPosition = new Vector2(300, -375);
        }
        else if (index == 5)
        {
            avatarSelectionMarker.anchoredPosition = new Vector2(500, -375);
        }
    }


    public void NextPanel()
    {
        if (selectedAvatarIndex == -1)
        {
            //show avatar empty
            selectAvatarWindow.Open();
        }
        else if (nicknameInput.inputText.text == "")
        {
            //show nickname empty
            enterNicknameWindow.Open();
        }        
        else
        {
            UserAvatarManager.instance.SetUserSprite(selectedAvatarIndex);

            PlayerPrefsX.SetString(PlayerPrefsLocations.User.Details.nickname, nicknameInput.inputText.text);

            setupManager.NextPanel();
        }        
    }
}
