namespace CommonZones.Models;

/// <summary>Stores information needed to connect to a MySQL connection.</summary>
public struct MySqlData
{
    /// <summary>IP/Host Address</summary>
    public string Host;
    /// <summary>Database Name</summary>
    public string Database;
    /// <summary>User Password</summary>
    public string Password;
    /// <summary>User Name</summary>
    public string Username;
    /// <summary>Port, default is 3306</summary>
    public ushort Port;
    /// <summary>Character set to use when connecting</summary>
    public string CharSet;

    public MySqlData(string host, string database, string password, string username, ushort port, string charSet)
    {
        Host = host;
        Database = database;
        Password = password;
        Username = username;
        Port = port;
        CharSet = charSet;
    }

    /// <summary>Generated on run, connection string for mysql connectors.</summary>
    /// <remarks><code>server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};</code></remarks>
    public readonly string GetConnectionString() => $"server={Host};port={Port};database={Database};uid={Username};password={Password};charset={CharSet};Allow User Variables=True;";
}
