using UnityEngine;
namespace CursedDeck.View
{
    public sealed partial class GameView
    {
        public AmbientCastleMist AmbientMist {get;private set;}
        public FloatingScoreText ScorePopups {get;private set;}
        public DrawButtonBreath DrawBreath {get;private set;}
        public float DisplayedScore=>hud.DisplayedScore;
        private void BuildGameFeel()
        {
            var prefab=Resources.Load<AmbientCastleMist>("VFX/VFX_AmbientCastleMist");
            if(prefab==null)throw new System.InvalidOperationException("Prepare Visual Phase 2 to create the ambient mist prefab.");
            AmbientMist=Instantiate(prefab,transform,false);AmbientMist.name="AmbientCastleMist";UiFactory.Anchor(AmbientMist.rectTransform,Vector2.zero,Vector2.one);AmbientMist.transform.SetSiblingIndex(safe.GetSiblingIndex());
            var popup=UiFactory.Rect("FloatingScores",safe);UiFactory.Anchor(popup,Vector2.zero,Vector2.one);popup.SetSiblingIndex(Particles.transform.GetSiblingIndex()+1);
            ScorePopups=popup.gameObject.AddComponent<FloatingScoreText>();ScorePopups.Initialize(font);
            DrawBreath=drawRect.GetComponentInChildren<DrawButtonBreath>(true);DrawBreath.Initialize();Board.SetDealSource(drawRect);
        }
        private void Update(){if(initialized&&hud!=null)hud.Step(Time.unscaledDeltaTime);}
        private bool motionReduced;
        private void ApplyMotionPreference(bool reduced)
        {
            if(reduced&&!motionReduced){Particles.ResetPool();ScorePopups.ResetPool();}motionReduced=reduced;
            tweens.SetReduceMotion(reduced);Particles.ReduceMotion=reduced;ScorePopups.ReduceMotion=reduced;
            if(AmbientMist.gameObject.activeSelf==reduced)AmbientMist.gameObject.SetActive(!reduced);
            DrawBreath.SetReduceMotion(reduced);
        }
    }
}
