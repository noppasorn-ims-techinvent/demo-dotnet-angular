using backend.Models.Entities;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services;

public class TodoItemService : BaseService<TodoItem>, ITodoItemService
{
    public TodoItemService(ITodoItemRepository repository) : base(repository)
    {
    }
}
