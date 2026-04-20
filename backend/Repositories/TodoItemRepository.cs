using backend.Data;
using backend.Models.Entities;
using backend.Repositories.Interfaces;

namespace backend.Repositories;

public class TodoItemRepository : BaseRepository<TodoItem>, ITodoItemRepository
{
    public TodoItemRepository(AppDbContext context) : base(context)
    {
    }
}
