using System;
using System.Collections.Generic;
using CursedDeck.Core;

namespace CursedDeck.Run
{
    public sealed class ShopOffer
    {
        public string Id { get; }
        public int Price { get; }
        public ConsumableKind? Consumable { get; }
        public RelicSpec Relic { get; }
        public bool Purchased { get; internal set; }
        internal ShopOffer(string id,int price,ConsumableKind? consumable,RelicSpec relic)
        { Id=id; Price=price; Consumable=consumable; Relic=relic; }
    }

    public sealed class ShopManager
    {
        private readonly RunState run;
        private readonly ShopOffer[] offers;
        public IReadOnlyList<ShopOffer> Offers { get; }
        public ShopManager(RunState run,IReadOnlyList<RelicSpec> catalog,RunConfig config,int seed)
        {
            this.run=run ?? throw new ArgumentNullException(nameof(run));
            if(catalog==null || config==null) throw new ArgumentNullException();
            config.Validate();
            var rng=new SeededRNG(seed); var result=new List<ShopOffer>(6);
            for(int i=0;i<3;i++) result.Add(new ShopOffer("item-"+i,
                config.ConsumablePriceMin+rng.NextInt(config.ConsumablePriceMax-config.ConsumablePriceMin+1),(ConsumableKind)i,null));
            var eligible=new List<RelicSpec>(); var seen=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<catalog.Count;i++)
            {
                if(catalog[i]==null || !seen.Add(catalog[i].Id)) throw new ArgumentException("Invalid catalog.");
                if(!run.Owns(catalog[i].Id)) eligible.Add(catalog[i]);
            }
            eligible.Sort((a,b)=>StringComparer.Ordinal.Compare(a.Id,b.Id)); rng.Shuffle(eligible);
            for(int i=0;i<Math.Min(3,eligible.Count);i++) result.Add(new ShopOffer("relic-"+eligible[i].Id,
                config.RelicPriceMin+rng.NextInt(config.RelicPriceMax-config.RelicPriceMin+1),null,eligible[i]));
            offers=result.ToArray(); Offers=Array.AsReadOnly(offers);
        }
        public bool TryBuy(string id)
        {
            if(run.Stage!=RunStage.Shop) return false;
            for(int i=0;i<offers.Length;i++)
            {
                var offer=offers[i];
                if(!StringComparer.Ordinal.Equals(offer.Id,id)) continue;
                if(offer.Purchased || run.Gold<offer.Price ||
                    (offer.Relic!=null && run.Owns(offer.Relic.Id)) ||
                    (offer.Consumable.HasValue && !run.Consumables.CanAdd(offer.Consumable.Value))) return false;
                if(!run.TrySpendGold(offer.Price)) return false;
                if(offer.Relic!=null) run.Acquire(offer.Relic);
                else run.Consumables.Add(offer.Consumable.Value);
                offer.Purchased=true; return true;
            }
            return false;
        }
    }
}
