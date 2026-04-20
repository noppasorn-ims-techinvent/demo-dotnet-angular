using backend.Models.Entities;
using backend.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace backend.Controllers;

public class TodoItemsController : BaseApiController
{
    private readonly ITodoItemService _todoItemService;

    public TodoItemsController(ITodoItemService todoItemService)
    {
        _todoItemService = todoItemService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoItem>>> GetAllAsync()
    {
        var todoItems = await _todoItemService.GetAllAsync();
        return Ok(todoItems);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TodoItem>> GetByIdAsync(int id)
    {
        var todoItem = await _todoItemService.GetByIdAsync(id);
        if (todoItem is null)
        {
            return NotFound();
        }

        return Ok(todoItem);
    }

    [HttpPost]
    public async Task<ActionResult<TodoItem>> CreateAsync([FromBody] TodoItem todoItem)
    {
        var createdTodoItem = await _todoItemService.CreateAsync(todoItem);
        return CreatedAtAction(nameof(GetByIdAsync), new { id = createdTodoItem.Id }, createdTodoItem);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateAsync(int id, [FromBody] TodoItem todoItem)
    {
        var isUpdated = await _todoItemService.UpdateAsync(id, todoItem);
        return isUpdated ? NoContent() : NotFound();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id)
    {
        var isDeleted = await _todoItemService.DeleteAsync(id);
        return isDeleted ? NoContent() : NotFound();
    }
}
