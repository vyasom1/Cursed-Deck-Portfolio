using System;
using System.Runtime.Serialization;
namespace CursedDeck.Meta
{
    [DataContract] public sealed class GameSettings
    {
        // Composition binds the loaded profile; the Meta assembly remains Unity-free.
        public static GameSettings Instance { get; private set; } = new GameSettings();
        public static void Bind(GameSettings value) { Instance=value ?? throw new ArgumentNullException(nameof(value)); }
        [DataMember] public bool Sound { get; set; }=true;
        [DataMember] public bool Haptics { get; set; }=true;
        [DataMember] public bool LeftHanded { get; set; }
        [DataMember] public bool Colorblind { get; set; }
        [DataMember] public bool hasCompletedTutorial;
        [DataMember] public bool reduceMotion;
        [OnDeserializing] private void Defaults(StreamingContext context) { Sound=true;Haptics=true; }
    }
}
