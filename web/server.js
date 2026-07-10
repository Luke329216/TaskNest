const http = require('http');
const fs = require('fs');
const path = require('path');
const url = require('url');

const DATA_DIR = path.join(__dirname, 'data');
const TASKS_FILE = path.join(DATA_DIR, 'tasks.json');

// Ensure data dir exists
if (!fs.existsSync(DATA_DIR)) fs.mkdirSync(DATA_DIR, { recursive: true });
if (!fs.existsSync(TASKS_FILE)) fs.writeFileSync(TASKS_FILE, JSON.stringify({ categories: [], tasks: [] }, null, 2));

function readData() {
  try { return JSON.parse(fs.readFileSync(TASKS_FILE, 'utf8')); }
  catch (e) { return { categories: [], tasks: [] }; }
}

function writeData(data) { fs.writeFileSync(TASKS_FILE, JSON.stringify(data, null, 2)); }

function sendJson(res, obj, code = 200) {
  const body = JSON.stringify(obj);
  res.writeHead(code, { 'Content-Type': 'application/json', 'Content-Length': Buffer.byteLength(body) });
  res.end(body);
}

function serveStatic(req, res, pathname) {
  const filePath = path.join(__dirname, 'public', pathname === '/' ? 'index.html' : pathname);
  if (!filePath.startsWith(path.join(__dirname, 'public'))) return false;
  fs.readFile(filePath, (err, data) => {
    if (err) { res.writeHead(404); res.end('Not found'); return; }
    const ext = path.extname(filePath).toLowerCase();
    const map = { '.html': 'text/html', '.js': 'application/javascript', '.css': 'text/css', '.png': 'image/png' };
    res.writeHead(200, { 'Content-Type': map[ext] || 'application/octet-stream' });
    res.end(data);
  });
  return true;
}

const server = http.createServer((req, res) => {
  const parsed = url.parse(req.url, true);
  const pathname = decodeURIComponent(parsed.pathname);

  if (pathname.startsWith('/api')) {
    // Simple REST API
    const data = readData();
    if (req.method === 'GET' && pathname === '/api/tasks') {
      return sendJson(res, data.tasks);
    }

    if (req.method === 'GET' && pathname === '/api/categories') {
      return sendJson(res, data.categories);
    }

    if (req.method === 'POST' && pathname === '/api/tasks') {
      let body = '';
      req.on('data', c => body += c);
      req.on('end', () => {
        try {
          const payload = JSON.parse(body || '{}');
          const task = {
            id: Date.now().toString(36) + Math.random().toString(36).slice(2,8),
            text: payload.text || '',
            category: payload.category || 'General',
            priority: payload.priority || 'Medium',
            completed: false
          };
          data.tasks.push(task);
          if (!data.categories.includes(task.category)) data.categories.push(task.category);
          writeData(data);
          sendJson(res, task, 201);
        } catch (e) { sendJson(res, { error: 'invalid json' }, 400); }
      });
      return;
    }

    if ((req.method === 'PUT' || req.method === 'PATCH') && pathname.startsWith('/api/tasks/')) {
      const id = pathname.split('/').pop();
      let body = '';
      req.on('data', c => body += c);
      req.on('end', () => {
        try {
          const payload = JSON.parse(body || '{}');
          const t = data.tasks.find(x => x.id === id);
          if (!t) return sendJson(res, { error: 'not found' }, 404);
          Object.assign(t, payload);
          writeData(data);
          sendJson(res, t);
        } catch (e) { sendJson(res, { error: 'invalid json' }, 400); }
      });
      return;
    }

    if (req.method === 'DELETE' && pathname.startsWith('/api/tasks/')) {
      const id = pathname.split('/').pop();
      const idx = data.tasks.findIndex(x => x.id === id);
      if (idx === -1) return sendJson(res, { error: 'not found' }, 404);
      const removed = data.tasks.splice(idx, 1)[0];
      writeData(data);
      return sendJson(res, removed);
    }

    // Unknown API
    res.writeHead(404, { 'Content-Type': 'application/json' });
    res.end(JSON.stringify({ error: 'not found' }));
    return;
  }

  // Serve static files from public/
  let p = pathname === '/' ? '/index.html' : pathname;
  if (!serveStatic(req, res, p)) { res.writeHead(404); res.end('Not found'); }
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => console.log(`TaskNest web server running at http://localhost:${PORT}/`));
