using Microsoft.Extensions.Options;
using MongoDB.Driver;
using WebApplication2.Infrastructure.Books.Dtos;

namespace WebApplication2.Infrastructure.Books;

public class BooksRepository
{
    private readonly IMongoCollection<Book> _booksCollection;
    private readonly IMongoClient _client;

    public BooksRepository(
        IOptions<MongoDatabaseSettings> bookStoreDatabaseSettings)
    {
        //setx MONGO_URI "mongodb://..."

        //var connectionString = Environment.GetEnvironmentVariable("MONGO_URI");

        _client = new MongoClient(
            bookStoreDatabaseSettings.Value.ConnectionString);

        var mongoDatabase = _client.GetDatabase(
            bookStoreDatabaseSettings.Value.DatabaseName);

        _booksCollection = mongoDatabase.GetCollection<Book>(
            bookStoreDatabaseSettings.Value.BooksCollectionName);
    }

    public async Task<List<Book>> GetAsync()
    {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        watch.Start();
        var books = await _booksCollection.Find(_ => true).ToListAsync();
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Console.WriteLine($"Elapsed time to fetch books: {elapsedMs} ms");

        return books;
    }


    public async Task<Book?> GetAsync(int id)
    {
        return new Book();
    }

    public async Task<Book?> GetAsync(string id) =>
        await _booksCollection.Find(x => x.Id == id).FirstOrDefaultAsync();

    public async Task CreateAsync(Book newBook)
    {
        using var session = await _client.StartSessionAsync();
        try
        {
            session.StartTransaction();
            await _booksCollection.InsertOneAsync(newBook);
            session.CommitTransaction();
        }
        catch (Exception)
        {
            session.AbortTransaction();
            throw;
        }
    }

    public async Task UpdateAsync(string id, Book updatedBook) =>
        await _booksCollection.ReplaceOneAsync(x => x.Id == id, updatedBook);

    public async Task RemoveAsync(string id) =>
        await _booksCollection.DeleteOneAsync(x => x.Id == id);
}
