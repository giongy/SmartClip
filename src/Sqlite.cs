using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// SQLite, the copy that comes with Windows 10 and 11 (winsqlite3.dll): nothing to install or ship beside the
// program. Only what the program needs: open a file, run statements with values bound to ?1, ?2…, read rows,
// transactions. Values are always bound, never written into the text of a statement. Used from one thread.
sealed class Database : IDisposable
{
    IntPtr handle;
    readonly HashSet<Statement> open = new HashSet<Statement>();   // closed along with the database, which frees the file
    int depth;   // transactions inside one another, each a savepoint
    const int BusyMs = 2000;   // how long to wait for a file another program holds for a moment (an antivirus, a sync tool)

    // Opens the database at 'path', creating it if it is not there; ":memory:" is one that lives in memory only.
    public Database(string path)
    {
        IntPtr opened;
        int code = Sqlite.sqlite3_open16(path, out opened);
        handle = opened;
        if (code != Sqlite.SQLITE_OK)
        {
            string message = handle == IntPtr.Zero ? "out of memory" : Message;
            Sqlite.sqlite3_close(handle);
            handle = IntPtr.Zero;
            throw new SqliteException(code, message + " (" + path + ")");
        }
        Sqlite.sqlite3_extended_result_codes(handle, 1);   // a constraint says which one it was
        Sqlite.sqlite3_busy_timeout(handle, BusyMs);
    }

    // The version of SQLite in this Windows, as a number: 3051001 is 3.51.1.
    public static int VersionNumber
    {
        get { return Sqlite.sqlite3_libversion_number(); }
    }

    // The oldest SQLite the program works with: 3.8.0 brought the partial index of the schema. Every Windows 10 has a
    // newer one.
    public const int MinimumVersion = 3008000;

    public static string MinimumName
    {
        get { return MinimumVersion / 1000000 + "." + MinimumVersion / 1000 % 1000 + "." + MinimumVersion % 1000; }
    }

    public static string Version
    {
        get { return Marshal.PtrToStringAnsi(Sqlite.sqlite3_libversion()); }
    }

    internal IntPtr Handle
    {
        get
        {
            if (handle == IntPtr.Zero) throw new ObjectDisposedException("Database");
            return handle;
        }
    }

    internal string Message
    {
        get { return Marshal.PtrToStringUni(Sqlite.sqlite3_errmsg16(handle)); }
    }

    // Rows changed by the last INSERT, UPDATE or DELETE, and the id of the last row inserted.
    public int Changes
    {
        get { return Sqlite.sqlite3_changes(Handle); }
    }

    public long LastId
    {
        get { return Sqlite.sqlite3_last_insert_rowid(Handle); }
    }

    // Runs every statement of 'sql' to its end, each with the values it asks for: a whole schema at once.
    public void Execute(string sql, params object[] values)
    {
        IntPtr text = Marshal.StringToHGlobalUni(sql);
        try
        {
            long at = text.ToInt64(), end = at + sql.Length * 2L;
            while (at < end)
            {
                IntPtr prepared, tail;
                Check(Sqlite.sqlite3_prepare16_v2(Handle, new IntPtr(at), (int)(end - at), out prepared, out tail), sql);
                at = tail.ToInt64();
                if (prepared == IntPtr.Zero) continue;   // only spaces or a comment
                using (Statement statement = new Statement(this, prepared, sql))
                {
                    statement.Bind(values, false);
                    while (statement.Step()) { }
                }
            }
        }
        finally
        {
            Marshal.FreeHGlobal(text);
        }
    }

    // One statement, ready to step through its rows (Step) or to run again and again with other values (Run).
    public Statement Prepare(string sql, params object[] values)
    {
        IntPtr text = Marshal.StringToHGlobalUni(sql), prepared = IntPtr.Zero;
        try
        {
            IntPtr tail;
            Check(Sqlite.sqlite3_prepare16_v2(Handle, text, sql.Length * 2, out prepared, out tail), sql);
            if (prepared == IntPtr.Zero || sql.Substring((int)((tail.ToInt64() - text.ToInt64()) / 2)).Trim().Length > 0)
                throw new ArgumentException("One statement is expected: " + sql);
        }
        catch (Exception)
        {
            Sqlite.sqlite3_finalize(prepared);
            throw;
        }
        finally
        {
            Marshal.FreeHGlobal(text);
        }
        Statement statement = new Statement(this, prepared, sql);
        try
        {
            if (values.Length > 0) statement.Bind(values, true);
        }
        catch (Exception)
        {
            statement.Dispose();
            throw;
        }
        return statement;
    }

    // The first column of the first row, or null if there is none.
    public object Value(string sql, params object[] values)
    {
        using (Statement statement = Prepare(sql, values)) return statement.Step() ? statement.Get(0) : null;
    }

    // Everything done until Commit happens at once, or not at all: disposed without Commit, it is undone. One can
    // be opened inside another, and then only its own part is undone.
    public Transaction Begin()
    {
        string name = "t" + depth;
        Execute("SAVEPOINT " + name);
        depth++;
        return new Transaction(this, name);
    }

    internal void End(string name, bool commit)
    {
        depth--;
        if (!commit) Execute("ROLLBACK TO " + name);
        Execute("RELEASE " + name);
    }

    internal void Opened(Statement statement)
    {
        open.Add(statement);
    }

    internal void Closed(Statement statement)
    {
        open.Remove(statement);
    }

    internal void Check(int code, string sql)
    {
        if (code != Sqlite.SQLITE_OK) throw new SqliteException(code, Message + " (" + sql + ")");
    }

    // Closes the file for good: the statements still open are finalized first, or SQLite would keep it open.
    public void Dispose()
    {
        if (handle == IntPtr.Zero) return;
        foreach (Statement statement in new List<Statement>(open)) statement.Dispose();
        Sqlite.sqlite3_close(handle);
        handle = IntPtr.Zero;
    }
}

// A statement of a database, with its values bound: its rows are read one at a time.
sealed class Statement : IDisposable
{
    readonly Database database;
    readonly string sql;
    IntPtr handle;

    internal Statement(Database database, IntPtr handle, string sql)
    {
        this.database = database;
        this.handle = handle;
        this.sql = sql;
        database.Opened(this);
    }

    IntPtr Handle
    {
        get
        {
            if (handle == IntPtr.Zero) throw new ObjectDisposedException("Statement");
            return handle;
        }
    }

    // Binds the values to ?1, ?2…: null, a string, a number (int, long, double), a bool (as 1 or 0), bytes. 'exact':
    // as many as the statement asks for; otherwise only those it asks for, as when one set of values serves several.
    internal void Bind(object[] values, bool exact)
    {
        int wanted = Sqlite.sqlite3_bind_parameter_count(Handle);
        if (exact && values.Length != wanted)
            throw new ArgumentException(wanted + " values are expected, not " + values.Length + ": " + sql);
        for (int i = 0; i < Math.Min(values.Length, wanted); i++) Bind(i + 1, values[i]);
    }

    void Bind(int index, object value)
    {
        int code;
        string text = value as string;
        byte[] bytes = value as byte[];
        if (value == null) code = Sqlite.sqlite3_bind_null(Handle, index);
        // With its length: a text can hold the character zero, which would otherwise end it there.
        else if (text != null) code = Sqlite.sqlite3_bind_text16(Handle, index, text, text.Length * 2, Sqlite.SQLITE_TRANSIENT);
        // Empty bytes are not nothing: bound as such, SQLite would read them as NULL.
        else if (bytes != null) code = bytes.Length == 0 ? Sqlite.sqlite3_bind_zeroblob(Handle, index, 0)
            : Sqlite.sqlite3_bind_blob(Handle, index, bytes, bytes.Length, Sqlite.SQLITE_TRANSIENT);
        else if (value is int || value is long) code = Sqlite.sqlite3_bind_int64(Handle, index, Convert.ToInt64(value));
        else if (value is bool) code = Sqlite.sqlite3_bind_int64(Handle, index, (bool)value ? 1 : 0);
        else if (value is double) code = Sqlite.sqlite3_bind_double(Handle, index, (double)value);
        else throw new ArgumentException("A value SQLite does not take: " + value.GetType().Name);
        database.Check(code, sql);
    }

    // The next row: false once there are no more, and when a statement that returns none is done.
    public bool Step()
    {
        int code = Sqlite.sqlite3_step(Handle);
        if (code == Sqlite.SQLITE_ROW) return true;
        if (code == Sqlite.SQLITE_DONE) return false;
        string message = database.Message;
        Sqlite.sqlite3_reset(handle);   // ready to run again: the error has been said
        throw new SqliteException(code, message + " (" + sql + ")");
    }

    // Runs it again with other values, to its end: the same insertion for many rows, prepared once.
    public void Run(params object[] values)
    {
        Sqlite.sqlite3_reset(Handle);
        Sqlite.sqlite3_clear_bindings(Handle);
        Bind(values, true);
        while (Step()) { }
    }

    // The columns of the row just stepped to, from 0.
    public int Columns
    {
        get { return Sqlite.sqlite3_column_count(Handle); }
    }

    public bool IsNull(int column)
    {
        return Sqlite.sqlite3_column_type(Handle, column) == Sqlite.SQLITE_NULL;
    }

    public long Long(int column)
    {
        return Sqlite.sqlite3_column_int64(Handle, column);
    }

    public int Int(int column)
    {
        return (int)Long(column);
    }

    public bool Bool(int column)
    {
        return Long(column) != 0;
    }

    // Null for NULL.
    public string Text(int column)
    {
        if (IsNull(column)) return null;
        IntPtr text = Sqlite.sqlite3_column_text16(Handle, column);   // first: it decides the length in bytes
        return Marshal.PtrToStringUni(text, Sqlite.sqlite3_column_bytes16(Handle, column) / 2);
    }

    // Null for NULL; empty bytes stay empty.
    public byte[] Blob(int column)
    {
        if (IsNull(column)) return null;
        IntPtr data = Sqlite.sqlite3_column_blob(Handle, column);
        byte[] bytes = new byte[Sqlite.sqlite3_column_bytes(Handle, column)];
        if (bytes.Length > 0) Marshal.Copy(data, bytes, 0, bytes.Length);
        return bytes;
    }

    // Whatever the column holds, as SQLite has it: long, double, string, bytes or null.
    public object Get(int column)
    {
        switch (Sqlite.sqlite3_column_type(Handle, column))
        {
            case Sqlite.SQLITE_INTEGER: return Long(column);
            case Sqlite.SQLITE_FLOAT: return Sqlite.sqlite3_column_double(Handle, column);
            case Sqlite.SQLITE_TEXT: return Text(column);
            case Sqlite.SQLITE_BLOB: return Blob(column);
            default: return null;
        }
    }

    public void Dispose()
    {
        if (handle == IntPtr.Zero) return;
        Sqlite.sqlite3_finalize(handle);
        handle = IntPtr.Zero;
        database.Closed(this);
    }
}

// See Database.Begin.
sealed class Transaction : IDisposable
{
    readonly Database database;
    readonly string name;
    bool ended;

    internal Transaction(Database database, string name)
    {
        this.database = database;
        this.name = name;
    }

    public void Commit()
    {
        ended = true;
        database.End(name, true);
    }

    public void Dispose()
    {
        if (ended) return;
        ended = true;
        database.End(name, false);
    }
}

// What SQLite refused, with its code and its own words, and the statement it was running (never the values).
class SqliteException : Exception
{
    public readonly int Code;   // the extended code: its low byte is the primary one

    public SqliteException(int code, string message) : base(message)
    {
        Code = code;
    }

    // A rule of the database was broken: a name or a number already taken, a CHECK, a missing folder.
    public bool IsConstraint
    {
        get { return (Code & 0xFF) == Sqlite.SQLITE_CONSTRAINT; }
    }
}

// The functions of winsqlite3.dll, with their official names. It uses the calling convention of Windows (stdcall),
// which counts only in a 32-bit process.
static class Sqlite
{
    const string Dll = "winsqlite3.dll";
    public const int SQLITE_OK = 0, SQLITE_CONSTRAINT = 19, SQLITE_ROW = 100, SQLITE_DONE = 101;
    public const int SQLITE_INTEGER = 1, SQLITE_FLOAT = 2, SQLITE_TEXT = 3, SQLITE_BLOB = 4, SQLITE_NULL = 5;
    public static readonly IntPtr SQLITE_TRANSIENT = new IntPtr(-1);   // SQLite copies the value at once

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr sqlite3_libversion();

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_libversion_number();

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int sqlite3_open16(string path, out IntPtr database);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_close(IntPtr database);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_extended_result_codes(IntPtr database, int on);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_busy_timeout(IntPtr database, int milliseconds);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr sqlite3_errmsg16(IntPtr database);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_changes(IntPtr database);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern long sqlite3_last_insert_rowid(IntPtr database);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_prepare16_v2(IntPtr database, IntPtr sql, int bytes, out IntPtr statement, out IntPtr tail);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_step(IntPtr statement);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_reset(IntPtr statement);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_clear_bindings(IntPtr statement);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_finalize(IntPtr statement);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_bind_parameter_count(IntPtr statement);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_bind_null(IntPtr statement, int index);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_bind_int64(IntPtr statement, int index, long value);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_bind_double(IntPtr statement, int index, double value);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode)]
    public static extern int sqlite3_bind_text16(IntPtr statement, int index, string text, int bytes, IntPtr destructor);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_bind_blob(IntPtr statement, int index, byte[] data, int bytes, IntPtr destructor);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_bind_zeroblob(IntPtr statement, int index, int bytes);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_column_count(IntPtr statement);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_column_type(IntPtr statement, int column);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern long sqlite3_column_int64(IntPtr statement, int column);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern double sqlite3_column_double(IntPtr statement, int column);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr sqlite3_column_text16(IntPtr statement, int column);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_column_bytes16(IntPtr statement, int column);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr sqlite3_column_blob(IntPtr statement, int column);

    [DllImport(Dll, CallingConvention = CallingConvention.StdCall)]
    public static extern int sqlite3_column_bytes(IntPtr statement, int column);
}
