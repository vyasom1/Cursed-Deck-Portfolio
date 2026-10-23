using System;
using System.IO;
namespace CursedDeck.Meta
{
    public static class RecoveryJournal
    {
        private static string Unique(string path,string suffix)
        { string candidate=path+suffix;int n=0;while(File.Exists(candidate))candidate=path+"."+DateTime.UtcNow.Ticks+"."+(++n)+suffix;return candidate; }
        public static bool Preserve(string path,Exception error)
        {
            bool copied=true;
            try{if(File.Exists(path))File.Copy(path,Unique(path,".corrupt.bak"),false);}
            catch(IOException){copied=false;}catch(UnauthorizedAccessException){copied=false;}
            try{Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path),"save-recovery.log"),DateTime.UtcNow.ToString("O")+" | "+System.IO.Path.GetFileName(path)+" | "+error+" | raw preserved="+copied+Environment.NewLine);}
            catch(IOException){copied=false;}catch(UnauthorizedAccessException){copied=false;}
            return copied;
        }
        public static void ArchiveForReset(string path)
        { if(File.Exists(path))File.Move(path,Unique(path,".reset.bak")); }
    }
}
