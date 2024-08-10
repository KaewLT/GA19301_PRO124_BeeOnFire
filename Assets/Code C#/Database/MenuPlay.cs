using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPlay : MonoBehaviour
{
    [SerializeField] private GamePlay gamePlayData;
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject groupButtonMenu;
    [SerializeField] private GameObject HTPPanel;
    public void playGame()
    {
        if (gamePlayData.login == true)
        {
            gamePlayData.levelId = UnityEngine.Random.Range(1, 4);
            gamePlayData.starTime = DateTime.Now;
            SceneManager.LoadScene(gamePlayData.levelId);
        }
        else
        {
            panelLogin.gameObject.SetActive(true);
            groupButtonMenu.gameObject.SetActive(false);
        }
    }

    public void logOut()
    {
        gamePlayData.login = false;
        gamePlayData.userId = 0;
        gamePlayData.UserName = string.Empty;
    }
    public void exit()
    {
        Application.Quit();
    }

    public void GetData()
    {
        LoginWeb.Instance.GetData();
    }

    public void testend()
    {
        SceneManager.LoadScene("CutScenes");
    }
    public void HowToPlay()
    {
        HTPPanel.gameObject.SetActive(true);
    }
    public void OffHTP()
    {
        HTPPanel.gameObject.SetActive(false);
    }
}
