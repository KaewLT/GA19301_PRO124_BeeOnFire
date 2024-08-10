using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class LoginWeb : Singleton<LoginWeb>
{
    // Start is called before the first frame update
    [SerializeField] TextMeshProUGUI Rank;
    [SerializeField] TextMeshProUGUI Name;
    [SerializeField] TextMeshProUGUI Time;

    [SerializeField] public TextMeshProUGUI loginStatus;
    [SerializeField] public TextMeshProUGUI registerStatus;

    private static readonly string LoginURL = "https://phamduchuan.name.vn/LogIn.php";
    private static readonly string RegisterURL = "https://phamduchuan.name.vn/RegisterUser.php";
    private static readonly string rankURL = "https://phamduchuan.name.vn/rank.php";
    private static readonly string GetItemValueURL = "https://phamduchuan.name.vn/GetValueItems.php";

    [SerializeField] GamePlay gameplayData;
    public GameObject[] rows;

    public void login()
    {
        loginStatus.text = string.Empty;
        StartCoroutine(LogInCoroutine());
    }
    IEnumerator LogInCoroutine()
    {
        WWWForm form = new WWWForm();
        form.AddField("User_Name", Value.Instance.GetUser());
        form.AddField("Password_", Value.Instance.GetPasword());

        using (UnityWebRequest www = UnityWebRequest.Post(LoginURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                loginStatus.text = "Lỗi mạng: " + www.error;
            }
            else
            {
                Debug.Log("Response: " + www.downloadHandler.text);
                ProcessResponse(www.downloadHandler.text);
            }
        }
    }


    void ProcessResponse(string response)
    {
        Debug.Log(response);

        // Phân tích ph?n h?i JSON
        ResponseData responseData = JsonUtility.FromJson<ResponseData>(response);

        if (responseData.status == "success")
        {
            loginStatus.text = "Đăng nhập thành công!";
            gameplayData.userId = responseData.User_Id;
            gameplayData.UserName = Value.Instance.GetUser();
            gameplayData.login = true;
        }
        else
        {
            loginStatus.text = "Đăng nhập thất bại: " + responseData.message;
        }
    }

    [System.Serializable]
    public class ResponseData
    {
        public string status;
        public int User_Id;
        public string message;
    }


    public void registerUser()
    {
        registerStatus.text = string.Empty;
        StartCoroutine(RegisterUser());
    }

    IEnumerator RegisterUser()
    {
        WWWForm form = new WWWForm();
        form.AddField("User_Name", Value.Instance.GetUser());
        form.AddField("Password_", Value.Instance.GetPasword());

        using (UnityWebRequest www = UnityWebRequest.Post(RegisterURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                registerStatus.text = "Lỗi mạng: " + www.error;
            }
            else
            {
                Debug.Log("Response: " + www.downloadHandler.text);
                ProcessResponseRegister(www.downloadHandler.text);
            }
        }
    }

    void ProcessResponseRegister(string response)
    {
        Debug.Log(response);

        // Phân tích ph?n h?i JSON
        var responseDataRegister = JsonUtility.FromJson<ResponseData>(response);

        if (responseDataRegister.status == "success")
        {
            registerStatus.text = "Đăng ký thành công! " + responseDataRegister.message;
        }
        else
        {
            registerStatus.text = "Đăng ký thất bai: " + responseDataRegister.message;
        }
    }

    [System.Serializable]
    public class ResponseDataRegister
    {
        public string status;
        public string message;
    }


    public void GetData()
    {
        Rank.text += string.Empty;
        Name.text += string.Empty;
        Time.text += string.Empty;
        StartCoroutine(GetDataFromServer());
    }
    IEnumerator GetDataFromServer()
    {
        using (UnityWebRequest www = UnityWebRequest.Get(rankURL))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Error: " + www.error);
            }
            else
            {
                string jsonResult = www.downloadHandler.text;
                Debug.Log("Received: " + jsonResult);

                try
                {
                    GameDataWrapper gameDataWrapper = JsonUtility.FromJson<GameDataWrapper>(jsonResult);

                    if (gameDataWrapper != null && gameDataWrapper.items.Length > 0)
                    {
                        for (int i = 0; i < gameDataWrapper.items.Length; i++)
                        {
                            if (i < rows.Length)
                            {
                                var data = gameDataWrapper.items[i];
                                TextMeshProUGUI[] texts = rows[i].GetComponentsInChildren<TextMeshProUGUI>();

                                texts[0].text = data.Ranking.ToString();
                                texts[1].text = data.Character_Name.ToString();
                                int totalSeconds = int.Parse(data.Played_Time);
                                int minutes = Mathf.FloorToInt(totalSeconds / 60);
                                int seconds = totalSeconds % 60;
                                texts[2].text = string.Format("{0:00}:{1:00}", minutes, seconds);
                            }
                        }
                    }
                    else
                    {
                        Debug.Log("No items found");
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.Log("JSON parse error: " + ex.Message);
                }
            }
        }
    }
    [System.Serializable]
    private class GameData
    {
        public int Ranking;
        public string Character_Name;
        public string Played_Time;
    }

    [System.Serializable]
    private class GameDataWrapper
    {
        public GameData[] items;
    }
}
