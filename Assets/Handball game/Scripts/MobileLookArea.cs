using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class MobileLookArea : MonoBehaviour,
    IPointerDownHandler,
    IDragHandler,
    IPointerUpHandler
{
    public bool IsDragging { get; private set; }

    private const int NoPointer = int.MinValue;

    private int activePointerId = NoPointer;
    private Vector2 accumulatedDelta;

    public void OnPointerDown(PointerEventData eventData)
    {
        if (IsDragging)
        {
            return;
        }

        activePointerId = eventData.pointerId;
        accumulatedDelta = Vector2.zero;
        IsDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsDragging ||
            eventData.pointerId != activePointerId)
        {
            return;
        }

        accumulatedDelta += eventData.delta;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != activePointerId)
        {
            return;
        }

        ResetInput();
    }

    public Vector2 ConsumeDelta()
    {
        Vector2 delta = accumulatedDelta;
        accumulatedDelta = Vector2.zero;

        return delta;
    }

    private void OnDisable()
    {
        ResetInput();
    }

    private void ResetInput()
    {
        accumulatedDelta = Vector2.zero;
        activePointerId = NoPointer;
        IsDragging = false;
    }
}