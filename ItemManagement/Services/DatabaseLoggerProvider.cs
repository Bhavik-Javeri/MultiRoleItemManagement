using ItemManagement.Model;

namespace ItemManagement.Services
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly Func<ApplicationDbContext> _dbContext;

        public DatabaseLoggerProvider(Func<ApplicationDbContext> dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new DatabaseLogger(categoryName, _dbContext);
        }

        public void Dispose()
        {
            // Dispose of any resources if needed
        }
    }
}
