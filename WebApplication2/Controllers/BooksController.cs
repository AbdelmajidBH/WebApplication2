using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using WebApplication2.Infrastructure.Analytics;
using WebApplication2.Infrastructure.Books;
using WebApplication2.Infrastructure.Books.Dtos;

namespace WebApplication2.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly BooksRepository _booksRepository;
    private readonly IOptions<MySettings> _options;
    private readonly IOptionsSnapshot<MySettings> _snapshot;
    private readonly IOptionsMonitor<MySettings> _monitor;

    public BooksController(BooksRepository booksRepository, IOptions<MySettings> options,
        IOptionsSnapshot<MySettings> snapshot,
        IOptionsMonitor<MySettings> monitor)
    {
        _options = options;
        _snapshot = snapshot;
        _monitor = monitor;
        _booksRepository = booksRepository;
    }

    [Authorize("IsAdminEditor")]
    [HttpGet("configs")]
    public IActionResult GetConfig()
    {

        return Ok(new
        {
            Time = DateTime.Now.ToString("HH:mm:ss"),
            Options = _options.Value.Message,
            Snapshot = _snapshot.Value.Message,
            Monitor = _monitor.CurrentValue.Message,
            User.Identity?.IsAuthenticated,
            User.Identity?.Name,
            Claims = User.Claims.Select(c => new { c.Type, c.Value })
        });
    }

    [HttpGet]
    public async Task<List<Book>> Get() =>
        await _booksRepository.GetAsync();

    [Route("orders/{id:int}")]
    public async Task<ActionResult<Book>> GetOrderById(int id)
    {
        var book = await _booksRepository.GetAsync(id);
        return book;
    }

    [HttpGet("{id:length(24)}")]
    public async Task<ActionResult<Book>> Get(string id)
    {
        var book = await _booksRepository.GetAsync(id);

        if (book is null)
        {
            return NotFound();
        }

        return book;
    }

    [HttpPost]
    public async Task<IActionResult> Post(Book newBook)
    {
        await _booksRepository.CreateAsync(newBook);

        return CreatedAtAction(nameof(Get), new { id = newBook.Id }, newBook);
    }

    [HttpPut("{id:length(24)}")]
    public async Task<IActionResult> Update(string id, Book updatedBook)
    {
        var book = await _booksRepository.GetAsync(id);

        if (book is null)
        {
            return NotFound();
        }

        updatedBook.Id = book.Id;

        await _booksRepository.UpdateAsync(id, updatedBook);

        return NoContent();
    }

    [HttpDelete("{id:length(24)}")]
    public async Task<IActionResult> Delete(string id)
    {
        var book = await _booksRepository.GetAsync(id);

        if (book is null)
        {
            return NotFound();
        }

        await _booksRepository.RemoveAsync(id);

        return NoContent();
    }
}

public class MySettings
{
    public string Message { get; set; } = string.Empty;
}
