using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel;

namespace WebApplication2.Infrastructure.Analytics.Dtos;

public class Friend
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Name { get; set; } = default!;
    public int Age { get; set; }
}
