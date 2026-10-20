using System;
using System.Globalization;
using System.IO;
using System.Text;

// What went wrong, written to a small text file beside the data of the program, so that a problem a user reports
// can be looked into. Only what failed and where, never what was copied or saved.
static class Log
{
    const long MaxLength = 256 * 1024;   // beyond this the file starts again, the previous one kept beside it
    const string OldSuffix = ".old";
    public static string FileName = FileIn(Program.DataFolder);   // beside the database; the checks write elsewhere
    static readonly object writing = new object();

    // The file of the log in a folder.
    public static string FileIn(string folder)
    {
        return Path.Combine(folder, Program.AppName + ".log");
    }

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
                // The time as written everywhere: in some languages of Windows ':' would become another sign.
                File.AppendAllText(FileName, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) + "  " + Program.AppName + " "
                    + Program.Version + Environment.NewLine + ex + Environment.NewLine + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch (Exception)
        {
        }
    }
}
