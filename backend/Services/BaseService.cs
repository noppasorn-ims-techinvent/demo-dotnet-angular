using backend.Models.Entities;
using backend.Repositories.Interfaces;
using backend.Services.Interfaces;

namespace backend.Services;

public class BaseService<T> : IBaseService<T> where T : BaseEntity
{
    private readonly IBaseRepository<T> _repository;

    public BaseService(IBaseRepository<T> repository)
    {
        _repository = repository;
    }

    public virtual Task<IEnumerable<T>> GetAllAsync()
    {
        return _repository.GetAllAsync();
    }

    public virtual Task<T?> GetByIdAsync(int id)
    {
        return _repository.GetByIdAsync(id);
    }

    public virtual Task<T> CreateAsync(T entity)
    {
        return _repository.AddAsync(entity);
    }

    public virtual async Task<bool> UpdateAsync(int id, T entity)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity is null)
        {
            return false;
        }

        entity.Id = id;
        await _repository.UpdateAsync(entity);
        return true;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var existingEntity = await _repository.GetByIdAsync(id);
        if (existingEntity is null)
        {
            return false;
        }

        await _repository.DeleteAsync(existingEntity);
        return true;
    }
}
