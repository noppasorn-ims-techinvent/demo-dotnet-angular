/**
 * โปรซีไปยัง API + SignalR
 * - ค่าเริ่มต้น http://localhost:5197 ตรงกับ backend/Properties/launchSettings.json (profile http)
 * - ถ้า backend รันพอร์ตอื่น: BACKEND_URL=http://localhost:XXXX npm start
 */
const target = process.env['BACKEND_URL'] || 'http://localhost:5197';

module.exports = {
  '/api': {
    target,
    secure: false,
    changeOrigin: true,
  },
  '/hubs': {
    target,
    secure: false,
    changeOrigin: true,
    ws: true,
  },
};
