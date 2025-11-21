# Page snapshot

```yaml
- generic [ref=e2]:
  - region "Notifications alt+T"
  - generic [ref=e3]:
    - banner [ref=e4]:
      - navigation [ref=e5]:
        - link "ChatComplete" [ref=e6] [cursor=pointer]:
          - /url: /
        - generic [ref=e7]:
          - link "Knowledge" [ref=e8] [cursor=pointer]:
            - /url: /knowledge
          - link "Chat" [ref=e9] [cursor=pointer]:
            - /url: /chat
          - link "Analytics" [ref=e10] [cursor=pointer]:
            - /url: /analytics
          - button "Toggle theme" [ref=e11]:
            - img
    - main [ref=e12]:
      - generic [ref=e14]:
        - generic [ref=e15]:
          - button "⚙️ Settings • undefined" [ref=e17]:
            - text: ⚙️ Settings
            - generic [ref=e18]: • undefined
          - generic [ref=e19]: Ollama
        - generic [ref=e22]:
          - textbox "Type your question…" [ref=e23]
          - button "Send" [disabled]
```