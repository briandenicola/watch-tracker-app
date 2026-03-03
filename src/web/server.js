import express from 'express';
import { createProxyMiddleware } from 'http-proxy-middleware';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const distDir = path.join(__dirname, 'dist');

const PORT = parseInt(process.env.PORT || '3000', 10);
const API_URL = process.env.API_URL || 'http://localhost:8080';

const app = express();

const proxyOptions = {
  target: API_URL,
  changeOrigin: true,
  timeout: 120_000,
  proxyTimeout: 120_000,
};

app.use('/api', createProxyMiddleware(proxyOptions));
app.use('/uploads', createProxyMiddleware(proxyOptions));

app.use(express.static(distDir));

// SPA fallback
app.use((_req, res) => {
  res.sendFile(path.join(distDir, 'index.html'));
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`Web server listening on :${PORT}, proxying API → ${API_URL}`);
});
