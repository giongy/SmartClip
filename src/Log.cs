using System;
using System.IO;
using System.Text;

// What went wrong, written to a small text file beside the data of the program, so that a problem a user reports
// can be looked into. Only what failed and where, never what was copied or saved.
static class Log
{
    const long MaxLength = 256 * 1024;   // beyond this the file starts again, the previous one kept beside it
    const string OldSuffix = ".old";
    public static string FileName = Path.Combine(Program.DataFolder, Program.AppName + ".log");   // the checks write elsewhere
    static readonly object writing = new object();

    // Never fails: a log that cannot be written must not become a problem of its own.
    public static void Error(Exception ex)
    {
        if (ex == null) return;
        try
        {
            lock (writing)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FileName));
                FileInfo file = new FileInfo(FileName);
                if (file.Exists && file.Length > MaxLength)
                {
                    File.Delete(FileName + OldSuffix);
                    File.Move(FileName, FileName + OldSuffix);
                }
                File.AppendAllText(FileName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + Program.AppName + " "
                    + Program.Version + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception)
        {
        }
    }
}
