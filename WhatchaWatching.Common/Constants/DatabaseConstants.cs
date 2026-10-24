namespace WhatchaWatching.Common.Constants;

public static class DatabaseConstants
{
    /// <summary>
    /// Name of the connection string (<c>ConnectionStrings:DefaultConnection</c>).
    /// It matches the database resource declared in the Aspire AppHost, which injects it for the web app.
    /// </summary>
    public const string CONNECTION_NAME = "DefaultConnection";

    /// <summary>
    /// Optional setting (<c>Database:ServerVersion</c>, for example <c>8.0.36-mysql</c> or <c>10.11.0-mariadb</c>).
    /// When it is absent the version is detected by connecting to the database.
    /// </summary>
    public const string SERVER_VERSION_KEY = "Database:ServerVersion";
}
