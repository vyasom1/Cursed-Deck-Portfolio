using UnityEngine;
using CursedDeck.Data;
namespace CursedDeck.Bootstrap.Editor
{
    public static class CardCharacterMatrix
    {
        public static readonly string[] SuitNames={"Clubs","Diamonds","Hearts","Spades"};
        private static readonly string[] Titles={"Void Conclave","Frost Constellation","Blood Covenant","Shadow Order"};
        private static readonly string[] Descriptions={
            "Void and arcane faction of void walkers, arcane witches, demon hunters and nether beasts, amethyst purple magic, obsidian armor and forbidden runes",
            "Frost and cosmic faction of frost paladins, crystal golems, star mages and archons, sapphire blue ice, silver armor and celestial constellations",
            "Blood and vampirism faction of vampire lords, blood mages, crimson sirens and thralls, ruby crimson magic, velvet and ornate gothic armor",
            "Shadow and necromancy faction of liches, death knights, wraiths and shadow assassins, emerald green soul fire, blackened armor and ancient bone sigils"};
        private static readonly Color[] Colors={new Color(213/255f,0,249/255f),new Color(0,229/255f,1),new Color(1,23/255f,68/255f),new Color(0,230/255f,118/255f)};
        private static readonly string[][] Names={
            new[]{"Nhal","Netherling Prowler","Amethyst Hexling","Riftmarked Hunter","Void-Touched Oracle","Arcane Covenant Witch","Nethermaw Beast","Obsidian Demon Hunter","Runebound Voidwalker","Abyssal Spellbreaker","Malrik","Vespera","Zerath"},
            new[]{"Asterion","Rimebound Initiate","Shardveil Ranger","Frostglass Sentry","Crystalwake Golem","Starlit Thaumaturge","Aurora Spearwarden","Cometforged Colossus","Celestial Cryomancer","Glacier Oathkeeper","Kaelis","Elyra","Orion"},
            new[]{"Vaelkris","Chalice Thrall","Scarlet Acolyte","Bloodthorn Duelist","Roseveil Siren","Crimson Alchemist","Nightcourt Stalker","Sanguine Haruspex","Velvetfang Huntress","Ruby Cryptwarden","Valerian","Serynthra","Vhorzath"},
            new[]{"Morcant","Ashbound Thrall","Veilstep Cutthroat","Sepulcher Wraith","Gravewatch Sentinel","Bone-Sigil Conjurer","Emerald Banshee","Plaguebound Reaver","Cryptblade Assassin","Dreadmarch Cavalier","Ossvyrn","Nyssara","Xal'Thur"}
        };
        private static readonly string[][] Poses={
            new[]{"hovering above an open rift with a crown of broken runes","crouched with spectral claws","weaving a floating violet hex sigil","drawing a hooked demon-hunting crossbow","cradling a forbidden amethyst orb","raising a spiral staff and spell-bound grimoire","rearing with smoky horns and rune-fanged jaws","standing with paired crescent blades","stepping through a portal with a void spear","shattering a ward with an obsidian warhammer","guarding a rift with a runic halberd","casting with a starless scepter and hovering talismans","seated on a floating basalt throne with an arcane greatsword"},
            new[]{"levitating with a frozen stellar halo and a crystal staff","holding a frost lantern in a defensive stance","drawing an ice-crystal bow","guarding with a faceted sapphire shield","striding with massive glowing crystal fists","conjuring a miniature star over an open spellbook","lunging with a luminous aurora spear","looming with comet-crystal shoulder armor","tracing constellations through a cloud of frost","raising a glacier-forged greatshield and mace","kneeling with an icy longsword held upright","weaving a spiral of silver stars with a sapphire wand","standing in crystalline regalia with an orbital astrolabe"},
            new[]{"standing beneath a ruby moon with an ornate blood-rune chalice","kneeling with a ceremonial silver dagger","casting crimson runes between gloved palms","lunging with an elegant scarlet rapier","singing with a rose-shaped ruby talisman","holding a glowing alchemical vial and a ritual blade","emerging from velvet shadows with twin fang daggers","reading floating crimson sigils above a silver basin","drawing a barbed ruby longbow","guarding a crypt gate with a crimson poleaxe","commanding with a blood-red saber and a gilded cloak","standing with a ruby crown and a thorned scepter","enthroned in vampire plate with a crimson greatsword"},
            new[]{"commanding spectral souls with an emerald necromantic staff","carrying a broken shield and a spectral short sword","crouched with twin shadow daggers","hovering with torn robes and an emerald lantern","braced behind a gravestone shield and a rusted spear","raising a bone wand and a hovering skull grimoire","singing amid spiraling emerald spirit ribbons","advancing with a corroded soul-harvesting scythe","leaping with a curved cryptblade and trailing smoke","riding spectral mist with a hooked cavalry lance","standing in blackened plate with a runeblade","spreading a wraith mantle around a jade soul crystal","standing on a bone dais with a phylactery and a death staff"}
        };
        public static CardCharacterData[] Create()
        {
            var cards=new CardCharacterData[52];
            for(int suit=0;suit<4;suit++)for(int rank=1;rank<=13;rank++)cards[suit*13+rank-1]=new CardCharacterData(suit,rank,Names[suit][rank-1],Titles[suit],Descriptions[suit],Poses[suit][rank-1],Colors[suit]);
            return cards;
        }
    }
}
