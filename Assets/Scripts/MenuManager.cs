using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 菜单管理器，用于管理游戏中的页面切换和显示
/// </summary>
public class MenuManager : MonoBehaviour
{
    [SerializeField] private List<GameObject> _allPages = new List<GameObject>();
    [SerializeField] private GameObject _mainPage;

    /// <summary>
    /// 初始化时显示主页面
    /// </summary>
    private void Awake()
    {
        ShowPage(_mainPage);
    }

    /// <summary>
    /// 显示指定的目标页面，同时隐藏其他所有页面
    /// </summary>
    /// <param name="targetPage">要显示的目标页面GameObject对象</param>
    public void ShowPage(GameObject targetPage)
    {
        // 遍历所有页面并隐藏它们
        foreach (var page in _allPages)
        {
            if (page != null)
                page.SetActive(false);
        }

        // 检查目标页面是否有效，如果有效则激活显示
        if (targetPage != null)
        {
            targetPage.SetActive(true);
        }
    }

    /// <summary>
    /// 返回到主页面
    /// </summary>
    public void GoBackToMain()
    {
        ShowPage(_mainPage);
    }
}
