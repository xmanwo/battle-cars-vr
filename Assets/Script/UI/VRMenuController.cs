using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class VRMenuController : MonoBehaviour
{
    public GameObject menuCanvas;   // 挂在 Canvas 上
    public Button restartButton;
    //public Button showKeyButton;
    public GameObject keyImage;     // 图片对象

    private bool menuActive = false;
    private bool keyImageActive = false;  // 新增状态，控制图片显示

    void Start()
    {
        menuCanvas.SetActive(false);  // 初始隐藏菜单

        // 按钮功能绑定
        restartButton.onClick.AddListener(RestartGame);
        //showKeyButton.onClick.AddListener(ToggleKeyImage);
    }

    void Update()
    {
        // Quest 左手柄菜单键触发
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
        {
            menuActive = !menuActive;
            menuCanvas.SetActive(menuActive);
        }
    }

    void RestartGame()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);  // 重新加载当前场景
    }

    void ToggleKeyImage()
    {
        // 每按一次切换状态
        keyImageActive = !keyImageActive;
        keyImage.SetActive(keyImageActive);  // 显示/隐藏图片
    }
}