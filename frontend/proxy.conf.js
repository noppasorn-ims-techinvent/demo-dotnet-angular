/**
 * โปรซีไปยัง API + SignalR + health
 * - ค่าเริ่มต้น http://localhost:5197 ตรงกับ backend/Properties/launchSettings.json (profile http)
 * - ถ้า backend รันพอร์ตอื่น: BACKEND_URL=http://localhost:XXXX npm start
 *
 * ECONNREFUSED = ยังไม่มี API ฟังที่ target — เปิดอีกเทอร์มินัลแล้วรัน:
 *   cd backend && dotnet watch run
 */
const target = process.env['BACKEND_URL'] || 'http://localhost:5197';

function attachProxyHints(proxy, label) {
  proxy.on('error', (err) => {
    if (err?.code === 'ECONNREFUSED') {
      console.warn(
        `[proxy ${label}] ต่อ ${target} ไม่ได้ — รัน backend ก่อน: cd ../backend && dotnet watch run (หรือตั้ง BACKEND_URL ให้ตรงพอร์ตที่ API ฟังอยู่)`,
      );
    }
  });
}

console.info(`[proxy] → ${target} (override: BACKEND_URL=... npm start)`);

const common = {
  target,
  secure: false,
  changeOrigin: true,
};

module.exports = {
  '/api': {
    ...common,
    configure: (proxy) => attachProxyHints(proxy, 'api'),
  },
  '/hubs': {
    ...common,
    ws: true,
    configure: (proxy) => attachProxyHints(proxy, 'hubs'),
  },
  '/health': {
    ...common,
    configure: (proxy) => attachProxyHints(proxy, 'health'),
  },
};
