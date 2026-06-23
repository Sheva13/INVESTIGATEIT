using TMPro;
using UnityEngine;

public class LaptopController : MonoBehaviour
{
    public GameObject laptopOff;
    public GameObject lockScreen;
    public GameObject desktopScreen;
    public GameObject errorText;

    public TMP_InputField passwordInput;

    public string correctPassword = "160406";

    void Start()
    {
        passwordInput.onSubmit.AddListener(delegate {
            CheckPassword();
        });
    }

    public void PowerOn()
    {
        laptopOff.SetActive(false);
        lockScreen.SetActive(true);
    }

    public void CheckPassword()
{
    Debug.Log("ENTER DITEKAN");

    if(passwordInput.text != correctPassword)
    {
        errorText.SetActive(true);
    }
    else
    {
        errorText.SetActive(false);
        lockScreen.SetActive(false);
        desktopScreen.SetActive(true);
    }
}
}