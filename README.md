Here's a **detailed Mermaid diagram** to visually represent your Azure-based video portal architecture, including the SPA frontend, Azure PaaS services, SQL schema interaction, Blob Storage, and Service Bus processing.

---

### 🎯 **Mermaid Architecture Diagram**

```mermaid
graph TD

subgraph User Interface
    A1[User - Admin/Viewer]
    A2[Knockout.js SPA UI]
    A3[Kendo UI Components]
end

subgraph Azure Web App (MVC + DI)
    B1[Azure App Service]
    B2[Controllers]
    B3[Services Layer (DI)]
    B4[Entity Framework ORM]
end

subgraph Database
    D1[Azure SQL Database]
    D2[Users Table]
    D3[Videos Table]
    D4[Tags Table]
    D5[VideoTags Table]
    D6[Categories Table]
    D7[VideoCategories Table]
    D8[Downloads Table]
end

subgraph Azure Blob Storage
    C1[Video Container]
    C2[Blob Files (MP4)]
    C3[SAS Token Generator]
end

subgraph Messaging & Background Processing
    E1[Azure Service Bus Topic: video-events]
    E2[Subscription: metadata-processor]
    E3[Subscription: encoding-service]
    E4[Subscription: notification-service]
    F1[Azure Function - Metadata Update]
    F2[Azure Function - Video Transcoder]
    F3[Azure Function - Notifier]
end

%% User Interaction
A1 --> A2
A2 --> A3
A3 --> B1

%% App Service Components
B1 --> B2
B2 --> B3
B3 --> B4

%% SQL Interactions
B4 --> D1
D1 --> D2
D1 --> D3
D1 --> D4
D1 --> D5
D1 --> D6
D1 --> D7
D1 --> D8

%% Blob Storage Integration
B3 --> C3
C3 --> C2
C2 --> C1

%% Uploading a Video
A2 -->|Uploads MP4| C1
C1 -->|Triggers Message| E1

%% Service Bus Flow
E1 --> E2 --> F1 --> D3
E1 --> E3 --> F2 --> C2
E1 --> E4 --> F3 --> A1

%% Download Process
A2 -->|Request Download| B3 --> C3 --> C2

%% Playback Process
A2 -->|View Video| B3 --> C3 --> C2

%% Tag & Filter Process
A2 -->|Filter/Search| B3 --> D1

```

---

### 🔎 **Diagram Highlights**

- **User** interacts with a **Knockout.js SPA**, which is hosted on **Azure App Service** using MVC architecture.
- **App Service** uses **Entity Framework** and **Dependency Injection** to call into the **Azure SQL Database**.
- Video files are stored in **Azure Blob Storage**, and SAS tokens are generated for secure access.
- When a video is uploaded, a message is sent to **Azure Service Bus**, where three subscriptions trigger Azure Functions to:
  - Update video metadata (e.g., status = "Processed")
  - Transcode the video
  - Notify users/admins
- Download and streaming use **SAS tokens** from Blob Storage for secure access.
- Rich filtering and tag support comes from joins between `Videos`, `Tags`, and `Categories` tables.

---

Let me know if you’d like this exported as an image, or if you want the YAML or Bicep provisioning to go along with this architecture!


Thanks for the detailed background! Based on your scenario—cloud-based video solution using Azure PaaS, MVC/Entity Framework, SPA with Knockout.js, Kendo UI, and secure video blob storage—here's a full breakdown of the **SQL Database Tables**, **Azure Services** (including Service Bus and Storage), and a **scenario walkthrough** that illustrates how all these components work together.

---

## ✅ **Solution Scenario**

**PBS Video Portal MVP** (3-month release cycle):

**Use Case**:  
End-users can:
- Upload videos (via portal interface)
- Assign tags, categories, and metadata
- Search and filter videos by title/tags/date
- View and download videos securely
- Admin users can manage video content

**Architecture:**
- **Azure App Service (PaaS)** for hosting MVC App (UI + API)
- **Azure SQL Database** for metadata
- **Azure Storage (Blob)** for storing video files
- **Azure Service Bus** for handling video upload events asynchronously (e.g., post-processing, logging)
- **Knockout.js SPA** with Kendo UI and jQuery for UI

---

## 📊 **SQL Database Tables**

Here are the core tables to support your scenario:

### 1. `Users`
```sql
CREATE TABLE Users (
    UserId INT PRIMARY KEY IDENTITY,
    Username NVARCHAR(100) NOT NULL,
    Email NVARCHAR(200),
    Role NVARCHAR(50), -- Admin, Viewer
    CreatedAt DATETIME DEFAULT GETDATE()
);
```

### 2. `Videos`
```sql
CREATE TABLE Videos (
    VideoId INT PRIMARY KEY IDENTITY,
    Title NVARCHAR(255),
    Description NVARCHAR(MAX),
    FileName NVARCHAR(255),
    BlobUrl NVARCHAR(500),
    UploadedBy INT FOREIGN KEY REFERENCES Users(UserId),
    UploadedAt DATETIME DEFAULT GETDATE(),
    FileSizeMB FLOAT,
    DurationSec INT,
    Status NVARCHAR(50), -- Pending, Processed, Failed
    IsPublic BIT
);
```

### 3. `Tags`
```sql
CREATE TABLE Tags (
    TagId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) UNIQUE
);
```

### 4. `VideoTags`
```sql
CREATE TABLE VideoTags (
    VideoId INT FOREIGN KEY REFERENCES Videos(VideoId),
    TagId INT FOREIGN KEY REFERENCES Tags(TagId),
    PRIMARY KEY(VideoId, TagId)
);
```

### 5. `Categories`
```sql
CREATE TABLE Categories (
    CategoryId INT PRIMARY KEY IDENTITY,
    Name NVARCHAR(100) UNIQUE
);
```

### 6. `VideoCategories`
```sql
CREATE TABLE VideoCategories (
    VideoId INT FOREIGN KEY REFERENCES Videos(VideoId),
    CategoryId INT FOREIGN KEY REFERENCES Categories(CategoryId),
    PRIMARY KEY(VideoId, CategoryId)
);
```

### 7. `Downloads`
```sql
CREATE TABLE Downloads (
    DownloadId INT PRIMARY KEY IDENTITY,
    VideoId INT FOREIGN KEY REFERENCES Videos(VideoId),
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    DownloadedAt DATETIME DEFAULT GETDATE(),
    IPAddress NVARCHAR(50)
);
```

---

## ☁️ **Azure Services Used**

### 🔷 **Azure Storage (Blob)**
- Stores video files.
- Blob URL is stored in the `Videos` table.
- Secure access via **SAS tokens** (generated per request).

### 🔷 **Azure Service Bus**
- **Topic**: `video-events`
  - Handles messages for post-upload processing (e.g., compression, encoding).
- Subscriptions:
  - `metadata-processor` (updates SQL status)
  - `encoding-service` (triggers Azure Functions to transcode video)
  - `notification-service` (sends email/push notifications)

**Sample Message Payload** (Service Bus):
```json
{
  "videoId": 123,
  "eventType": "Uploaded",
  "uploadedBy": 45,
  "blobUrl": "https://pbsstorage.blob.core.windows.net/videos/xyz.mp4"
}
```

### 🔷 **Azure App Service (Web App)**
- Hosts the MVC + Knockout.js SPA
- Implements Dependency Injection (DI) pattern for services and repositories

---

## 📷 **Sample User Scenario**

> **User Journey - Upload & Watch a Video**

1. **Login (Auth via Azure AD B2C or local auth DB)**
2. User uploads a video:
   - File stored in **Azure Blob Storage**
   - Metadata written to `Videos` table
   - Upload triggers a message to **Azure Service Bus**
3. Background services (Azure Functions) process:
   - Transcode video
   - Update `Videos.Status` to "Processed"
4. Users access the SPA UI:
   - Knockout.js + Kendo UI pull metadata from SQL
   - Video list shows thumbnails, tags, filters
5. User clicks "Watch":
   - App Service generates **SAS token** to Blob
   - Video streamed securely from Azure Blob Storage
6. User clicks "Download":
   - Secure SAS download link
   - `Downloads` table updated with user info and timestamp

---

## 🚀 Features Summary

| Feature                | Azure Service              | Details                                                                 |
|------------------------|----------------------------|-------------------------------------------------------------------------|
| File Storage           | Blob Storage               | Secure with SAS, scalable, cost-effective                              |
| Metadata Storage       | Azure SQL                  | Stores all metadata for videos, users, tags                            |
| Async Processing       | Service Bus + Azure Func   | Transcoding, logging, notification queues                              |
| Rich UI                | Knockout.js, Kendo UI      | SPA with dynamic search/filter                                         |
| Secure Access          | Blob SAS Tokens            | Temporary access, URL expiration, IP restrictions                      |
| Fast Search            | SQL + Indexing             | Fast queries by title/tag/category                                     |

---

Would you like a full **Mermaid architecture diagram** of this flow as well?
