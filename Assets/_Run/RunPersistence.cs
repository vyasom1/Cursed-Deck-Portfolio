using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using CursedDeck.Core;

namespace CursedDeck.Run
{
    internal static class JsonCodec
    {
        internal static string Write<T>(T value)
        {
            using(var stream=new MemoryStream()) { new DataContractJsonSerializer(typeof(T)).WriteObject(stream,value); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        internal static T Read<T>(string json)
        {
            if(string.IsNullOrEmpty(json) || json.Length>2000000) throw new SerializationException("Save is empty or too large.");
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json))) return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(stream);
        }
        internal static string Hash(string text)
        {
            using(var sha=SHA256.Create()) return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(text)));
        }
    }
    [DataContract] internal sealed class RunSnapshot
    {
        [DataMember(Order=0)] public int Seed;
        [DataMember(Order=1)] public int Floor;
        [DataMember(Order=2)] public int Round;
        [DataMember(Order=3)] public int Gold;
        [DataMember(Order=4)] public bool LastBreathUsed;
        [DataMember(Order=5)] public RunStage Stage;
        [DataMember(Order=6)] public string[] Relics;
        [DataMember(Order=7)] public int[] Consumables;
        internal static RunSnapshot From(RunState state)
        {
            var ids=new string[state.Relics.Count]; for(int i=0;i<ids.Length;i++) ids[i]=state.Relics[i].Id;
            return new RunSnapshot {Seed=state.Seed,Floor=state.Floor,Round=state.Round,Gold=state.Gold,
                LastBreathUsed=state.LastBreathUsed,Stage=state.Stage,Relics=ids,
                Consumables=new[] {state.Consumables[ConsumableKind.Undo],state.Consumables[ConsumableKind.Peek],state.Consumables[ConsumableKind.ShuffleDrawPile]}};
        }
        internal RunState Restore(IReadOnlyList<RelicSpec> catalog)
        {
            if(Floor<1 || Floor>3 || Round<1 || Round>3 || Gold<0 || !Enum.IsDefined(typeof(RunStage),Stage) ||
                Relics==null || Relics.Length>32 || Consumables==null || Consumables.Length!=3)
                throw new SerializationException("Invalid run snapshot.");
            var run=new RunState(Seed,Gold) {Floor=Floor,Round=Round,Stage=Stage}; run.RestoreLastBreath(LastBreathUsed);
            foreach(string id in Relics)
            {
                RelicSpec found=null;
                for(int i=0;i<catalog.Count;i++) if(StringComparer.Ordinal.Equals(catalog[i].Id,id)) { found=catalog[i]; break; }
                if(found==null || !run.Acquire(found)) throw new SerializationException("Unknown/duplicate saved relic.");
            }
            for(int i=0;i<3;i++)
            {
                if(Consumables[i]<0 || Consumables[i]>10000) throw new SerializationException("Invalid consumable count.");
                if(Consumables[i]>0) run.Consumables.Add((ConsumableKind)i,Consumables[i]);
            }
            return run;
        }
    }
    [DataContract] internal sealed class RelicData
    {
        [DataMember(Order=0)] public string Id;
        [DataMember(Order=1)] public RelicKind Kind;
        [DataMember(Order=2)] public int Order;
        [DataMember(Order=3)] public int Amount;
        [DataMember(Order=4)] public int Interval;
        [DataMember(Order=5)] public decimal Multiplier;
        internal static RelicData From(RelicSpec spec) => new RelicData {Id=spec.Id,Kind=spec.Kind,Order=spec.Order,Amount=spec.Amount,Interval=spec.Interval,Multiplier=spec.Multiplier};
        internal RelicSpec Restore() => new RelicSpec(Id,Kind,Order,Amount,Interval,Multiplier);
    }
    [DataContract] internal sealed class SlotData
    {
        [DataMember(Order=0)] public int X;
        [DataMember(Order=1)] public int Y;
        [DataMember(Order=2)] public int[] Covering;
    }
    [DataContract] internal sealed class ActionData
    {
        [DataMember(Order=0)] public RunActionKind Kind;
        [DataMember(Order=1)] public int Value;
    }
    [DataContract] internal sealed class RunSaveData
    {
        [DataMember(Order=0)] public int GeneratorVersion;
        [DataMember(Order=1)] public RunConfig Config;
        [DataMember(Order=2)] public RelicData[] Catalog;
        [DataMember(Order=3)] public SlotData[] Layout;
        [DataMember(Order=4)] public RunSnapshot RoundStart;
        [DataMember(Order=5)] public int RoundSeed;
        [DataMember(Order=6)] public ActionData[] Actions;
        [DataMember(Order=7)] public bool RewardResolved;
        [DataMember(Order=8)] public string RewardChoice;
        [DataMember(Order=9)] public string[] Purchases;
        [DataMember(Order=10)] public RunSnapshot Expected;
    }
    [DataContract] internal sealed class SaveEnvelope
    {
        [DataMember(Order=0)] public int Version;
        [DataMember(Order=1)] public string Payload;
        [DataMember(Order=2)] public string Checksum;
    }

    public sealed partial class RunController
    {
        public string ToJson()
        {
            var specs=new RelicData[catalog.Length]; for(int i=0;i<specs.Length;i++) specs[i]=RelicData.From(catalog[i]);
            var slots=new SlotData[layout.Count];
            for(int i=0;i<slots.Length;i++)
            {
                var covers=new int[layout[i].CoveringSlots.Count]; for(int j=0;j<covers.Length;j++) covers[j]=layout[i].CoveringSlots[j];
                slots[i]=new SlotData {X=layout[i].X,Y=layout[i].Y,Covering=covers};
            }
            var ledger=new ActionData[actions.Count];
            for(int i=0;i<ledger.Length;i++) ledger[i]=new ActionData {Kind=actions[i].Kind,Value=actions[i].Value};
            string payload=JsonCodec.Write(new RunSaveData {GeneratorVersion=GeneratedBoard.GeneratorVersion,Config=Config,
                Catalog=specs,Layout=slots,RoundStart=roundStart,RoundSeed=roundSeed,Actions=ledger,
                RewardResolved=rewardResolved,RewardChoice=rewardChoice,Purchases=purchases.ToArray(),Expected=RunSnapshot.From(State)});
            return JsonCodec.Write(new SaveEnvelope {Version=1,Payload=payload,Checksum=JsonCodec.Hash(payload)});
        }
        public static RunController FromJson(string json)
        {
            var envelope=JsonCodec.Read<SaveEnvelope>(json);
            if(envelope==null || envelope.Version!=1 || envelope.Payload==null || envelope.Checksum!=JsonCodec.Hash(envelope.Payload))
                throw new SerializationException("Unsupported or damaged save; original file preserved.");
            var data=JsonCodec.Read<RunSaveData>(envelope.Payload);
            if(data==null || data.GeneratorVersion!=GeneratedBoard.GeneratorVersion || data.Config==null || data.Catalog==null ||
                data.Catalog.Length>32 || data.Layout==null || (data.Layout.Length!=19 && data.Layout.Length!=28) || data.RoundStart==null || data.Expected==null ||
                data.Actions==null || data.Actions.Length>4096 || data.Purchases==null || data.Purchases.Length>6)
                throw new SerializationException("Invalid save payload.");
            data.Config.Validate();
            var catalog=new RelicSpec[data.Catalog.Length]; for(int i=0;i<catalog.Length;i++) catalog[i]=data.Catalog[i].Restore();
            var slots=new BoardSlot[data.Layout.Length]; for(int i=0;i<slots.Length;i++) slots[i]=new BoardSlot(data.Layout[i].X,data.Layout[i].Y,data.Layout[i].Covering);
            var controller=new RunController(data.RoundStart.Seed,catalog,data.Config,new BoardLayout(slots));
            controller.State=data.RoundStart.Restore(catalog); controller.roundStart=data.RoundStart; controller.roundSeed=data.RoundSeed;
            controller.BuildRound();
            foreach(var action in data.Actions)
                if(action==null || !controller.Execute(new RunAction(action.Kind,action.Value))) throw new SerializationException("Saved command cannot be replayed.");
            if(data.RewardResolved && !controller.ChooseReward(data.RewardChoice)) throw new SerializationException("Saved reward cannot be restored.");
            foreach(string purchase in data.Purchases) if(!controller.Buy(purchase)) throw new SerializationException("Saved purchase cannot be restored.");
            if(JsonCodec.Write(RunSnapshot.From(controller.State))!=JsonCodec.Write(data.Expected)) throw new SerializationException("Replayed save differs from expected state.");
            return controller;
        }
    }

    public sealed class RunSaveStore
    {
        public string Path { get; }
        public Func<string,Exception,bool> CorruptionRecovery {get;set;}
        private bool damagedPrimary,writesBlocked;
        public RunSaveStore(string path)
        {
            if(string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            Path=System.IO.Path.GetFullPath(path);
        }
        public void Save(RunController controller)
        {
            if(controller==null) throw new ArgumentNullException(nameof(controller));
            if(writesBlocked)throw new IOException("Damaged checkpoint could not be preserved; writes are blocked until explicit reset.");
            string json=controller.ToJson();
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string temporary=Path+".tmp";
            using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(json); stream.Write(bytes,0,bytes.Length); stream.Flush(true);
            }
            if(damagedPrimary && File.Exists(Path)){File.Delete(Path);File.Move(temporary,Path);damagedPrimary=false;}
            else if(File.Exists(Path)) File.Replace(temporary,Path,Path+".bak");
            else File.Move(temporary,Path);
        }
        public bool TryLoad(out RunController controller,out string warning)
        {
            controller=null; warning=null; damagedPrimary=false;writesBlocked=false;
            foreach(string file in new[] {Path,Path+".bak"})
            {
                if(!File.Exists(file)) continue;
                try
                {
                    var info=new FileInfo(file); if(info.Length>2000000) throw new SerializationException("Save is too large.");
                    controller=RunController.FromJson(File.ReadAllText(file));
                    if(file!=Path) warning="Recovered the previous checkpoint; damaged save was preserved.";
                    return true;
                }
                catch(Exception ex) when(ex is IOException || ex is UnauthorizedAccessException || ex is System.Xml.XmlException || ex is SerializationException || ex is ArgumentException || ex is InvalidOperationException || ex is OverflowException || ex is NullReferenceException)
                { if(CorruptionRecovery!=null){bool preserved=CorruptionRecovery(file,ex);writesBlocked|=!preserved;if(file==Path)damagedPrimary=preserved;} warning="Could not load checkpoint: "+ex.Message; }
            }
            return false;
        }
    }
    /// <summary>Platform-independent launch/close boundary; Unity lifecycle hooks call this in Phase 5.</summary>
    public sealed class RunLifecycle
    {
        private readonly RunSaveStore store;
        public RunController Controller { get; }
        public string Warning { get; }
        public bool Resumed { get; }
        public RunLifecycle(RunSaveStore store,int newSeed,IReadOnlyList<RelicSpec> catalog,RunConfig config=null,BoardLayout layout=null)
        {
            this.store=store ?? throw new ArgumentNullException(nameof(store));
            Resumed=store.TryLoad(out RunController loaded,out string warning);
            Controller=Resumed ? loaded : new RunController(newSeed,catalog,config,layout);
            Warning=warning;
        }
        public void Close() => store.Save(Controller);
    }
}
