using System;
using UnityEngine;
using UnityEngine.EventSystems;
using CursedDeck.Run;
namespace CursedDeck.View
{
    public sealed class CardLongPress : MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IDragHandler
    {
        private EventBus bus;private Func<int> slot;private bool held,suppress;private float age;private Vector2 origin;
        public bool CanInspect {get;set;}
        public void Initialize(EventBus events,Func<int> cardSlot){bus=events;slot=cardSlot;}
        public void OnPointerDown(PointerEventData e){suppress=false;held=CanInspect;age=0;origin=e.position;}
        public void OnPointerUp(PointerEventData e){held=false;}
        public void OnDrag(PointerEventData e){if((origin-e.position).sqrMagnitude>144)held=false;}
        private void Update(){Step(Time.unscaledDeltaTime);}
        public void Step(float delta){if(!held)return;age+=delta;if(age<.5f)return;held=false;suppress=true;bus?.RequestCardInspection(slot());}
        public bool ConsumeClick(){bool result=suppress;suppress=false;return result;}
        public void ResetHold(){held=suppress=false;age=0;CanInspect=false;}
        private void OnDisable(){ResetHold();}
    }
}
