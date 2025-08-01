using ItemManagement.Model;

namespace ItemManagement.Services
{
    public class DatabaseLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly Func<ApplicationDbContext> _dbContext;

        public DatabaseLogger(string categoryName, Func<ApplicationDbContext> dbContext)
        {
            _categoryName = categoryName ?? throw new ArgumentNullException(nameof(categoryName));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            // Log only errors and above (customize if needed)
            return logLevel >= LogLevel.Error;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception == null) return;

            // Create a new DbContext instance for each log entry
            using (var context = _dbContext())
            {
                var logEntry = new Logger
                {
                    Timestamp = DateTime.UtcNow,
                    Message = message,
                    ExceptionType = exception?.ToString()
                };

                context.Loggers.Add(logEntry);
                context.SaveChanges();
            }
        }
    }
   
}
