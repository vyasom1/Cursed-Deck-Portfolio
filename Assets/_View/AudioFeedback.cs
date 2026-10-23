using System;
using UnityEngine;
using CursedDeck.Run;
namespace CursedDeck.View
{
    [Flags] public enum FeedbackSound {None=0,Draw=1,Play=2,Reveal=4,Purchase=8,Win=16,Lose=32}
    public readonly struct FeedbackAudioClips
    {
        public readonly AudioClip Play,Reveal,Purchase,Win,Lose;
        public FeedbackAudioClips(AudioClip play,AudioClip reveal,AudioClip purchase,AudioClip win,AudioClip lose){Play=play;Reveal=reveal;Purchase=purchase;Win=win;Lose=lose;}
    }
    public sealed class AudioFeedback:MonoBehaviour
    {
        private EventBus bus;private AudioSource music,chimes;private AudioSource[] voices;
        private AudioClip card,combo;private FeedbackAudioClips clips;private GameSnapshot previous;private int cursor;private uint random=0x50A7D1u;
        public FeedbackSound LastCues {get;private set;}
        public int PlayedCueCount {get;private set;}
        public float LastCardPitch {get;private set;}=1;
        public int VoiceCapacity=>voices==null?0:voices.Length;
        private AudioSource Source(){var s=gameObject.AddComponent<AudioSource>();s.playOnAwake=false;s.spatialBlend=0;return s;}
        public void Initialize(EventBus events,AudioClip ambient,AudioClip cardSound,AudioClip comboSound,bool sound,FeedbackAudioClips effects=default)
        {
            bus=events;card=cardSound;combo=comboSound;clips=effects;music=Source();chimes=Source();voices=new AudioSource[8];for(int i=0;i<voices.Length;i++)voices[i]=Source();
            music.loop=true;music.volume=.18f;music.clip=ambient;Mute(!sound);if(ambient!=null)music.Play();bus.GameChanged+=Changed;
        }
        public static float ChainPitch(int chain)=>Mathf.Pow(2,Mathf.Clamp(chain-1,0,12)/12f);
        public static FeedbackSound Cues(GameSnapshot before,GameSnapshot after)
        {
            if(before==null)return FeedbackSound.None;
            bool same=before.BoardSeed==after.BoardSeed&&before.Floor==after.Floor&&before.Round==after.Round;
            FeedbackSound result=FeedbackSound.None;
            if(same&&after.Feedback==GameFeedback.Play)result|=FeedbackSound.Play;
            if(same&&after.Feedback==GameFeedback.Draw&&before.Stock!=after.Stock)result|=FeedbackSound.Draw;
            if(after.Feedback==GameFeedback.Shop&&before.Gold>after.Gold)result|=FeedbackSound.Purchase;
            if(same&&(after.Feedback==GameFeedback.Play||after.Feedback==GameFeedback.Peek)&&before.Cards!=null&&after.Cards!=null)
                for(int i=0;i<Mathf.Min(before.Cards.Count,after.Cards.Count);i++)if(!after.Cards[i].Removed&&!before.Cards[i].Visible&&after.Cards[i].Visible){result|=FeedbackSound.Reveal;break;}
            if(same&&before.Stage==RunStage.Playing)
            {
                if(after.Stage==RunStage.Reward||after.Stage==RunStage.Won)result|=FeedbackSound.Win;
                if(after.Stage==RunStage.Lost)result|=FeedbackSound.Lose;
            }
            return result;
        }
        private void Mute(bool mute){music.mute=chimes.mute=mute;for(int i=0;i<voices.Length;i++)voices[i].mute=mute;}
        public float NextCardPitch(){random=random*1664525u+1013904223u;return .95f+(random>>8)*(0.1f/16777216f);}
        private void Play(AudioClip clip,bool cardSound,float volume)
        {
            if(clip==null)return;var source=voices[cursor];cursor=(cursor+1)%voices.Length;
            source.Stop();source.clip=clip;source.pitch=cardSound?NextCardPitch():1;source.volume=volume;source.Play();PlayedCueCount++;if(cardSound)LastCardPitch=source.pitch;
        }
        private void Changed(GameSnapshot snapshot)
        {
            LastCues=Cues(previous,snapshot);previous=snapshot;Mute(!snapshot.Settings.Sound);if(!snapshot.Settings.Sound)return;
            if((LastCues&FeedbackSound.Draw)!=0)Play(card,true,.35f);
            if((LastCues&FeedbackSound.Play)!=0){Play(clips.Play??card,true,.4f);if(snapshot.Chain>1&&combo!=null){chimes.Stop();chimes.clip=combo;chimes.pitch=ChainPitch(snapshot.Chain);chimes.volume=.2f;chimes.Play();}}
            if((LastCues&FeedbackSound.Reveal)!=0)Play(clips.Reveal,true,.3f);
            if((LastCues&FeedbackSound.Purchase)!=0)Play(clips.Purchase,false,.4f);
            if((LastCues&FeedbackSound.Win)!=0)Play(clips.Win,false,.45f);
            if((LastCues&FeedbackSound.Lose)!=0)Play(clips.Lose,false,.45f);
        }
        private void OnDestroy(){if(bus!=null)bus.GameChanged-=Changed;}
    }
}
