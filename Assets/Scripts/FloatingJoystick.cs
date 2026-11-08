using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.OnScreen;

public class FloatingJoystick : OnScreenControl,
    IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [InputControl(layout = "Vector2")]
    [SerializeField] string m_ControlPath = "<Gamepad>/leftStick";

    public RectTransform background;
    public RectTransform handle;
    public float movementRange = 100f;

    RectTransform area;

    protected override string controlPathInternal
    {
        get => m_ControlPath;
        set => m_ControlPath = value;
    }

    void Start()
    {
        area = (RectTransform)transform;
        background.gameObject.SetActive(false);
    }

    public void OnPointerDown(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            area, e.position, e.pressEventCamera, out Vector2 local);
        background.anchoredPosition = local;
        handle.anchoredPosition = Vector2.zero;
        background.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            background, e.position, e.pressEventCamera, out Vector2 local);
        Vector2 delta = Vector2.ClampMagnitude(local, movementRange);
        handle.anchoredPosition = delta;
        SendValueToControl(delta / movementRange);
    }

    public void OnPointerUp(PointerEventData e)
    {
        handle.anchoredPosition = Vector2.zero;
        SendValueToControl(Vector2.zero);
        background.gameObject.SetActive(false);
    }
}