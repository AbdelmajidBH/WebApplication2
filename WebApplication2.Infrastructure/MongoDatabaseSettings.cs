namespace WebApplication2.Infrastructure;

public class MongoDatabaseSettings
{
    public string ConnectionString { get; set; } = null!;

    public string DatabaseName { get; set; } = null!;

    public string BooksCollectionName { get; set; } = null!;
    public string AnalyticsDatabaseName { get; set; } = null!;

    public string FriendsCollectionName { get; set; } = null!;
}
