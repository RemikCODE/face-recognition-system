# Face Recognition System – ASP.NET Core Backend

ASP.NET Core 8 Web API that stores face identity records in a SQLite database and exposes endpoints for face recognition.

## Architecture

```
PyQt Desktop App  ──▶  POST /api/faces/recognize ──▶  Python ML service (configurable)
                                                             │
                   ──▶  GET  /api/persons          ──▶  SQLite DB (seeded from CSV)
```

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)

### Run

```bash
cd FaceRecognitionApi
dotnet run
```

The API will be available at `http://localhost:5000`.  
Swagger UI is available at `http://localhost:5000/swagger` (development mode).

### Configuration (`appsettings.json`)

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQLite connection string | `Data Source=face_recognition.db` |
| `MlService:Url` | URL of the Python ML microservice that performs face recognition | *(empty – feature disabled)* |
| `Cors:AllowedOrigins` | Array of allowed CORS origins | *(empty – all origins allowed)* |

In `appsettings.Development.json` the ML service is pre-configured to `http://localhost:5001/recognize`.

### ML Service Contract

The backend delegates recognition to an external Python service.  
The service must accept `POST multipart/form-data` with a field named `image` and return:

```json
{ "label": "Robert Downey Jr_87.jpg", "confidence": 0.92 }
```

## API Endpoints

### Persons

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/api/persons` | List persons (supports `?search=`, `?page=`, `?pageSize=`) |
| `GET` | `/api/persons/{id}` | Get person by ID |
| `POST` | `/api/persons/seed` | Seed DB from CSV file |
| `DELETE` | `/api/persons` | Delete all persons |

**Seed request body:**
```json
{ "csvFilePath": "/absolute/path/to/faces.csv" }
```

**CSV format** (`id,label`):
```
id,label
1,Robert Downey Jr_87.jpg
2,Scarlett Johansson_12.jpg
```

### Face Recognition

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/faces/recognize` | Upload image, get recognized person |

**Request:** `multipart/form-data` with field `image` (JPEG/PNG).  
**Response:**
```json
{
  "found": true,
  "person": { "id": 1, "name": "Robert Downey Jr", "imageFileName": "Robert Downey Jr_87.jpg" },
  "confidence": 0.92,
  "message": "Face recognized successfully."
}
```

## Running Tests

```bash
cd ..
dotnet test FaceRecognitionSystem.slnx
```
