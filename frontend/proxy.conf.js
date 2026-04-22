const target = process.env['BACKEND_URL'] || 'http://localhost:5197';

function attachProxyHints(proxy, label) {
  proxy.on('error', (err) => {
    if (err?.code === 'ECONNREFUSED') {
      console.warn(
        `[proxy ${label}] ต่อ ${target} ไม่ได้ — รัน backend ก่อน`,
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
