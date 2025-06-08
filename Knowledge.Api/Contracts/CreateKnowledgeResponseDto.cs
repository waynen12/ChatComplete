namespace Knowledge.Api.Contracts
{
    /// <summary>
    /// Represents the response after creating new knowledge content.
    /// </summary>
    public class CreateKnowledgeResponseDto
    {
        /// <summary>
        /// The ID of the newly created knowledge entity.
        /// </summary>
        public string Id { get; set; } = string.Empty;
    }
}
