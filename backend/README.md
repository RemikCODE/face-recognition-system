# Face Recognition System – ASP.NET Core Backend

ASP.NET Core 8 Web API that stores face identity records in a SQLite database and exposes endpoints for face recognition.

## Architecture

```
PyQt Desktop App  ──▶  POST /api/faces/recognize ──▶  Python ML service (configurable)
                                                             │
                   ──▶  GET  /api/persons          ──▶  SQLite DB (seeded from CSV or dataset scan)
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

### Where is the database created?

The SQLite database file is created **automatically on first startup** in the application's working directory.  
Default filename: **`face_recognition.db`** (in the folder where `dotnet run` is executed).

You can change the location by setting `ConnectionStrings:DefaultConnection` in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=/absolute/path/to/face_recognition.db"
}
```

On startup the application also tries to auto-seed the `Persons` table from a bundled CSV:
- Path: `Data/faces.csv` (relative to the application's output directory, e.g. `bin/Debug/net8.0/Data/faces.csv`)
- If the file is absent the table stays empty, but recognized persons are **auto-inserted** the first time they are matched by the ML service.

### Configuration (`appsettings.json`)

| Key | Description | Default |
|-----|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQLite connection string | `Data Source=face_recognition.db` |
| `MlService:Url` | URL of the Python ML microservice that performs face recognition | *(empty – feature disabled)* |
| `DatasetPath` | Path to the ML dataset folder – required for `POST /api/persons` and auto-seeding | *(empty)* |
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
| `POST` | `/api/persons` | **Add a single person** – saves photo to the dataset and adds DB record |
| `POST` | `/api/persons/seed` | Seed DB from a CSV file **path on the server** |
| `POST` | `/api/persons/seed-upload` | Seed DB by **uploading** a CSV file (multipart/form-data) |
| `POST` | `/api/persons/scan-dataset` | Seed DB by **scanning a dataset folder** for image files |
| `DELETE` | `/api/persons` | Delete all persons |

**Add person** (`POST /api/persons`):  
`multipart/form-data` with fields:
- `name` – full name of the person (e.g. `John Smith`)
- `image` – photo file (JPEG/PNG/BMP)

The photo is saved to the configured `DatasetPath` folder as `Name_timestamp.ext`.  
The DeepFace embedding cache (`.pkl`) is deleted so the ML service rebuilds it on the next recognition request.  
Set `DatasetPath` in `appsettings.json` to the same folder used by the ML service.

**Response** (`201 Created`):
```json
{ "id": 42, "name": "John Smith", "imageFileName": "John Smith_1741296000.jpg" }
```

**Seed by server path** (`POST /api/persons/seed`) request body:
```json
{ "csvFilePath": "/absolute/path/to/faces.csv" }
```

**Seed by CSV upload** (`POST /api/persons/seed-upload`):  
`multipart/form-data` with field `csv` containing the CSV file.

**Seed by dataset scan** (`POST /api/persons/scan-dataset`) request body:
```json
{ "datasetPath": "/absolute/path/to/ml/dataset" }
```
Scans the directory for `*.jpg / *.jpeg / *.png / *.bmp` files named in the format  
`Person Name_N.ext` (e.g. `Akshay Kumar_87.jpg`) and populates the Persons table.  
This is the quickest way to sync the database with the ML dataset folder.

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

> **Note:** If the ML service recognizes a person whose name is not yet in the Persons table
> (e.g. the table was never seeded), the person is **automatically inserted** into the database
> and the response returns `"found": true`. You can also pre-populate the table using
> `POST /api/persons/scan-dataset` pointing at the ML `dataset/` folder.

## Running Tests

```bash
cd ..
dotnet test FaceRecognitionSystem.slnx
```
