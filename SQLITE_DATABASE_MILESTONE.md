# SQLite Database Implementation (Milestone #18)

Complete transition from MongoDB to SQLite for local, zero-dependency deployment.

## 🎯 Goals

- **Zero external dependencies**: Remove MongoDB requirement completely
- **Persistent metadata**: Replace in-memory knowledge repository
- **Chat history preservation**: Local conversation storage
- **Configuration management**: App settings and encryption for sensitive data
- **User management**: Future-proof with user accounts and preferences

## 🏗️ Architecture Overview

### Current State (Problems)
```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   AI Knowledge      │    │      MongoDB        │    │      Qdrant         │
│     Manager         │────│   (Chat History +   │    │   (Vector Data)     │
│                     │    │    Metadata)        │    │                     │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
                                      ↑
                           Requires connection string
                           External dependency
```

### Target State (Solution)
```
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   AI Knowledge      │    │      SQLite         │    │      Qdrant         │
│     Manager         │────│  (Local File DB)    │    │   (Vector Data)     │
│                     │    │  All Metadata       │    │                     │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
                                      ↑
                           Self-contained file
                           Zero configuration
```

## 📊 Database Schema Design

### Phase 1: Core Functionality

#### 1. AppSettings Table
```sql
CREATE TABLE AppSettings (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name VARCHAR(255) NOT NULL UNIQUE,
    Description VARCHAR(500),
    Value TEXT,
    EncryptedValue BLOB,
    IsEncrypted BOOLEAN DEFAULT 0,
    Category VARCHAR(100) DEFAULT 'General',
    DataType VARCHAR(50) DEFAULT 'String', -- String, Integer, Boolean, Json
    IsRequired BOOLEAN DEFAULT 0,
    DefaultValue TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance
CREATE INDEX idx_appsettings_name ON AppSettings(Name);
CREATE INDEX idx_appsettings_category ON AppSettings(Category);
```

**Purpose**: Store all application configuration including API keys, model settings, feature flags
**Examples**:
- `OpenAI.ApiKey` (encrypted)
- `Chat.DefaultTemperature` (0.7)
- `Vector.DefaultModel` (text-embedding-ada-002)
- `UI.Theme` (dark/light)

#### 2. ChatHistory Table  
```sql
CREATE TABLE Conversations (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ConversationId VARCHAR(36) NOT NULL UNIQUE, -- GUID format
    ClientId VARCHAR(255),
    Title VARCHAR(500),
    KnowledgeId VARCHAR(255),
    Provider VARCHAR(50), -- OpenAI, Anthropic, Gemini, Ollama
    ModelName VARCHAR(100),
    Temperature REAL,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    IsArchived BOOLEAN DEFAULT 0
);

CREATE TABLE Messages (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ConversationId VARCHAR(36) NOT NULL,
    Role VARCHAR(20) NOT NULL, -- user, assistant, system
    Content TEXT NOT NULL,
    TokenCount INTEGER,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    MessageIndex INTEGER NOT NULL, -- Order within conversation
    FOREIGN KEY (ConversationId) REFERENCES Conversations(ConversationId) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX idx_conversations_id ON Conversations(ConversationId);
CREATE INDEX idx_conversations_client ON Conversations(ClientId);
CREATE INDEX idx_messages_conversation ON Messages(ConversationId);
CREATE INDEX idx_messages_timestamp ON Messages(Timestamp);
```

**Purpose**: Replace MongoDB chat history with local SQLite storage
**Features**:
- Conversation threading
- Message ordering
- Token usage tracking
- Multi-model support

#### 3. Knowledge Metadata Tables
```sql
CREATE TABLE KnowledgeCollections (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CollectionId VARCHAR(255) NOT NULL UNIQUE,
    Name VARCHAR(500) NOT NULL,
    Description TEXT,
    DocumentCount INTEGER DEFAULT 0,
    ChunkCount INTEGER DEFAULT 0,
    TotalTokens INTEGER DEFAULT 0,
    EmbeddingModel VARCHAR(100),
    VectorStore VARCHAR(50) DEFAULT 'Qdrant', -- Qdrant, MongoDB, InMemory
    Status VARCHAR(50) DEFAULT 'Active', -- Active, Processing, Error, Deleted
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE KnowledgeDocuments (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CollectionId VARCHAR(255) NOT NULL,
    DocumentId VARCHAR(36) NOT NULL,
    OriginalFileName VARCHAR(500),
    FileSize INTEGER,
    FileType VARCHAR(50), -- pdf, docx, txt, md
    ChunkCount INTEGER DEFAULT 0,
    ProcessingStatus VARCHAR(50) DEFAULT 'Pending', -- Pending, Processing, Complete, Error
    ErrorMessage TEXT,
    UploadedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    ProcessedAt DATETIME,
    FOREIGN KEY (CollectionId) REFERENCES KnowledgeCollections(CollectionId) ON DELETE CASCADE
);

CREATE TABLE KnowledgeChunks (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    CollectionId VARCHAR(255) NOT NULL,
    DocumentId VARCHAR(36) NOT NULL,
    ChunkId VARCHAR(36) NOT NULL UNIQUE,
    ChunkText TEXT NOT NULL,
    ChunkOrder INTEGER NOT NULL,
    TokenCount INTEGER,
    CharacterCount INTEGER,
    VectorStored BOOLEAN DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (CollectionId) REFERENCES KnowledgeCollections(CollectionId) ON DELETE CASCADE,
    FOREIGN KEY (DocumentId) REFERENCES KnowledgeDocuments(DocumentId) ON DELETE CASCADE
);

-- Indexes for performance  
CREATE INDEX idx_collections_id ON KnowledgeCollections(CollectionId);
CREATE INDEX idx_documents_collection ON KnowledgeDocuments(CollectionId);
CREATE INDEX idx_chunks_collection ON KnowledgeChunks(CollectionId);
CREATE INDEX idx_chunks_document ON KnowledgeChunks(DocumentId);
```

**Purpose**: Replace in-memory knowledge repository with persistent metadata storage
**Features**:
- Document tracking
- Chunk management
- Processing status
- Token usage analytics

### Phase 2: User Management (Future)

#### 4. Users Table
```sql
CREATE TABLE Users (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId VARCHAR(36) NOT NULL UNIQUE,
    Username VARCHAR(100) UNIQUE,
    Email VARCHAR(255) UNIQUE,
    PasswordHash VARCHAR(255), -- Argon2 or bcrypt
    Role VARCHAR(50) DEFAULT 'User', -- Admin, User, ReadOnly
    IsActive BOOLEAN DEFAULT 1,
    LastLoginAt DATETIME,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for authentication
CREATE INDEX idx_users_username ON Users(Username);
CREATE INDEX idx_users_email ON Users(Email);
```

#### 5. User Preferences Table
```sql
CREATE TABLE UserPreferences (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    UserId VARCHAR(36) NOT NULL,
    PreferenceKey VARCHAR(255) NOT NULL,
    PreferenceValue TEXT,
    DataType VARCHAR(50) DEFAULT 'String',
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    UNIQUE(UserId, PreferenceKey)
);

CREATE INDEX idx_userprefs_user ON UserPreferences(UserId);
CREATE INDEX idx_userprefs_key ON UserPreferences(PreferenceKey);
```

**Purpose**: Per-user customization and settings
**Examples**:
- `UI.Theme` (dark/light per user)
- `Chat.PreferredModel` (gpt-4o, claude-sonnet)
- `Chat.DefaultTemperature` (user override)
- `Knowledge.DefaultCollection`

## 🔧 Implementation Plan

### Phase 1 Tasks ✅ **COMPLETED** (2025-01-10)

1. **Database Infrastructure** ✅
   - [x] Create SQLite database service/provider → `SqliteDbContext.cs`
   - [x] Implement connection management and migrations → Auto-schema initialization  
   - [x] Add encryption service for sensitive data → `EncryptionService.cs` (AES-256)
   - [x] Create base repository pattern → Repository interfaces implemented

2. **Replace In-Memory Knowledge Repository** ✅
   - [x] Implement `SqliteKnowledgeRepository` → Full CRUD operations
   - [x] Add CRUD operations for collections/documents → Complete implementation
   - [x] Update dependency injection registration → `ServiceCollectionExtensions.cs`
   - [x] Migrate collection tracking to SQLite → Persistent metadata storage

3. **Replace MongoDB Chat Service** ✅
   - [x] Implement `SqliteChatService` → Identical interface to `MongoChatService`
   - [x] Create conversation and message management → `SqliteConversationRepository.cs`
   - [x] Update chat endpoints and DTOs → Automatic provider switching
   - [x] Test conversation persistence → Fully functional with chat history

4. **Configuration Management** ✅
   - [x] Implement `SqliteAppSettingsService` → Dynamic configuration loading
   - [x] Load/save encrypted API keys → PBKDF2 + AES-256 encryption
   - [x] Dynamic configuration updates → Runtime settings modification
   - [x] Settings validation and defaults → Comprehensive default initialization

5. **Configuration Integration** ✅ **BONUS**
   - [x] Configurable database path → `"DatabasePath"` in appsettings.json
   - [x] Smart path defaults → Container vs development environment detection  
   - [x] Directory auto-creation → Automatic parent directory creation
   - [x] Startup initialization → Database ready on application start

## 🎉 Phase 1 Implementation Summary

**Zero-Dependency Architecture Achieved** ✅

### Core Components Delivered
```
KnowledgeEngine/Persistence/Sqlite/
├── SqliteDbContext.cs              # Database connection & schema initialization
├── Services/
│   ├── EncryptionService.cs        # AES-256 encryption for sensitive data
│   └── SqliteAppSettingsService.cs # Dynamic configuration management
└── Repositories/
    ├── SqliteAppSettingsRepository.cs    # Encrypted settings storage
    ├── SqliteKnowledgeRepository.cs      # Knowledge metadata persistence
    └── SqliteConversationRepository.cs   # Chat history storage
```

### Service Registration & Configuration
- **Automatic Provider Switching**: Qdrant → SQLite services, MongoDB → Legacy services
- **Configurable Database Path**: `"DatabasePath": "/custom/path"` in appsettings.json
- **Smart Defaults**: Container (`/app/data/`) vs Development (`{AppDir}/data/`) detection
- **Startup Initialization**: Database creation, schema setup, default settings population

### Database Schema (Auto-created)
- **AppSettings**: Encrypted configuration with categories and descriptions
- **Conversations & Messages**: Complete chat history with indexing for performance
- **KnowledgeCollections**: Metadata tracking for document collections
- **KnowledgeDocuments**: Individual document processing status and statistics  
- **KnowledgeChunks**: Chunk-level tracking with token counts and vector status

### Deployment Impact
**Before**: Requires MongoDB Atlas connection + Qdrant + Application
**After**: Requires only Qdrant + Application (SQLite embedded)

**Docker Command**: `docker-compose -f docker-compose.dockerhub.yml up -d` 
**Result**: Fully functional RAG system with zero external dependencies

### Phase 2 Tasks

6. **User Management**
   - [ ] Implement authentication service
   - [ ] User registration and login
   - [ ] Role-based access control
   - [ ] Session management

7. **User Preferences**
   - [ ] Per-user settings override
   - [ ] Preference inheritance (global → user)
   - [ ] Settings UI components
   - [ ] Import/export user data

## 🗂️ File Structure

```
KnowledgeEngine/
├── Persistence/
│   ├── Sqlite/
│   │   ├── SqliteDbContext.cs
│   │   ├── Migrations/
│   │   │   ├── 001_InitialSchema.sql
│   │   │   ├── 002_UserManagement.sql
│   │   │   └── Migration.cs
│   │   ├── Repositories/
│   │   │   ├── SqliteKnowledgeRepository.cs
│   │   │   ├── SqliteChatRepository.cs
│   │   │   ├── SqliteAppSettingsRepository.cs
│   │   │   └── SqliteUserRepository.cs
│   │   └── Services/
│   │       ├── EncryptionService.cs
│   │       └── SqliteDatabaseService.cs
│   └── Models/
│       ├── SqliteModels.cs
│       └── MigrationModels.cs
└── Extensions/
    └── SqliteServiceExtensions.cs
```

## 🔒 Security Considerations

### Encryption Strategy
- **API Keys**: AES-256 encryption with app-specific key
- **User Passwords**: Argon2 hashing (if Phase 2)
- **Database File**: Consider file-level encryption for sensitive deployments

### Key Management
- **Development**: Hardcoded key (acceptable for self-hosted)
- **Production**: Environment-derived key or hardware security module
- **Docker**: Mount secrets or use Docker secrets management

## 🐳 Docker Integration

### Volume Configuration
```yaml
volumes:
  - ai-knowledge-data:/app/data          # Contains SQLite database
  - ai-knowledge-config:/app/config      # Configuration overrides
```

### Database Location
- **Path**: `/app/data/knowledge.db`
- **Backup**: Regular SQLite backup to same volume
- **Migration**: Automatic schema updates on startup

## 📊 Performance Considerations

### Indexing Strategy
- Primary keys for all tables
- Foreign key indexes for joins
- Composite indexes for common queries
- Partial indexes for filtered queries

### Connection Management
- **SQLite**: Single-writer, multiple-reader model
- **Pooling**: Connection pooling for read operations
- **Transactions**: Batch operations for performance
- **WAL Mode**: Write-Ahead Logging for better concurrency

## 🧪 Testing Strategy

### Unit Tests
- Repository CRUD operations
- Service layer logic
- Encryption/decryption
- Migration scripts

### Integration Tests
- End-to-end conversation flows
- Knowledge upload and search
- Configuration management
- Multi-user scenarios (Phase 2)

### Migration Tests
- MongoDB → SQLite data migration
- Schema version upgrades
- Data integrity validation

## 📈 Benefits

### Phase 1 Completion
- ✅ **Zero MongoDB dependency**
- ✅ **Persistent metadata storage**
- ✅ **Local chat history**
- ✅ **Encrypted configuration**
- ✅ **Single-command deployment**

### Phase 2 Completion  
- ✅ **Multi-user support**
- ✅ **Per-user customization**
- ✅ **Role-based access**
- ✅ **Enterprise-ready deployment**

## 🚀 Migration Path

1. **Development**: Implement SQLite services alongside MongoDB
2. **Testing**: Dual-mode operation for validation
3. **Migration**: One-time data transfer from MongoDB
4. **Production**: Switch to SQLite-only mode
5. **Cleanup**: Remove MongoDB dependencies

This milestone transforms AI Knowledge Manager from a MongoDB-dependent application into a truly self-contained, zero-configuration RAG system perfect for Docker deployment.

---

## ✅ IMPLEMENTATION STATUS: COMPLETED (2025-08-17)

**Phase 1 SQLite Implementation Successfully Completed:**

### Core Database Features ✅
- **Schema Management**: Auto-creating database with full initialization
- **Chat History**: Complete conversation persistence replacing MongoDB  
- **Knowledge Metadata**: Document tracking, chunk counts, processing status
- **Configuration Storage**: AES-256 encrypted API keys and settings
- **Ollama Integration**: Model management with download progress tracking

### Database Migration Features ✅  
- **Automatic Migration**: Foreign key constraint removal for existing databases
- **Smart Defaults**: Container and development path handling
- **WAL Mode**: Optimized for concurrent access
- **Schema Versioning**: Future-proof upgrade path

### Key Fixes Applied ✅
1. **Foreign Key Issues**: Automatic detection and removal of problematic constraints
2. **Ollama API Compatibility**: Updated JSON models for newer Ollama versions
3. **HTTP Method Corrections**: Fixed DELETE endpoint usage
4. **Real-time Progress**: Server-Sent Events for download tracking

### Test Results ✅
- Integration tests passing with download-verify-delete workflow
- Database operations validated with transaction support
- Migration logic tested with existing constraint scenarios
- Performance validated with concurrent access patterns

**Status**: Production-ready zero-dependency deployment achieved.