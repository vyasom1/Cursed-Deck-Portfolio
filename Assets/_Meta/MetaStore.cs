using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
namespace CursedDeck.Meta
{
    public sealed class MetaStore
    {
        public string Path {get;}
        public bool RecoveredDefault {get;private set;}
        public bool SaveBlocked {get;private set;}
        private bool damagedPrimary;
        public MetaStore(string path){Path=System.IO.Path.GetFullPath(path);}
        public static string ToJson(MetaProfile profile){profile.Validate();using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(MetaProfile)).WriteObject(stream,profile);return Encoding.UTF8.GetString(stream.ToArray());}}
        public static MetaProfile FromJson(string json){if(string.IsNullOrEmpty(json)||json.Length>2000000)throw new SerializationException("Invalid profile length.");using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json))){var p=(MetaProfile)new DataContractJsonSerializer(typeof(MetaProfile)).ReadObject(stream);if(p==null)throw new SerializationException("Empty profile.");p.Validate();return p;}}
        public void Save(MetaProfile profile){if(SaveBlocked)throw new IOException("Original damaged data could not be archived; writes are blocked until explicit reset.");string text=ToJson(profile);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));string temp=Path+".tmp";using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write)){byte[] data=Encoding.UTF8.GetBytes(text);stream.Write(data,0,data.Length);stream.Flush(true);}if(damagedPrimary&&File.Exists(Path)){File.Delete(Path);File.Move(temp,Path);damagedPrimary=false;}else if(File.Exists(Path))File.Replace(temp,Path,Path+".bak");else File.Move(temp,Path);}
        public MetaProfile Load(out string warning,Func<string,bool> checkpointValidator=null)
        {
            warning=null;RecoveredDefault=false;SaveBlocked=false;damagedPrimary=false;
            foreach(string path in new[]{Path,Path+".bak"})
            {
                if(!File.Exists(path))continue;
                try
                {
                    if(new FileInfo(path).Length>2000000)throw new SerializationException("Profile too large.");
                    var profile=FromJson(File.ReadAllText(path));
                    if(profile.Active?.RunCheckpoint!=null && checkpointValidator!=null && !checkpointValidator(profile.Active.RunCheckpoint))throw new SerializationException("Invalid embedded checkpoint.");
                    if(path!=Path)warning="Recovered profile backup; damaged file preserved.";
                    return profile;
                }
                catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException||ex is SerializationException||ex is System.Xml.XmlException||ex is ArgumentException||ex is InvalidOperationException||ex is OverflowException||ex is NullReferenceException||ex is FormatException)
                { if(path==Path)damagedPrimary=true;SaveBlocked|=!RecoveryJournal.Preserve(path,ex);warning="Profile could not load: "+ex.Message; }
            }
            RecoveredDefault=warning!=null;
            if(RecoveredDefault)warning="Save Data Recovered - Default profile restored";
            return new MetaProfile();
        }
    }
}
