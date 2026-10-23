namespace CursedDeck.Run
{
    public enum TutorialStage { Welcome, Match, Chain, Draw, Complete }
    /// <summary>Command authority for the practice session, independent of Unity or pointer routing.</summary>
    public sealed class GuidedTutorial
    {
        public TutorialStage Stage {get;private set;}
        public int Slot {get;}
        public GuidedTutorial(int slot) { Slot=slot; }
        public bool Allows(GameCommand command)
        {
            if(command.Kind==GameCommandKind.Play)return Stage==TutorialStage.Match&&command.Value==Slot;
            if(command.Kind==GameCommandKind.Draw)return Stage==TutorialStage.Draw;
            if(command.Kind==GameCommandKind.TutorialNext)return Stage==TutorialStage.Welcome||Stage==TutorialStage.Chain;
            if(command.Kind==GameCommandKind.TutorialComplete)return Stage==TutorialStage.Complete;
            return command.Kind==GameCommandKind.StartTutorial||command.Kind==GameCommandKind.Refresh||command.Kind==GameCommandKind.Pause||command.Kind==GameCommandKind.Resume||
                command.Kind==GameCommandKind.ReturnToTitle||command.Kind==GameCommandKind.ToggleSound||command.Kind==GameCommandKind.ToggleHaptics||
                command.Kind==GameCommandKind.ToggleLeftHanded||command.Kind==GameCommandKind.ToggleColorblind||command.Kind==GameCommandKind.ToggleReduceMotion;
        }
        public void Advance() { if(Stage==TutorialStage.Welcome)Stage=TutorialStage.Match;else if(Stage==TutorialStage.Chain)Stage=TutorialStage.Draw; }
        public void Accepted(GameCommand command)
        { if(Stage==TutorialStage.Match&&command.Kind==GameCommandKind.Play&&command.Value==Slot)Stage=TutorialStage.Chain;else if(Stage==TutorialStage.Draw&&command.Kind==GameCommandKind.Draw)Stage=TutorialStage.Complete; }
    }
}
