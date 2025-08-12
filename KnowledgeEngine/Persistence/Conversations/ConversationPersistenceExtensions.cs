using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeEngine.Persistence.Conversations;

public static class ConversationPersistenceExtensions
{
    /// <summary>
    /// Registers MongoDB conversation persistence (legacy)
    /// </summary>
    public static IServiceCollection AddConversationPersistence(this IServiceCollection services)
        => services.AddSingleton<IConversationRepository, MongoConversationRepository>();

    /// <summary>
    /// Registers SQLite conversation persistence (zero-dependency)
    /// Note: SQLite services must be registered first via AddSqlitePersistence
    /// </summary>
    public static IServiceCollection AddSqliteConversationPersistence(this IServiceCollection services)
        => services; // SQLite repository is registered in AddSqlitePersistence
}