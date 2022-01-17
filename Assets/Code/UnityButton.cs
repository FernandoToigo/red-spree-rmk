using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UnityButton : Button
{
    public bool IsUp { get; private set; }
    
    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        IsUp = true;
        StartCoroutine(DisableIsUpOnNextFrame());
    }

    private IEnumerator DisableIsUpOnNextFrame()
    {
        yield return new WaitForFixedUpdate();
        IsUp = false;
    }
}
