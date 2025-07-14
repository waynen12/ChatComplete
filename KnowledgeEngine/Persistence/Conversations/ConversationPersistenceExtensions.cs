using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeEngine.Persistence.Conversations;

public static class ConversationPersistenceExtensions
{
    public static IServiceCollection AddConversationPersistence(this IServiceCollection services)
        => services.AddSingleton<IConversationRepository, MongoConversationRepository>();
}