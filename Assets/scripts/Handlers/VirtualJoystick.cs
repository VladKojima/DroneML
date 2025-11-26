using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Joystick Settings")]
    public RectTransform background;
    public RectTransform handle;
    public float handleRange = 50f;
    public bool snapX = false;
    public bool snapY = false;

    [Header("Output")]
    [Range(-1f, 1f)]
    public float Horizontal = 0f;
    [Range(-1f, 1f)]
    public float Vertical = 0f;

    private Vector2 input = Vector2.zero;
    private bool isDragging = false;
    private Canvas parentCanvas;

    void Start()
    {
        // Если компоненты не назначены, попробуем найти их автоматически
        if (background == null)
            background = GetComponent<RectTransform>();
        if (handle == null && transform.childCount > 0)
            handle = transform.GetChild(0).GetComponent<RectTransform>();
        if (handle == null)
        {
            Debug.LogError("VirtualJoystick: Handle не найден!");
        }

        // Находим родительский Canvas
        parentCanvas = GetComponentInParent<Canvas>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        // Возвращаем джойстик в центр
        input = Vector2.zero;
        if (handle != null)
        {
            handle.anchoredPosition = Vector2.zero;
        }
        UpdateOutput();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || background == null) return;

        Vector2 localPoint;

        // Правильно конвертируем позицию экрана в локальную позицию относительно фона джойстика
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background,
            eventData.position,
            parentCanvas?.worldCamera,
            out localPoint))
        {
            // Нормализуем относительно размера фона
            Vector2 sizeDelta = background.sizeDelta;
            input.x = -localPoint.x / (sizeDelta.x * 0.5f);  // Инвертируем X
            input.y = -localPoint.y / (sizeDelta.y * 0.5f);  // Инвертируем Y для правильного направления

            // Обрезаем ввод в пределах единичной окружности
            input = Vector2.ClampMagnitude(input, 1f);

            // Применяем привязку к осям если нужно
            if (snapX)
            {
                if (Mathf.Abs(input.x) < 0.5f)
                    input.x = 0f;
            }
            if (snapY)
            {
                if (Mathf.Abs(input.y) < 0.5f)
                    input.y = 0f;
            }

            // Обновляем позицию рукоятки
            if (handle != null)
            {
                handle.anchoredPosition = input * handleRange;
            }

            UpdateOutput();
        }
    }

    void UpdateOutput()
    {
        Horizontal = input.x;
        Vertical = input.y;
    }

    /// <summary>
    /// Получает направление джойстика как Vector2
    /// </summary>
    public Vector2 Direction
    {
        get { return new Vector2(Horizontal, Vertical); }
    }

    /// <summary>
    /// Получает силу нажатия джойстика (0-1)
    /// </summary>
    public float Magnitude
    {
        get { return Direction.magnitude; }
    }

    /// <summary>
    /// Программно устанавливает значение джойстика
    /// </summary>
    public void SetValue(Vector2 value)
    {
        input = Vector2.ClampMagnitude(value, 1f);
        if (handle != null)
        {
            handle.anchoredPosition = input * handleRange;
        }
        UpdateOutput();
    }

    /// <summary>
    /// Сбрасывает джойстик в центр
    /// </summary>
    public void Reset()
    {
        SetValue(Vector2.zero);
    }
}