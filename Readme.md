### Supported model providers
| Provider     | How to request in JSON | Notes |
|--------------|-----------------------|-------|
| OpenAI       | `"provider": "OpenAi"` | GPT-4o |
| Google Gemini| `"provider": "Google"` | gemini-2.5-flash |
| Anthropic    | `"provider": "Anthropic"` | claude-sonnet-4-20250514 |
| Ollama       | `"provider": "Ollama"` | Any local model (e.g. gemma3:12b) |

**Example request**

```bash
curl -X POST http://localhost:7040/api/chat \
     -H "Content-Type: application/json" \
     -d '{
           "message": "Hello, world!",
           "provider": "Ollama",
           "knowledgeId": null,
           "conversationId": null
         }'
