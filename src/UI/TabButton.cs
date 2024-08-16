using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CompetitiveCompany.UI;

internal class TabButton : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] TMP_Text _label;
    [SerializeField] Image _background;
    [Space]
    [SerializeField] Sprite _defaultBackground;
    [SerializeField] Color _unselectedTextColor;
    [SerializeField] Color _selectedTextColor;
    [Space]
    [SerializeField] UnityEvent _onSelected;
    [SerializeField] UnityEvent _onDeselected;
    
    bool _selected;

    public bool Selected {
        get => _selected;
        set {
            _selected = value;
            SetState(value);
        }
    }
    
    public event Action<TabButton>? OnClicked;
    
    public void OnPointerDown(PointerEventData eventData) {
        OnClicked?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (!Selected) {
            SetState(true, invokeEvents: false);
        }
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (!Selected) {
            SetState(false, invokeEvents: false);
        }
    }

    void SetState(bool selected, bool invokeEvents = true) {
        _label.color = selected ? _selectedTextColor : _unselectedTextColor;
        _background.sprite = selected ? null : _defaultBackground;

        if (!invokeEvents) return;
        
        if (selected) {
            _onSelected.Invoke();
        } else {
            _onDeselected.Invoke();
        }
    }
}