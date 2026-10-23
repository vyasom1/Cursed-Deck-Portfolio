using System.Collections.Generic;
using TMPro;
using UnityEngine;
using CursedDeck.Run;

namespace CursedDeck.View
{
    [ExecuteAlways]
    public sealed class BoardView : MonoBehaviour, UnityEngine.UI.ILayoutSelfController
    {
        private readonly Stack<CardView> pool=new Stack<CardView>(28);
        private readonly CardView[] all=new CardView[28],active=new CardView[28],popping=new CardView[28];
        private readonly CardUiState[] last=new CardUiState[28];
        private RectTransform rect;
        private TweenRunner tweens;private RectTransform wasteTarget,dealSource;
        public void SetWasteTarget(RectTransform target){wasteTarget=target;}
        public void SetDealSource(RectTransform target){dealSource=target;}
        public bool IsDealing=>tweens!=null&&tweens.ActiveDealCount>0;
        public Vector3 LastPlayWorldPosition {get;private set;}
        private bool initialized;private int cardCount;private float pixelsPerDp;
        public void SetParticleFX(CardParticleFX fx){for(int i=0;i<all.Length;i++)all[i].SetParticleFX(fx);}
        public int AllocatedCardCount => initialized ? 28 : 0;
        public int ActiveCardCount { get; private set; }
        public CardView CardAt(int slot) => active[slot];
        public void Initialize(EventBus bus,TMP_FontAsset font,Sprite front,Sprite back,TweenRunner tweens,Sprite[] suitIcons=null,CardView prefab=null,CardCharacterVisual[] characters=null)
        {
            rect=(RectTransform)transform; this.tweens=tweens;pixelsPerDp=MobileUiScale.ReadPixelsPerDp();
            if(prefab==null)prefab=Resources.Load<CardView>("TCG/CardView");
            if(prefab==null)throw new System.InvalidOperationException("Run Tools > Cursed Deck > Card Art Pipeline to prepare CardView.prefab.");
            for(int i=0;i<28;i++)
            {
                var card=Instantiate(prefab,transform,false);card.name="Card-"+i;
                card.Initialize(this,bus,font,front,back,suitIcons,characters);all[i]=card;
            }
            initialized=true;ResetPool();
        }
        private void ResetPool()
        {
            pool.Clear(); ActiveCardCount=0;
            for(int i=27;i>=0;i--)
            {
                tweens.Cancel(all[i].Rect);tweens.Cancel(all[i].FX.MotionRoot);all[i].ResetForPool(); all[i].gameObject.SetActive(false); all[i].InPool=true;
                active[i]=null; popping[i]=null; pool.Push(all[i]);
            }
        }
        public void Render(GameSnapshot snapshot,bool newDeal)
        {
            tweens.SetReduceMotion(snapshot.Settings.ReduceMotion);
            for(int i=0;i<all.Length;i++)all[i].SetReduceMotion(snapshot.Settings.ReduceMotion);
            if(newDeal || cardCount!=snapshot.Cards.Count) ResetPool();
            cardCount=snapshot.Cards.Count;
            ActiveCardCount=0;
            for(int i=0;i<cardCount;i++)
            {
                var state=snapshot.Cards[i]; last[i]=state;
                if(state.Removed)
                {
                    if(active[i]!=null)
                    {
                        var card=active[i]; active[i]=null;
                        if(snapshot.Feedback==GameFeedback.Play && snapshot.PlayedSlot==i) { popping[i]=card;LastPlayWorldPosition=card.FX.HasTapPosition?card.FX.TapWorldPosition:card.Rect.position;
                            // After acceptance the waste DTO discloses even a successfully matched blind card.
                            // Bind only presentation data; never reveal a hidden card before the rule engine accepts it.
                            card.Bind(new CardUiState(i,state.X,state.Y,snapshot.WasteRank,snapshot.WasteSuit,true,true,false,false),snapshot.Settings.Colorblind);
                            tweens.Cancel(card.FX.MotionRoot);card.OnCardPlayed(snapshot.Chain);
                            if(wasteTarget!=null)tweens.FlyToWaste(card,rect.InverseTransformPoint(wasteTarget.TransformPoint(wasteTarget.rect.center)),wasteTarget);else tweens.Pop(card); }
                        else Release(card);
                    }
                    continue;
                }
                if(active[i]==null)
                {
                    if(popping[i]!=null) { active[i]=popping[i]; popping[i]=null; tweens.Cancel(active[i].Rect); }
                    else active[i]=pool.Pop();
                }
                active[i].Bind(state,snapshot.Settings.Colorblind); active[i].transform.SetSiblingIndex(i); ActiveCardCount++;
            }
            PositionCards();
            if(newDeal&&!snapshot.Settings.ReduceMotion&&(dealSource!=null||wasteTarget!=null))
            {
                var origin=dealSource!=null?dealSource:wasteTarget;var source=rect.InverseTransformPoint(origin.TransformPoint(origin.rect.center));
                for(int i=0;i<cardCount;i++)if(active[i]!=null)tweens.Deal(active[i],(Vector2)source-active[i].Rect.anchoredPosition,cardCount>1?i*.24f/(cardCount-1):0);
            }
            if(snapshot.PlayedSlot>=0&&popping[snapshot.PlayedSlot]!=null)popping[snapshot.PlayedSlot].transform.SetAsLastSibling();
        }
        public void Release(CardView card)
        {
            if(card.InPool) return;
            if(card.Slot>=0 && card.Slot<28 && popping[card.Slot]==card) popping[card.Slot]=null;
            card.ResetForPool();card.gameObject.SetActive(false); card.InPool=true; pool.Push(card);
        }
        public void SetLayoutHorizontal(){if(initialized)PositionCards();}
        public void SetLayoutVertical(){if(initialized)PositionCards();}
        private void OnRectTransformDimensionsChange()
        {
            if(!initialized)return;
            PositionCards();UnityEngine.UI.LayoutRebuilder.MarkLayoutForRebuild(rect);
        }
        private void PositionCards()
        {
            if(cardCount==0)return;
            int minX=int.MaxValue,maxX=int.MinValue,minY=int.MaxValue,maxY=int.MinValue;
            for(int i=0;i<cardCount;i++){minX=Mathf.Min(minX,last[i].X);maxX=Mathf.Max(maxX,last[i].X);minY=Mathf.Min(minY,last[i].Y);maxY=Mathf.Max(maxY,last[i].Y);}
            var canvas=GetComponentInParent<Canvas>();
            float width=BoardGeometry.CardWidth(rect.rect.width,rect.rect.height,(maxX-minX)*.5f,maxY-minY,cardCount==19,canvas!=null?canvas.scaleFactor:1,pixelsPerDp);
            float height=width*1.4f,pitch=width,step=BoardGeometry.RowStep(height,rect.rect.height,maxY-minY);
            for(int i=0;i<cardCount;i++) if(active[i]!=null)
            {
                var card=active[i].Rect;card.anchorMin=card.anchorMax=new Vector2(.5f,.5f);
                active[i].SetLayout(width,height);
                // Screen-space -12px is +12 in Unity's upward-positive anchored Y.
                card.anchoredPosition=new Vector2((last[i].X-(minX+maxX)*.5f)*pitch*.5f,((minY+maxY)*.5f-last[i].Y)*step+(last[i].Playable?12:0));
            }
        }
    }
}
