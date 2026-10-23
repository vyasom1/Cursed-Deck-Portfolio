using CursedDeck.Services;
using UnityEngine;
using UnityEngine.UI;

namespace CursedDeck.View
{
    /// <summary>Presentation suit IDs mirror the DTO, without referencing Core.</summary>
    public enum Suit {Clubs=0,Diamonds=1,Hearts=2,Spades=3}

    /// <summary>Bounded native ParticleSystem pool, drawn through uGUI so bursts appear over overlay cards.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CardParticleFX:MaskableGraphic
    {
        private sealed class Burst
        {
            public ParticleSystem System;
            public ParticleSystem.Particle[] Particles;
            public Vector3 Position;
            public int Count,Token;
            public Suit Suit;
        }
        private Burst[] pool;
        private int cursor,nextToken;public bool ReduceMotion {get;set;}
        public int AllocatedBurstCount=>pool==null?0:pool.Length;
        public int ActiveBurstCount {get;private set;}
        public int LiveParticleCount {get;private set;}
        public int TotalBurstsPlayed {get;private set;}
        public Suit LastSuit {get;private set;}
        public Vector3 LastWorldPosition {get;private set;}
        public static bool ShouldBurst(int rank,int chain)=>rank>=1&&rank<=13&&chain>=0;
        public static Color SuitColor(Suit suit)
        {
            switch(suit){case Suit.Spades:return new Color(0,230/255f,118/255f);case Suit.Hearts:return new Color(1,23/255f,68/255f);case Suit.Diamonds:return new Color(0,229/255f,1);case Suit.Clubs:return new Color(213/255f,0,249/255f);default:return Color.clear;}
        }
        public void Initialize(int capacity=12,int particlesPerBurst=28)
        {
            if(pool!=null)return;
            capacity=Mathf.Clamp(capacity,1,16);particlesPerBurst=Mathf.Clamp(particlesPerBurst,1,32);
            raycastTarget=false;material=Resources.Load<Material>("TCG/CardSpecularGlow");
            pool=new Burst[capacity];
            for(int i=0;i<capacity;i++)
            {
                var child=new GameObject("SuitBurst-"+i,typeof(ParticleSystem));child.transform.SetParent(transform,false);
                var ps=child.GetComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.playOnAwake=false;main.loop=false;main.maxParticles=particlesPerBurst;main.startLifetime=.65f;main.startSpeed=new ParticleSystem.MinMaxCurve(55,115);main.startSize=new ParticleSystem.MinMaxCurve(6,14);main.simulationSpace=ParticleSystemSimulationSpace.Local;main.useUnscaledTime=true;main.stopAction=ParticleSystemStopAction.None;
                var emission=ps.emission;emission.enabled=false;
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=6;
                ps.GetComponent<ParticleSystemRenderer>().enabled=false;
                pool[i]=new Burst{System=ps,Particles=new ParticleSystem.Particle[particlesPerBurst]};
            }
        }
        public void PlaySuitBurst(Suit suit,Vector3 position){PlayOwnedSuitBurst(suit,position);}
        public int PlayOwnedSuitBurst(Suit suit,Vector3 position)
        {
            if((int)suit<0||(int)suit>3||!isActiveAndEnabled)return 0;
            if(pool==null)Initialize();
            Burst burst=null;
            for(int i=0;i<pool.Length;i++){int index=(cursor+i)%pool.Length;if(pool[index].Count==0){burst=pool[index];cursor=(index+1)%pool.Length;break;}}
            if(burst==null){burst=pool[cursor];cursor=(cursor+1)%pool.Length;}
            burst.System.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            burst.Suit=suit;burst.Position=transform.InverseTransformPoint(position);burst.Position.z=0;burst.Token=unchecked(++nextToken);if(burst.Token==0)burst.Token=unchecked(++nextToken);
            var main=burst.System.main;main.startColor=SuitColor(suit);main.startSpeed=ReduceMotion?new ParticleSystem.MinMaxCurve(0):new ParticleSystem.MinMaxCurve(55,115);main.startLifetime=ReduceMotion?.15f:.65f;
            var force=burst.System.forceOverLifetime;force.enabled=true;force.space=ParticleSystemSimulationSpace.Local;force.y=ReduceMotion?0:suit==Suit.Spades?65:suit==Suit.Hearts?-45:suit==Suit.Diamonds?10:30;
            burst.System.Emit(burst.Particles.Length);burst.System.Pause();burst.Count=burst.System.GetParticles(burst.Particles);
            LastSuit=suit;LastWorldPosition=position;TotalBurstsPlayed++;Recount();SetVerticesDirty();return burst.Token;
        }
        public void StopBurst(int token)
        {
            if(token==0||pool==null)return;
            for(int i=0;i<pool.Length;i++)if(pool[i].Token==token){Clear(pool[i]);Recount();SetVerticesDirty();return;}
        }
        private static void Clear(Burst burst){burst.System.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);burst.Count=burst.Token=0;}
        public void ResetPool(){if(pool!=null)for(int i=0;i<pool.Length;i++)Clear(pool[i]);ActiveBurstCount=LiveParticleCount=0;SetVerticesDirty();}
        private void Recount(){ActiveBurstCount=LiveParticleCount=0;for(int i=0;i<pool.Length;i++){if(pool[i].Count>0)ActiveBurstCount++;LiveParticleCount+=pool[i].Count;}}
        private void Update(){Step(Time.unscaledDeltaTime);}
        public void Step(float delta)
        {
            if(pool==null||ActiveBurstCount==0)return;
            for(int i=0;i<pool.Length;i++)if(pool[i].Count>0)
            {
                var burst=pool[i];burst.System.Simulate(Mathf.Max(0,delta),false,false,false);burst.Count=burst.System.GetParticles(burst.Particles);if(burst.Count==0)burst.Token=0;
            }
            Recount();SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();if(pool==null)return;
            for(int b=0;b<pool.Length;b++)for(int p=0;p<pool[b].Count;p++)
            {
                var burst=pool[b];var particle=burst.Particles[p];var center=burst.Position+particle.position;
                Color color=particle.startColor;color.a*=Mathf.Clamp01(particle.remainingLifetime/Mathf.Max(.001f,particle.startLifetime));
                float size=particle.startSize;int sides=burst.Suit==Suit.Clubs?12:8;
                int first=vh.currentVertCount;var vertex=UIVertex.simpleVert;vertex.position=center;vertex.color=color;vh.AddVert(vertex);
                for(int j=0;j<sides;j++)
                {
                    float a=(j/(float)sides)*Mathf.PI*2+particle.rotation*Mathf.Deg2Rad;
                    float r=(j%2==0?1:.3f)*size;float stretch=burst.Suit==Suit.Hearts?1.9f:burst.Suit==Suit.Spades?1.35f:1;
                    vertex.position=center+new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r*stretch);vertex.color=new Color(color.r,color.g,color.b,0);vh.AddVert(vertex);
                }
                for(int j=0;j<sides;j++)vh.AddTriangle(first,first+1+j,first+1+(j+1)%sides);
            }
        }
        protected override void OnDisable(){ResetPool();base.OnDisable();}
    }
}
