async function api(path, method='GET', body) {
  const opts = { method, headers: {} };
  if (body) { opts.headers['Content-Type'] = 'application/json'; opts.body = JSON.stringify(body); }
  const res = await fetch('/api' + path, opts);
  if (!res.ok) throw new Error('API error');
  return res.json();
}

function el(tag, cls, txt){ const e = document.createElement(tag); if(cls) e.className = cls; if(txt) e.textContent = txt; return e; }

async function load() {
  const tasks = await api('/tasks');
  const container = document.getElementById('tasks'); container.innerHTML='';
  for (const t of tasks) {
    const card = el('div','card');
    const row = el('div','row');
    const left = el('div');
    left.appendChild(el('div', 'title', t.text));
    left.appendChild(el('div','meta', `${t.category} • ${t.priority}`));
    const right = el('div');
    const del = el('button', '', 'Delete'); del.onclick = async ()=>{ await api('/tasks/' + t.id, 'DELETE'); load(); };
    right.appendChild(del);
    row.appendChild(left); row.appendChild(right);
    card.appendChild(row); container.appendChild(card);
  }
}

document.getElementById('addBtn').addEventListener('click', async ()=>{
  const text = document.getElementById('taskInput').value.trim();
  const category = document.getElementById('categoryInput').value.trim() || 'General';
  if (!text) return; await api('/tasks', 'POST', { text, category }); document.getElementById('taskInput').value=''; load();
});

document.getElementById('themeSelect').addEventListener('change', (e)=>{
  document.documentElement.classList.toggle('light', e.target.value === 'light');
});

document.getElementById('sizeSelect').addEventListener('change', (e)=>{
  document.documentElement.style.setProperty('--scale', e.target.value);
});

load().catch(err=>console.error(err));
