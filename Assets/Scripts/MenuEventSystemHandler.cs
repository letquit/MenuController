using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 菜单事件系统处理器，负责处理菜单中的选择、导航和动画效果
/// </summary>
public class MenuEventSystemHandler : MonoBehaviour
{
    [Header("References")]
    public List<Selectable> Selectables = new List<Selectable>();
    [SerializeField] protected Selectable _firstSelected;

    [Header("Controls")] [SerializeField] protected InputActionReference _navigateReference;
    
    [Header("Animations")] 
    [SerializeField] protected float _selectedAnimationScale = 1.1f;
    [SerializeField] protected float _scaleDuration = 0.25f;
    [SerializeField] protected List<GameObject> _animationExclusions = new List<GameObject>();

    [Header("Sounds")]  
    [SerializeField] protected UnityEvent SoundEvent;
    
    protected Dictionary<Selectable, Vector3> _scales = new Dictionary<Selectable, Vector3>();

    protected Selectable _lastSelected;
    
    protected Tween _scaleUpTween;
    protected Tween _scaleDownTween;

    /// <summary>
    /// 初始化方法，在对象创建时调用
    /// 遍历所有可选择对象并添加选择监听器，同时记录原始缩放比例
    /// </summary>
    public virtual void Awake()
    {
        foreach (var selectable in Selectables)
        {
            AddSelectionListeners(selectable);
            _scales.Add(selectable, selectable.transform.localScale);
        }
    }

    /// <summary>
    /// 启用组件时调用的方法
    /// 订阅导航输入事件，重置所有可选择对象的缩放到原始大小，并延迟选择第一个对象
    /// </summary>
    public virtual void OnEnable()
    {
        _navigateReference.action.performed += OnNavigate;
        
        //ensure all selectables are reset back to original size
        for (int i = 0; i < Selectables.Count; i++)
        {
            Selectables[i].transform.localScale = _scales[Selectables[i]];
        }
        
        StartCoroutine(SelectAfterDelay());
    }

    /// <summary>
    /// 延迟一帧后选择初始选中对象的协程
    /// </summary>
    /// <returns>协程迭代器</returns>
    protected virtual IEnumerator SelectAfterDelay()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(_firstSelected.gameObject);
    }

    /// <summary>
    /// 禁用组件时调用的方法
    /// 取消订阅导航输入事件，并终止正在进行的缩放动画
    /// </summary>
    public virtual void OnDisable()
    {
        _navigateReference.action.performed -= OnNavigate;
        
        _scaleUpTween.Kill(true);
        _scaleDownTween.Kill(true);
    }
    
    /// <summary>
    /// 为指定的可选择对象添加各种选择相关的事件监听器
    /// 包括选择、取消选择、鼠标进入和鼠标离开事件
    /// </summary>
    /// <param name="selectable">要添加监听器的可选择对象</param>
    protected virtual void AddSelectionListeners(Selectable selectable)
    {
        //add listener
        EventTrigger trigger = selectable.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = selectable.gameObject.AddComponent<EventTrigger>();
        }
        
        //add SELECT event
        EventTrigger.Entry SelectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Select
        };
        SelectEntry.callback.AddListener(OnSelect);
        trigger.triggers.Add(SelectEntry);
        
        //add DESELECT event
        EventTrigger.Entry DeselectEntry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.Deselect
        };
        DeselectEntry.callback.AddListener(OnDeselect);
        trigger.triggers.Add(DeselectEntry);
        
        //add ONPOINTERENTER event
        EventTrigger.Entry PointerEnter = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerEnter
        };
        PointerEnter.callback.AddListener(OnPointerEnter);
        trigger.triggers.Add(PointerEnter);
        //add ONPOINTEREXIT event
        EventTrigger.Entry PointerExit = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerExit
        };
        PointerExit.callback.AddListener(OnPointerExit);
        trigger.triggers.Add(PointerExit);
    }

    /// <summary>
    /// 处理选择事件的回调方法
    /// 播放音效，记录最后选中的对象，并执行放大动画（如果不在排除列表中）
    /// </summary>
    /// <param name="eventData">事件数据，包含选中的对象信息</param>
    private void OnSelect(BaseEventData eventData)
    {
        SoundEvent?.Invoke();
        _lastSelected = eventData.selectedObject.GetComponent<Selectable>();
        
        if (_animationExclusions.Contains(eventData.selectedObject))
            return;
        
        Vector3 newScale = eventData.selectedObject.transform.localScale * _selectedAnimationScale;
        _scaleUpTween = eventData.selectedObject.transform.DOScale(newScale, _scaleDuration);
    }

    /// <summary>
    /// 处理取消选择事件的回调方法
    /// 执行缩小动画以恢复到原始大小（如果不在排除列表中）
    /// </summary>
    /// <param name="eventData">事件数据，包含取消选中的对象信息</param>
    private void OnDeselect(BaseEventData eventData)
    {
        if (_animationExclusions.Contains(eventData.selectedObject))
            return;
        
        Selectable sel = eventData.selectedObject.GetComponent<Selectable>();
        _scaleDownTween = eventData.selectedObject.transform.DOScale(_scales[sel], _scaleDuration);
    }

    /// <summary>
    /// 处理鼠标指针进入事件的回调方法
    /// 将指针进入的对象设置为当前选中对象
    /// </summary>
    /// <param name="eventData">事件数据，包含指针进入的信息</param>
    private void OnPointerEnter(BaseEventData eventData)
    {
        PointerEventData pointerEventData = eventData as PointerEventData;
        if (pointerEventData != null)
        {
            Selectable sel = pointerEventData.pointerEnter.GetComponentInParent<Selectable>();
            if (sel == null)
            {
                sel = pointerEventData.pointerEnter.GetComponentInChildren<Selectable>();
            }

            pointerEventData.selectedObject = sel.gameObject;
        }
    }

    /// <summary>
    /// 处理鼠标指针离开事件的回调方法
    /// 清除当前选中对象
    /// </summary>
    /// <param name="eventData">事件数据，包含指针离开的信息</param>
    private void OnPointerExit(BaseEventData eventData)
    {
        PointerEventData pointerEventData = eventData as PointerEventData;
        if (pointerEventData != null)
        {
            pointerEventData.selectedObject = null;
        }
    }

    /// <summary>
    /// 处理导航输入的回调方法
    /// 当前没有选中对象但有最后选中对象时，恢复到最后选中的对象
    /// </summary>
    /// <param name="context">输入动作的回调上下文</param>
    protected virtual void OnNavigate(InputAction.CallbackContext context)
    {
        if (EventSystem.current.currentSelectedGameObject == null && _lastSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(_lastSelected.gameObject);
        }
    }
}
