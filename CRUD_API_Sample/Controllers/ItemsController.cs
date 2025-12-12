using CRUD_API_Sample.Data;
using CRUD_API_Sample.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CRUD_API_Sample.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ItemsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ItemsController(AppDbContext db) => _db = db;

    // GET /api/items
    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await _db.Items.OrderByDescending(x => x.CreatedAt).ToListAsync());

    // GET /api/items/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await _db.Items.FindAsync(id);
        return item == null ? NotFound() : Ok(item);
    }

    // POST /api/items
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Item item)
    {
        item.Id = 0;
        item.CreatedAt = DateTime.UtcNow;
        _db.Items.Add(item);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
    }

    // PUT /api/items/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Item dto)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();
        item.Name = dto.Name;
        item.Description = dto.Description;
        await _db.SaveChangesAsync();
        return Ok(item);
    }

    // DELETE /api/items/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _db.Items.FindAsync(id);
        if (item == null) return NotFound();
        _db.Items.Remove(item);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

