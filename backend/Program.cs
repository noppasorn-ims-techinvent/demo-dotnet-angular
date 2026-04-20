using backend.Data;
using backend.Middleware;
using backend.Repositories;
using backend.Repositories.Interfaces;
using backend.Services;
using backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using NLog;
using NLog.Web;

var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    builder.Services.AddScoped(typeof(IBaseRepository<>), typeof(BaseRepository<>));
    builder.Services.AddScoped(typeof(IBaseService<>), typeof(BaseService<>));

    builder.Services.AddScoped<ITodoItemRepository, TodoItemRepository>();
    builder.Services.AddScoped<ITodoItemService, TodoItemService>();

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseHttpsRedirection();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Application stopped because of an exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}
