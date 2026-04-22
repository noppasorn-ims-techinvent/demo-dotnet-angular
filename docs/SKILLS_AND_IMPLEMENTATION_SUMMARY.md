# สรุปสกิล + ว่าโปรเจกต์ทำอะไรกับอะไร

เอกสารนี้คู่กับ `PROJECT_CHECKLIST_SUMMARY.md` — แต่ละหัวข้ออธิบาย **ความหมายสั้น ๆ** แล้วตามด้วย **ว่าใน Marketplace Demo ใช้ยังไง** (อ้างโฟลเดอร์/ไฟล์หลัก)

---

## Angular

### Lifecycle Hook

**คืออะไร:** จุดใน “ชีวิต” ของ component ที่ framework เรียกให้เราโค้ดทำงาน เช่น หลังสร้าง component, หลังวาด DOM, เมื่อ `@Input` จากแม่เปลี่ยน

**โปรเจกต์ทำอะไร:** ใช้โหลดข้อมูลจาก route, ตั้งค่าฟอร์มจากตะกร้า, โฟกัสช่องกรอกหลังวาดหน้า

| ตัวอย่าง | ไฟล์ / บริบท |
|----------|----------------|
| `OnInit` — อ่านข้อมูลหลัง component พร้อม | `product-detail`, `order-pay`, `checkout`, `seller-product-form` |
| `AfterViewInit` — หลัง template วาดแล้ว (เหมาะโฟกัส input) | `checkout` (`qtyInputs`), `seller-product-form` (`nameInput`) |
| `OnChanges` — เมื่อ `@Input` เปลี่ยน (แม่ bind คนละ instance) | `features/shop/components/product-card.component.ts` |
| `effect()` — reactive กับ signal ภายนอก (เช่น hub tick) | `product-list` โหลดแคตตาล็อกเมื่อข้อมูล refresh |

---

### Component Communication

**คืออะไร:** วิธีส่งข้อมูล/เหตุการณ์ระหว่าง component หรือระหว่าง component กับบริการกลาง

**โปรเจกต์ทำอะไร:**

- **`@Input` / `@Output`:** การ์ดสินค้ารับ `product` ส่งเหตุการณ์ `addToCart` ขึ้นแม่ (`product-list` ↔ `product-card`)
- **`ViewChild` / `ViewChildren`:** โฟกัสช่องจำนวนในชำระเงิน, ชื่อสินค้าในฟอร์มร้าน
- **Service ร่วม (`providedIn: 'root'`):** ล็อกอิน, ตะกร้า, ออเดอร์, สินค้า, hub — หลายหน้า `inject()` ตัวเดียวกัน

---

### Reactive Forms

**คืออะไร:** สร้างและตรวจแบบฟอร์มใน TypeScript (`FormBuilder`, `FormGroup` / `FormArray`) แทนการผูกสถานะล้วนใน template

**โปรเจกต์ทำอะไร:** ล็อกอิน, สมัคร, ชำระเงิน (หลายบรรทัด + ขั้นจ่ายเงินจำลอง), ฟอร์มสินค้าร้าน — มี validators และ `markAllAsTouched` ตอนส่ง

**อ้างอิง:** `features/auth/pages/`, `features/orders/pages/checkout.component.ts`, `features/seller/pages/seller-product-form.component.ts`

---

### Routing / Guard / Resolver / Lazy Loading

**คืออะไร:**

- **Routing:** แมป URL → หน้า
- **Guard:** ตัดสินก่อนเข้า route (ล็อกอิน, บทบาท)
- **Resolver:** โหลดข้อมูลก่อนเปิด route แล้วส่งเข้า `route.data`
- **Lazy loading:** โหลดโค้ดของหน้านั้นเมื่อเข้า path จริง ไม่รวมใน bundle แรกทั้งหมด

**โปรเจกต์ทำอะไร:** แอปเป็น **standalone** — ใช้ `loadComponent` + `import()` ใน `app.routes.ts`; `authGuard`, `rolesGuard(...)`; `productResolver` สำหรับ `/shop/product/:id`

**อ้างอิง:** `frontend/src/app/app.routes.ts`, `core/guards/`, `core/resolvers/product.resolver.ts`

---

### Authorization / Session Handling

**คืออะไร:** ฝั่งเว็บรู้ว่า “ล็อกอินแล้วหรือยัง” และ “มีบทบาทอะไร”; เก็บสถานะเซสชัน (ที่นี่เป็น JWT ใน `localStorage`)

**โปรเจกต์ทำอะไร:** `AuthService` เก็บ token/user ด้วย signal, hydrate จาก storage ตอน initializer; route ใช้ guard ตามบทบาท Buyer/Seller/Admin; interceptor แนบ Bearer; 401 แล้วออกจากระบบ (ยกเว้นหน้า login/register)

**อ้างอิง:** `core/services/auth.service.ts`, `core/guards/`, `core/interceptors/auth.interceptor.ts`

---

### HttpClient / Interceptor

**คืออะไร:** `HttpClient` เรียก HTTP API; **interceptor** แทรกทุก request/response (แนบ header, แปลง body)

**โปรเจกต์ทำอะไร:** ลงทะเบียนใน `app.config.ts` — ลำดับ roughly: ถอด **`Result<T>.data`** → แนบโทเคน; ข้าม path hub/health ตามที่กำหนด; เมื่อ **`success: false`** แปลงเป็น **`HttpErrorResponse`** ให้ `subscribe` เข้า **`error`**

**อ้างอิง:** `app.config.ts`, `core/interceptors/api-envelope.interceptor.ts`, `core/interceptors/auth.interceptor.ts`, `core/services/*.service.ts`

---

### SignalR (ไม่ใช่การเขียน WebSocket ดิบทั้งเส้น)

**คืออะไร:** SignalR เป็นไลบรารี real-time ของ Microsoft — ขนส่งอาจใช้ WebSockets (หรือ fallback) แต่โค้ดแอปไม่ต้องจัดการ frame เอง

**โปรเจกต์ทำอะไร:** ฝั่งเว็บใช้ `@microsoft/signalr` ใน `MarketplaceHubService` เชื่อม hub หลังล็อกอิน (ตามบทบาท); รับเหตุการณ์ออเดอร์/สต็อก ฯลฯ; พร็อกซี dev รองรับ WebSocket ไป backend

**อ้างอิง:** `core/services/marketplace-hub.service.ts`, `frontend/proxy.conf.js`, backend `Hubs/MarketplaceHub.cs`

---

## .NET (Backend API)

### `Result<T>` / DTO

**คืออะไร:** **`Result<T>`** (ใน `DTO/Result.cs`) เป็นสัญญาเดียวของ API — `success`, `message`, `traceId`, `data`; **DTO** คือชนิดข้อมูลเข้า/ออกที่แยกจาก entity ฐานข้อมูล

**ทำไม:** ฝั่งเว็บจัดการคำตอบแบบเดียว (interceptor ถอด `data`, จับความล้มเหลวจาก `success`); ฝั่งซัพพอร์ตอ้าง **`traceId`** คู่กับ log; ข้อความสำเร็จ/ผิดมาจาก **`AppSettings`** แทนการ hard-code ในหลาย controller

**ทำได้อะไร:** ทุกหน้าเรียก `HttpClient` แล้วได้ข้อมูล “แบบเดิม” โดยไม่ต้องเขียนถอดห่อซ้ำ; เมื่อ `success: false` โค้ดฝั่ง Angular ไปเข้า `error` ของ `subscribe`/`catchError` ได้สม่ำเสมอ

**โปรเจกต์ทำอะไร:** Controller คืน **`Result<T>`** เองในแต่ละ action (`ITrace` + `AppSettings`); DTO ใน `Models/Dtos/`; ฝั่ง Angular ใช้ **`api-envelope.interceptor.ts`** ถอด `data` เมื่อสำเร็จ และแปลง `success: false` เป็น **`HttpErrorResponse`**

**อ้างอิง:** `Controllers/`, `Models/Dtos/`, `DTO/Result.cs`, `Utilities/AppSettings.cs`

---

### เส้นทาง Controller (`api/[controller]/[action]`)

**คืออะไร:** ค่าคงที่ **`Constant.AuthorizeConfig.RouteController`** ใน `Utilities/Constant.cs` กำหนดให้ URL มีทั้งชื่อ controller และชื่อ action ใน route (ค่าเริ่มต้นตัด `Async` ออก — เช่นเมธอด `GetCatalogAsync` → `/api/Products/GetCatalog`)

**ทำไม:** แต่ละ endpoint ชี้ชัดจากชื่อเมธอดในโค้ด ลดการซ้อนเทมเพลตสตริง `"login"` / `"mine"` ซ้ำ; เมื่อมีหลาย `HttpPost`/`HttpGet` คล้ายกัน แยกกันด้วยชื่อ action ได้ไม่งง; เก็บรูปแบบเส้นทางไว้ที่เดียวแก้ทีหลังง่าย

**ทำได้อะไร:** อ่าน Swagger หรือโค้ดแล้วรู้ทันทีว่าเมธอดไหนตรงกับ URL ไหน; ฝั่ง Angular เรียก **ชื่อใน route** (ค่าเริ่มต้น ASP.NET Core จะ **ตัด suffix `Async` ออกจากชื่อ action ใน URL** — เช่นเมธอด `LoginAsync` → path `/api/Auth/Login`)

**อ้างอิง:** `Utilities/Constant.cs`, `Controllers/`

---

### Entity Framework Core / LINQ

**คืออะไร:** ORM สำหรับ .NET — เขียน query ด้วย LINQ แมปไป SQL; จัดการ entity และความสัมพันธ์

**โปรเจกต์ทำอะไร:** `AppDbContext`, repository ใช้ `Include` โหลดความสัมพันธ์ชัดเจน — **ไม่เปิด lazy-loading proxies** เพื่อควบคุมจำนวน query

**อ้างอิง:** `Data/AppDbContext.cs`, `Repositories/`

---

### Code First / DB Migration

**คืออะไร:** นิยาม entity ในโค้ดก่อน แล้วให้ EF สร้าง/ปรับสคีมาผ่าน migration

**โปรเจกต์ทำอะไร:** มีโฟลเดอร์ `Migrations/`; seed ข้อมูลทดสอบตอนสตาร์ท

**อ้างอิง:** `Migrations/`, `Data/`, `Program.cs` (เรียก seeder)

---

### Async / Await

**คืออะไร:** รอ I/O (DB, HTTP) โดยไม่บล็อกเธรดแบบ synchronous ยาว ๆ

**โปรเจกต์ทำอะไร:** Controller → Service → Repository ใช้ `Task` / `async` / `await` กับ EF และ logic ทั่วไป

**อ้างอิง:** `Services/`, `Repositories/`, `Controllers/`

---

### Dependency Injection

**คืออะไร:** คอนเทนเนอร์สร้าง instance ให้ แทนการ `new` เองใน class — แยก interface กับ implementation

**โปรเจกต์ทำอะไร:** ลงทะเบียนใน `Program.cs` — `AddScoped` สำหรับ service/repository/`ITrace` ต่อ request, `AddTransient` สำหรับ `IJwtService`/`AppSettings`, `AddSingleton` สำหรับคิว in-memory ฯลฯ

**อ้างอิง:** `Program.cs` (บล็อก `AddScoped` / `AddSingleton` / `AddHostedService`)

---

### Queue / Channel

**คืออะไร:** **Channel** (`System.Threading.Channels`) เป็นคิวในหน่วยความจำ — producer ส่งข้อความ consumer อ่านทีละชิ้น (ไม่ใช่ RabbitMQ ในตัวอย่างนี้)

**โปรเจกต์ทำอะไร:** หลังสร้างออเดอร์สำเร็จ `OrderService` enqueue `OrderPlacedMessage`; `OrderPlacedConsumer` (BackgroundService) อ่านคิวแล้ว log เป็น demo ต่อยอดได้

**อ้างอิง:** `Queue/`, `Services/OrderService.cs` (เรียก `EnqueueAsync`)

---

### JWT / Authentication / Authorization / CORS

**คืออะไร:**

- **JWT:** โทเคน stateless ยืนยันตัวตน
- **Authentication:** รู้ว่าใคร
- **Authorization:** รู้ว่าทำได้ไหม (บทบาท/นโยบาย)
- **CORS:** ให้เบราว์เซอร์จาก origin อื่นเรียก API ได้ตาม policy

**โปรเจกต์ทำอะไร:** JWT Bearer ใน `Program.cs` + `JwtOptions`; `[Authorize]` / roles บน controller; policy CORS ชื่อ `Frontend` สำหรับ Angular dev

**อ้างอิง:** `Program.cs`, `Options/JwtOptions.cs`, `Utilities/JwtService.cs`, `Utilities/AppSettings.cs` (`AppSettings.Jwt`), `Controllers/`

---

### Logging (NLog)

**คืออะไร:** ไลบรารี log ฝั่งเซิร์ฟเวอร์ — เขียนไฟล์/console ตามระดับ (Info, Warn, Error) พร้อม layout

**โปรเจกต์ทำอะไร:** `nlog.config` เขียน `logs/info-*.log`, `logs/error-*.log`, `logs/backend-*.log`; layout ใช้ **`${aspnet-TraceIdentifier}`** เพื่อให้ log บรรทัดเดียวกับที่ ASP.NET Core ใช้ติดตามคำขอ — จับคู่กับ **`traceId`** ใน `Result` / ฝั่งเครือข่ายได้; `<extensions>` โหลด `NLog.Web.AspNetCore`; `UseNLog()` ใน host

**อ้างอิง:** `nlog.config`, `Program.cs`, `Utilities/Trace.cs` (`ITrace` / `Result.traceId` ใช้ลำดับ Activity ก่อน แล้ว `TraceIdentifier`)

---

### Health check

**คืออะไร:** endpoint ให้ orchestrator / load balancer ถามว่า “พร้อมรับ traffic หรือยัง” (แยก live vs ready ได้)

**ทำไมมี JSON กำหนดเอง:** ใช้ **ResponseWriter** สร้าง body เป็น `ServerStatus` + รายการ dependency (ชื่อแต่ละ check, สถานะ, ประเภท Server/Component) และบังคับ **HTTP 200** ทุกสถานะ (Healthy / Degraded / Unhealthy) — มอนิเตอร์อ่านรายละเอียดจาก JSON ได้เสมอโดยไม่ต้องพึ่งแค่รหัส HTTP

**โปรเจกต์ทำอะไร:** `/health`, `/health/live`, `/health/ready` — ready รวมการต่อ SQL ผ่าน `DatabaseHealthCheck`; live เป็น check ตัวเบา; body เป็น JSON ตาม DTO ใน `DTO/HealthCheck/`

**อ้างอิง:** `Health/DatabaseHealthCheck.cs`, `DTO/HealthCheck/`, `Program.cs` (`MapHealthChecks`, `JsonHealthCheckResponseWriter`)

---

### ErrorController

**คืออะไร:** endpoint **`/error`** ที่ ASP.NET Core เรียกผ่าน **`UseExceptionHandler("/error")`** เมื่อมี exception ที่ไม่ได้จับใน action — คืน **`Result<object>`** (`Success = false`, ข้อความจาก `AppSettings.ErrorMessage.General`, HTTP 200)

**ทำไม:** SPA เรียก API เป็น JSON อยู่แล้ว — ให้ความผิดพลาดที่ไม่ได้จับมี **รูปแบบเดียวกับคำตอบปกติ** (`Result`) แทน HTML หรือหน้า error ของเซิร์ฟเวอร์ เพื่อให้ interceptor/handler ฝั่งเว็บจัดการแบบเดียวกัน

**ทำได้อะไร:** โค้ด Angular ยังใช้ `success` / `traceId` ตามเดิม; ผู้ใช้หรือซัพพอร์ตใช้ **TraceId** ไล่คู่กับ NLog

**โปรเจกต์ทำอะไร:** ไม่มี endpoint debug แยก; **`traceId`** ใน `Result` มาจาก **`ITrace`** (`Utilities/Trace.cs`)

**อ้างอิง:** `Controllers/ErrorController.cs`, `Program.cs` (`UseExceptionHandler`)

---

## สิ่งที่ไม่ได้ใส่ในรายการแต่เกี่ยวข้อง

| หัวข้อ | สั้น ๆ |
|--------|--------|
| **Standalone Angular** | ไม่ใช้ `NgModule` หลัก — bootstrap จาก `app.config` + `loadComponent` |
| **UseExceptionHandler("/error")** | Exception ที่ไม่ได้จับใน action → **`ErrorController`** คืน **`Result`** เป็น JSON |
| **ไม่มี global filter ห่อคำตอบ** | Controller ประกอบ **`Result<T>`** เองทุก action — ควบคุมข้อความ/สาขาสำเร็จ-ล้มเหลวต่อ endpoint ได้ชัด ไม่ซ่อนชั้นห่อจาก pipeline กลาง |

หากต้องการ **เวอร์ชันสั้นสำหรับสไลด์** ให้ดู `POWERPOINT_PRESENTATION_GUIDE.md` และตารางโครงสร้างใน `PROJECT_CHECKLIST_SUMMARY.md`
