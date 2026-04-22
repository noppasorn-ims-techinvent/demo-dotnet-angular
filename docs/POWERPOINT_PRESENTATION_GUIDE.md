# คู่มือทำสไลด์ + เปิดโปรเจกต์ประกอบ (Marketplace Demo)

ใช้คู่มือนี้คู่กับ `docs/PROJECT_CHECKLIST_SUMMARY.md` — แต่ละหัวข้อด้านล่างออกแบบให้ **หนึ่งหัวข้อ ≈ หนึ่งสไลด์ (หรือหนึ่งชุดสไลด์)** แล้ว **เปิดไฟล์ในโปรเจกต์** ตามคอลัมน์ “เปิดโค้ด” พร้อมพูดตาม “คำอธิบายเวที”

**เคล็ดนำเสนอ:** สไลด์ใส่แค่หัวข้อกับบullet สั้น ๆ — รายละเอียดพูดจาก “คำอธิบายเวที” และชี้ที่โค้ด

---

## 0. เปิดตัว (1–2 สไลด์)

| บน PowerPoint (ย่อ) | เปิดโค้ด / โฟลเดอร์ | คำอธิบายเวที (พูดเพิ่ม) |
|---------------------|----------------------|---------------------------|
| ชื่อโปรเจกต์ + เป้าหมาย: ร้านค้าออนไลน์ตัวอย่าง เว็บ + API | โฟลเดอร์ราก `dotnet-angular/` — แยก `frontend/` กับ `backend/` | ฝั่งเว็บ Angular ฝั่งเซิร์ฟเวอร์ .NET 8 รันคู่กันผ่านพร็อกซีตอนพัฒนา |
| สิ่งที่จะโชว์วันนี้: checklist สกิล → ชี้โค้ดจริง | `docs/PROJECT_CHECKLIST_SUMMARY.md` | บอกว่ามีเอกสารสรุปเทียบรายการสกิล — วันนี้เดินตามหัวข้อแล้วเปิดโค้ดประกอบ |

---

## 1. ภาพรวมโปรเจกต์

| บน PowerPoint (ย่อ) | เปิดโค้ด / โฟลเดอร์ | คำอธิบายเวที |
|---------------------|----------------------|---------------|
| เว็บ + เซิร์ฟเวอร์ + ตอนพัฒนา | `frontend/proxy.conf.js` (ถ้าจะพูดเรื่องรวมพอร์ต) | เว็บเรียก API ผ่านพร็อกซี ไม่ต้องจัดการข้ามเว็บเองทุกจุด |

### คัดลอกไปใส่ PowerPoint — โครงโฟลเดอร์ (เวอร์ชันสั้น)

ใช้ **1 สไลด์แบ่งซ้าย–ขวา** หรือ **2 สไลด์** (Frontend กับ Backend)

**Frontend** (`frontend/src/app/`)

- Shell → `app.*` · `app.routes` · `app.config`
- Core → guards · interceptors · services · models
- Features → `features/…` → หลัก ๆ ที่ `pages/` (มี `components/` / `styles/` เฉพาะบางฟีเจอร์)
- Shared → `shared/components/`

**Backend** (`backend/`)

- HTTP → **Controllers** → **Services** → **Repositories**
- ข้อมูล/DB → **Models** · **Data** · **Migrations**
- อื่น ๆ ที่โชว์ได้สั้น ๆ → **Hubs** · **`DTO/Result`** · **`Utilities`** (`ITrace`, `JwtService`) · `Program.cs`

รายละเอียดเต็ม: `docs/PROJECT_CHECKLIST_SUMMARY.md` → หมวด **โครงสร้างโปรเจกต์ (Frontend / Backend)**

---

## 2. เว็บหน้า — โครงแอป (Standalone)

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| สตาร์ทแอปจาก config — ไม่ใช้โมดูลใหญ่แบบเก่า | `frontend/src/main.ts` → `frontend/src/app/app.config.ts` | ชี้ `bootstrapApplication`, `provideRouter`, `provideHttpClient` — แต่ละฟีเจอร์เป็น standalone |

---

## 3. เส้นทางหน้า — กันไม่ให้เข้า — โหลดทีหลัง

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| แยกหน้า โหลดเป็นชิ้น กันสิทธิ์ | `frontend/src/app/app.routes.ts` | ชี้ `loadComponent`, `canActivate` / guard, `resolve` ถ้ามี |

---

## 4. จังหวะชีวิตหน้าจอ + การ์ดสินค้า

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| เวลาข้อมูลจากด้านบนเปลี่ยน — ตัวอย่างในการ์ดสินค้า | `frontend/src/app/features/shop/components/product-card.component.ts` | ชี้ `OnChanges` / `ngOnChanges` (ตัวอย่างสกิล), `OnPush`, `@Input` / `@Output` — บอกว่าในโปรเจกต์บางส่วนเกือบว่างเพื่อ demo |
| แม่ส่งข้อมูลลง ลูกส่งเหตุการณ์ขึ้น | `frontend/src/app/features/shop/pages/product-list.component.html` (หรือ `.ts` ที่ใช้การ์ด) | ชี้ `(addToCart)` กับ `[product]` |

---

## 5. แบบฟอร์ม (Reactive Forms)

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| ฟอร์มยาว หลายแถว — ควบคุมในโค้ด | `frontend/src/app/features/orders/pages/checkout.component.ts` (หรือ `auth/pages/login` / `register` ตามเวลา) | ชี้ `FormBuilder`, `FormArray` ถ้ามีหลายบรรทัด |

---

## 6. เรียก API + ตัวกลางถอดห่อ + โทเคน

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| ลำดับตัวกลาง: ถอดห่อ → แนบโทเคน | `frontend/src/app/app.config.ts` (บรรทัด `withInterceptors`) | อธิบายว่าทุกหน้าได้ payload เดิม ไม่ต้องถอดเอง |
| ถอด **`Result`** ฝั่งเว็บ | `frontend/src/app/core/interceptors/api-envelope.interceptor.ts` | ชี้ว่าอ่าน `success`, `data`; ถ้าไม่สำเร็จโยงไป **`error`** |
| แนบ Bearer | `frontend/src/app/core/interceptors/auth.interceptor.ts` | ชี้การอ่าน token จากที่เก็บ |

---

## 7. เปิดแอป — โหลดค่าก่อน + SignalR

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| รันก่อนหน้าหลัก | `frontend/src/app/app.config.ts` (ส่วน `provideAppInitializer`) | โหลด config, คืนล็อกอิน, เชื่อม hub ถ้ามีสิทธิ์ |
| บริการ SignalR | `frontend/src/app/core/services/marketplace-hub.service.ts` | พูดว่าแจ้งออเดอร์/สต็อกแบบทันที — ชี้การสร้าง/สถานะการเชื่อมถ้ามีในไฟล์ |

---

## 8. ความเร็วหน้าเว็บ (รายการสินค้า)

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| โหมดประหยัด + บอกแถวเดิม + เลื่อนวาด | `frontend/src/app/features/shop/pages/product-list.component.html` + `.ts` | ชี้ `@defer`, `trackBy`, `ChangeDetectionStrategy.OnPush` ในการ์ด |

---

## 9. เซิร์ฟเวอร์ — จุดสตาร์ทและลงทะเบียนบริการ

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| ลงทะเบียน API, JWT, SignalR, health | `backend/Program.cs` | เลื่อนดู `AddControllers`, `AddAuthentication`, `MapHub`, health |

---

## 10. ชั้น Controller — Service — Repository

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| แยกรับคำขอ / กฎ / อ่านเขียน DB | `backend/Controllers/` ไฟล์ใดก็ได้ + `backend/Services/OrderService.cs` + `backend/Repositories/OrderRepository.cs` | สาธิต flow หนึ่งเส้น เช่น ออเดอร์ |
| เส้นทาง URL = ชื่อเมธอด | `backend/Utilities/Constant.cs` + controller | `api/[controller]/[action]` — ฝั่งเว็บเรียกเช่น `.../GetByIdAsync/1` ตรงกับเมธอด |

---

## 11. คำตอบมาตรฐาน (`Result<T>`) + DTO

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| รูปแบบเดียวกันทุก API | `backend/DTO/Result.cs` + controller คืน **`Result<T>`** + `frontend/.../api-envelope.interceptor.ts` | ถอด `data` / จับ `success: false` |
| ข้อมูลออเดอร์ที่ส่งออก | `backend/Models/Dtos/OrderDtos.cs` | ชี้ฟิลด์ที่หน้าเว็บใช้แสดง |

---

## 12. ฐานข้อมูล — โหลดข้อมูลที่เกี่ยวข้อง

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| บอกชัดว่าโหลดตารางไหนพร้อมกัน | `backend/Repositories/OrderRepository.cs` (หรือ repository อื่น) | ชี้ `Include` — ลดการถามซ้ำโดยไม่ตั้งใจ |

---

## 13. ธุรกรรม + คิวในหน่วยความจำ

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| งานที่ต้องสำเร็จพร้อมกันหรือยกเลิกทั้งก้อน | `backend/Services/OrderService.cs` (ค้นหา `BeginTransaction` หรือ `Transaction`) | พูดเรื่องยกเลิกออเดอร์แตะหลายตาราง |
| คิวจำลอง | `backend/Program.cs` + ค้นหา `Channel<` ใน `backend/` | ไม่ต้อง RabbitMQ แต่เห็นภาพผู้ผลิต–ผู้บริโภค |

---

## 14. SignalR Hub

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| แจ้งกลุ่มผู้ใช้ | `backend/Hubs/MarketplaceHub.cs` | เชื่อมกับบริการฝั่งเว็บ |

---

## 15. สุขภาพระบบ + ข้อผิดพลาด

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| เช็กพร้อมรับงาน + body กำหนดเอง | `backend/Program.cs` (health, `JsonHealthCheckResponseWriter`) + `DTO/HealthCheck/` | `/health` ฯลฯ ตอบ JSON รวมสถานะ + ราย dependency; 200 ทุกสถานะเพื่ออ่านรายละเอียดใน body |
| จับ error เป็น JSON | `Program.cs` → **`UseExceptionHandler("/error")`** + `Controllers/ErrorController.cs` (คืน **`Result`**) | SPA ได้รูปแบบเดียวกับ API ปกติ — ไม่เด้งไปหน้า error ของเซิร์ฟเวอร์ |

---

## 16. ปิดท้าย — สิ่งที่ไม่ได้ทำในโปรเจกต์ (ต่อยอด)

| บน PowerPoint (ย่อ) | เปิดโค้ด | คำอธิบายเวที |
|---------------------|-----------|---------------|
| ตาราง “ไม่มีหรือไม่ตรง” | `docs/PROJECT_CHECKLIST_SUMMARY.md` ส่วนตาราง | RabbitMQ, NgModule เก่า, Database First ฯลฯ — ใช้อธิบายว่าต่อยอด checklist ได้จากข้อไหน |

---

## ลำดับเวลาแนะนำ (เลือกใช้)

| ความยาว | แนะนำ |
|---------|--------|
| **15 นาที** | ข้อ 0 → 2 → 3 → 6 → 9 → 10 → 16 |
| **30 นาที** | ครบข้อ 0–16 แต่ละข้อสั้น ๆ |
| **45 นาที** | เหมือน 30 นาที + รันแอปสด (`npm start` + `dotnet run`) โชว์หน้าร้าน + ออเดอร์ + SignalR |

---

## เช็กลิสต์ก่อนขึ้นเวที

- [ ] รัน backend + frontend ได้ (พอร์ตตรง `proxy.conf.js` กับ `launchSettings.json`)
- [ ] มี user ทดสอบ (Buyer / Seller / Admin) พร้อมใน seed
- [ ] เปิดไฟล์สำคัญเป็นแท็บไว้แล้ว — สลับตามลำดับสไลด์
- [ ] สไลด์มีแค่หัวข้อ + path สั้น ๆ (เช่น `app.config.ts`) — รายละเอียดพูดเอง

หากต้องการ **ไฟล์ .pptx อัตโนมัติ** PowerPoint ไม่มีใน repo — คัดลอกคอลัมน์ “บน PowerPoint” ไปวางทีละสไลด์ หรือนำเข้าจากตารางใน Excel แล้วแปลงเป็นสไลด์ได้
