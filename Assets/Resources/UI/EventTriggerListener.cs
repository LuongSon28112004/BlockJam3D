// using UnityEngine;
// using UnityEngine.EventSystems;
// using System;

// public class EventTriggerListener : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
// {
//     public Action<GameObject> onEnter;
//     public Action<GameObject> onExit;

//     public static EventTriggerListener Get(GameObject go)
//     {
//         var listener = go.GetComponent<EventTriggerListener>();
//         if (listener == null)
//             listener = go.AddComponent<EventTriggerListener>();
//         return listener;
//     }

//     public void OnPointerEnter(PointerEventData eventData)
//     {
//         onEnter?.Invoke(gameObject);
//     }

//     public void OnPointerExit(PointerEventData eventData)
//     {
//         onExit?.Invoke(gameObject);
//     }
// }
