using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class inputLogin : MonoBehaviour
{
    [SerializeField] private TMP_InputField userLogin;
    [SerializeField] private TMP_InputField passwordLogin;
    [SerializeField] private TMP_InputField userRegister;
    [SerializeField] private TMP_InputField passwordRegister;
    [SerializeField] private TMP_InputField passwordRegister2;

    [SerializeField] private GamePlay dataGameplay;

    [SerializeField] private Toggle visibilityLoginToggle;
    [SerializeField] private Toggle visibilityRegisterToggle;

    [SerializeField] private Image loginCheckmark;
    [SerializeField] private Image registerCheckmark;

    [SerializeField] private Sprite showPasswordImage;
    [SerializeField] private Sprite hidePasswordImage;
    public void GetValueLogin()
    {
        if (userLogin.text.Trim().Length == 0) return;
        Value.Instance.value(userLogin.text, passwordLogin.text, passwordRegister2.text);
    }
    public void GetValueRegister()
    {
        if (userRegister.text.Trim().Length == 0) return;
        Value.Instance.value(userRegister.text, passwordRegister.text, passwordRegister2.text);
    }

    public void LogIn()
    {
        if(Value.Instance.GetUser() != "" && Value.Instance.GetPasword() != "")
        {
            LoginWeb.Instance.login();

        }else
        {
            LoginWeb.Instance.loginStatus.text = "Tài khoản và mật khẩu không đươc bỏ trống!";

        }
    }
    public void RegisterUser()
    {
        if(Value.Instance.GetUser() != "" && Value.Instance.GetPasword() != "" && Value.Instance.GetPassword2() != "")
        {
            if (Value.Instance.GetPasword() == Value.Instance.GetPassword2())
            {
                LoginWeb.Instance.registerUser();
            }
            else
            {
                LoginWeb.Instance.registerStatus.text = "Mật khẩu không trùng khớp";
            }
        }else
        {
            LoginWeb.Instance.registerStatus.text = "Tài khoản và mật khẩu không đươc bỏ trống!";

        }
    }

    void Start()
    {
        visibilityLoginToggle.onValueChanged.AddListener(OnToggleLoginChanged);
        visibilityRegisterToggle.onValueChanged.AddListener(OnToggleRegisterChanged);
        // Thiết lập hình ảnh ban đầu của Toggle
        UpdateLoginToggleImage();
        UpdateRegisterToggleImage();
    }

    void OnToggleLoginChanged(bool isOn)
    {
        // Thay đổi contentType của TMP_InputField
        if (isOn)
        {
            passwordLogin.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            passwordLogin.contentType = TMP_InputField.ContentType.Password;
        }

        // Cập nhật lại TMP_InputField để thay đổi có hiệu lực
        passwordLogin.ForceLabelUpdate();

        // Cập nhật hình ảnh của Toggle
        UpdateLoginToggleImage();
    }
    void UpdateLoginToggleImage()
    {

        if (visibilityLoginToggle.isOn == true)
        {
            loginCheckmark.sprite = showPasswordImage;
        }
        else
        {
            loginCheckmark.sprite = hidePasswordImage;
        }
    }

    void OnToggleRegisterChanged(bool isOn)
    {
        // Thay đổi contentType của TMP_InputField
        if (isOn)
        {
            passwordRegister.contentType = TMP_InputField.ContentType.Standard;
            passwordRegister2.contentType = TMP_InputField.ContentType.Standard;
        }
        else
        {
            passwordRegister.contentType = TMP_InputField.ContentType.Password;
            passwordRegister2.contentType = TMP_InputField.ContentType.Password;
        }

        // Cập nhật lại TMP_InputField để thay đổi có hiệu lực
        passwordLogin.ForceLabelUpdate();

        // Cập nhật hình ảnh của Toggle
        UpdateRegisterToggleImage();
    }

    void UpdateRegisterToggleImage()
    {

        if (visibilityLoginToggle.isOn == true)
        {
            registerCheckmark.sprite = showPasswordImage;
        }
        else
        {
            registerCheckmark.sprite = hidePasswordImage;
        }
    }
}
