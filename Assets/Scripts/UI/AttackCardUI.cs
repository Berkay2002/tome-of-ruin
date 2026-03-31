using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class AttackCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI tagText;
    public TextMeshProUGUI damageText;

    public AttackData AttackData { get; private set; }

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _originalPosition;
    private Transform _originalParent;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(AttackData data)
    {
        AttackData = data;
        if (nameText != null) nameText.text = data.attackName;
        if (tagText != null)
        {
            string tags = data.primaryTag.ToString();
            if (data.secondaryTag != AttackTag.None)
                tags += " / " + data.secondaryTag.ToString();
            tagText.text = tags;
        }
        if (damageText != null) damageText.text = $"DMG: {data.baseDamage}";
    }

    private Canvas _rootCanvas;

    public void OnBeginDrag(PointerEventData eventData)
    {
        _originalPosition = _rectTransform.anchoredPosition;
        _originalParent = transform.parent;
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.7f;
        _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        transform.SetParent(transform.root);
    }

    public void OnDrag(PointerEventData eventData)
    {
        float scaleFactor = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
        _rectTransform.anchoredPosition += eventData.delta / scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;
        transform.SetParent(_originalParent);
        _rectTransform.anchoredPosition = _originalPosition;
    }
}
