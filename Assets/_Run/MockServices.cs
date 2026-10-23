using System;
using System.IO;
using System.Globalization;
namespace CursedDeck.Run
{
    public interface IRewardedAds {bool Complete(string placement);}
    public interface IPurchases {bool Purchase(string product,out string receipt);}
    public interface IAnalytics {void Track(string name,int seed,int floor,int round,decimal score);}
    public sealed class MockRewardedAds:IRewardedAds {public bool Succeed {get;set;}=true;public int Requests {get;private set;}public bool Complete(string placement){if(placement!="extra-draw"&&placement!="revive")return false;Requests++;return Succeed;}}
    public sealed class MockPurchases:IPurchases {public bool Purchase(string product,out string receipt){receipt=null;if(product!="support-demo")return false;receipt="MOCK-NO-CHARGE-"+Guid.NewGuid().ToString("N");return true;}}
    public sealed class LocalAnalytics:IAnalytics
    {
        private readonly string path;
        public LocalAnalytics(string path){this.path=Path.GetFullPath(path);}
        public void Track(string name,int seed,int floor,int round,decimal score){if(name!="checkpoint"&&name!="test-ad"&&name!="mock-iap")throw new ArgumentException("Unknown event.");Directory.CreateDirectory(Path.GetDirectoryName(path));File.AppendAllText(path,DateTime.UtcNow.ToString("O",CultureInfo.InvariantCulture)+","+name+","+seed+","+floor+","+round+","+score.ToString(CultureInfo.InvariantCulture)+"\n");}
    }
    public sealed class RewardedActions
    {
        private readonly IRewardedAds ads;
        public RewardedActions(IRewardedAds ads){this.ads=ads??throw new ArgumentNullException(nameof(ads));}
        public bool TryComplete(RunController run,bool revive){if(revive?!run.CanRevive:!run.CanRewardDraw)return false;if(!ads.Complete(revive?"revive":"extra-draw"))return false;return run.Execute(new RunAction(revive?RunActionKind.Revive:RunActionKind.RewardDraw));}
    }
}
