using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using WebApplication2.Infrastructure.Analytics.Dtos;
using WebApplication2.Infrastructure.Books.Dtos;

namespace WebApplication2.Infrastructure.Analytics;

public class AnalyticsRepository
{
    private readonly IMongoCollection<Friend> _friendsCollection;
    private readonly IMongoClient _client;

    public AnalyticsRepository(
        IOptions<MongoDatabaseSettings> mongoDatabaseSettings)
    {
        _client = new MongoClient(
            mongoDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = _client.GetDatabase(
            mongoDatabaseSettings.Value.AnalyticsDatabaseName);

        _friendsCollection = mongoDatabase.GetCollection<Friend>(
            mongoDatabaseSettings.Value.FriendsCollectionName);
    }

    public async Task<List<Friend>> GetAsync()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        watch.Start();
        var books = await _friendsCollection.Find(_ => true).ToListAsync();
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Console.WriteLine($"Elapsed time to fetch books: {elapsedMs} ms");

        return books;
    }

    public async Task<PagedResult<Friend>> GetFriendsAsync(
        FriendsQuery query,
        CancellationToken cancellationToken = default)
    {
        var fb = Builders<Friend>.Filter;
        var filter = FilterDefinition<Friend>.Empty;

        if (query.MinAge is int minAge)
            filter &= fb.Gte(f => f.Age, minAge);

        if (query.MaxAge is int maxAge)
            filter &= fb.Lte(f => f.Age, maxAge);

        // 1) nombre total (pour le front : nb de pages, etc.)
        var totalCount = await _friendsCollection.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken);

        // 2) page de données
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var items = await _friendsCollection
            .Find(filter)
            .SortBy(f => f.Age)                            // important : garder un ordre déterministe
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Friend>(items, totalCount, page, pageSize);
    }


    public async Task<CursorPage<Friend>> GetFriendsByCursorAsync(
        string? cursor,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var fb = Builders<Friend>.Filter;
        var filter = FilterDefinition<Friend>.Empty;

        if (!string.IsNullOrEmpty(cursor) )
            {
            //if (ObjectId.TryParse(cursor, out ObjectId lastObjectId))
            //{
            // var lastId = cursor;
            //&& ObjectId.TryParse(cursor, out var lastId)
            filter &= fb.Gt(f => f.Id, cursor);
        }

        var items = await _friendsCollection
            .Find(filter)
            .SortBy(f => f.Id)           // très important pour cursor-based
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        string? nextCursor = items.Count == pageSize
            ? items[^1].Id.ToString()
            : null; // plus de page suivante

        return new CursorPage<Friend>(items, nextCursor);
    }


    public async Task<Friend?> GetAsync(string id)
    {
        //if (ObjectId.TryParse(id, out ObjectId objectId))
        //{
            return await _friendsCollection.Find(x => x.Id == id).FirstOrDefaultAsync();
        //}

    }

    public async Task CreateAsync(Friend newFriend)
    {
        using var session = await _client.StartSessionAsync();
        try
        {
            session.StartTransaction();
            await _friendsCollection.InsertOneAsync(newFriend);
            session.CommitTransaction();
        }
        catch (Exception)
        {
            session.AbortTransaction();
            throw;
        }
    }

    public async Task UpdateAsync(string id, Friend updatedFriend)
    {
        //if (ObjectId.TryParse(id, out ObjectId objectId))
        //{
            await _friendsCollection.ReplaceOneAsync(x => x.Id == id, updatedFriend);
        //}
    }

    public async Task RemoveAsync(string id)
    {
        //if (ObjectId.TryParse(id, out ObjectId objectId))
        //{
            await _friendsCollection.DeleteOneAsync(x => x.Id == id);
        //}
    }
}
