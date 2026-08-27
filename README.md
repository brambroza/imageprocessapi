# AIImageAPI

.NET 8 Web API สำหรับหน้าร้าน — รับภาพลูกค้าจากกล้อง แล้ว
1. รู้จำใบหน้าเทียบกับฐานลูกค้าเดิม
2. ประมาณอายุ / เพศ (สำหรับลูกค้าใหม่)
3. คืนประวัติซื้อครั้งก่อน + สินค้าแนะนำ เพื่อช่วยพนักงานปิดการขาย

> Repo นี้พอร์ตมาจาก ASP.NET Web API 2 (.NET Framework 4.6) เดิมที่ใช้ IronOcr
> ตัวเก่าถูกลบทิ้งใน commit ที่ scaffold โปรเจกต์ใหม่

## Requirements

- .NET SDK 8.0+
- SQL Server 2019+ (หรือใช้ container: `mcr.microsoft.com/mssql/server:2022-latest`)
- ONNX face models — ดาวน์โหลดจาก [InsightFace model zoo](https://github.com/deepinsight/insightface) แล้ววางในโฟลเดอร์ `models/` (ไม่ commit ขึ้น repo):
  - `scrfd_500m.onnx` — face detector
  - `arcface_r100.onnx` — 512-d face embedder
  - `genderage.onnx` — อายุ/เพศ

## Solution Layout

```
AIImageAPI.sln
src/
├── AIImageAPI.Api/            Web API host
├── AIImageAPI.Core/           Entities, DTOs, service interfaces (pure C#)
└── AIImageAPI.Infrastructure/ EF Core + ONNX Runtime implementations
models/                        ONNX files (gitignored)
```

## Configuration

`src/AIImageAPI.Api/appsettings.json`:

| Key | ค่า |
|---|---|
| `ConnectionStrings:Default` | SQL Server connection string |
| `Database:AutoMigrate` | ให้ EF Core apply migration ตอน startup |
| `Face:ModelsDirectory` | path ไปยัง ONNX models |
| `Face:CosineThreshold` | เกณฑ์ยอมรับ match (ค่าเริ่มต้น 0.5) |

## Build & Run

```bash
dotnet restore
dotnet build

# ครั้งแรก: สร้าง migration (ต้องติดตั้ง dotnet-ef ก่อน)
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate \
  -p src/AIImageAPI.Infrastructure \
  -s src/AIImageAPI.Api

dotnet run --project src/AIImageAPI.Api
# Swagger UI: http://localhost:5080/swagger
```

## Endpoints

### `POST /api/Recognition/recognize`

```json
{ "imageBase64": "<png/jpeg base64>" }
```

Response (matched):

```json
{
  "matched": true,
  "confidence": 0.78,
  "customer": { "id": "…", "fullName": "สมชาย ใจดี", "isMember": true, "memberSince": "…" },
  "lastVisit": { "purchasedAt": "…", "totalAmount": 890.00, "items": [...] },
  "recommendations": [
    { "productId": "…", "name": "กาแฟดำ", "reason": "ลูกค้าเคยซื้อครั้งก่อน" }
  ],
  "estimate": { "age": 34, "gender": "male", "genderConfidence": 0.91 },
  "suggestion": "ลูกค้าสมาชิก — แนะนำสินค้าจากประวัติซื้อ"
}
```

Response (ลูกค้าใหม่):

```json
{
  "matched": false,
  "estimate": { "age": 28, "gender": "female", "genderConfidence": 0.85 },
  "suggestion": "ลูกค้าใหม่ — แนะให้พนักงานชวนสมัครสมาชิก"
}
```

### `POST /api/Customers`
สร้างลูกค้าใหม่ พร้อมอัปโหลดภาพใบหน้า 1–N รูป (base64) — ระบบจะคำนวณ embedding เก็บ DB ทันที

### `POST /api/Customers/{id}/faces`
เพิ่มรูปใบหน้าให้ลูกค้าเดิม (เพิ่มความแม่นยำการรู้จำ)

### `POST /api/Purchases`
บันทึกการซื้อ (สำหรับให้ระบบแนะนำสินค้าเรียนรู้)

## สถาปัตยกรรมการ Recognize

```
POST /recognize
   ↓
decode → SCRFD (หน้า) → crop 112×112
   ↓
ArcFace → embedding (512-d)         genderage → age, gender
   ↓
cosine similarity เทียบกับ cache ใน memory
   ↓ ≥ threshold → matched
DB query: ประวัติซื้อล่าสุด + co-occurrence
   ↓
Response: customer + last visit + recommendations
```

Embedding cache โหลดจาก DB ตอน startup และ append ทุกครั้งที่มีการเพิ่ม face ใหม่

## Recommendation Logic (v1)

Score = 0.6 × co-occurrence + 0.4 × recency
- **Co-occurrence:** สินค้าที่ลูกค้าคนอื่นซื้อคู่กับสินค้าที่ลูกค้าคนนี้เคยซื้อ (SQL join บน `PurchaseItem`)
- **Recency:** สินค้าที่ลูกค้าคนนี้ซื้อล่าสุด (re-purchase prompts)

เปลี่ยน implementation ได้โดยเปลี่ยน binding ของ `IRecommendationService`

## TODO ก่อน production

- [ ] Consent flow ตาม PDPA (แจ้งลูกค้า + เก็บ opt-in) ก่อนบันทึก embedding
- [ ] Authentication (API key / OAuth) ที่หน้าร้าน
- [ ] Rate limiting / abuse protection
- [ ] SCRFD post-processing (anchor decode + NMS) ที่ถูกต้อง — โค้ดปัจจุบันเป็น stub
- [ ] Model warmup ตอน startup เพื่อลด cold-start latency
- [ ] Vector index (FAISS / pgvector / HNSW) เมื่อฐานลูกค้าเกิน ~100k คน
