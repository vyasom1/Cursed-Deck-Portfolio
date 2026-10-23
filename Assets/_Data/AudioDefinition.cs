using UnityEngine;
namespace CursedDeck.Data
{
    [CreateAssetMenu(menuName="Cursed Deck/Audio")]
    public sealed class AudioDefinition:ScriptableObject
    {
        [SerializeField] private AudioClip ambient,card,combo;
        [SerializeField] private AudioClip play, reveal, purchase, win, defeat;
        public AudioClip Play=>play;public AudioClip Reveal=>reveal;public AudioClip Purchase=>purchase;public AudioClip Win=>win;public AudioClip Defeat=>defeat;
        public AudioClip Ambient=>ambient;
        public AudioClip Card=>card;
        public AudioClip Combo=>combo;
    }
}
