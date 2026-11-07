#!/usr/bin/env node
// Kiểm tra cấu trúc production/BACKLOG.md. Thay việc CPO tự đếm sau mỗi lần sửa.
// Chạy từ gốc repo: node scripts/backlog-check.mjs [đường-dẫn-BACKLOG]   (thoát mã 1 nếu có lỗi)
// Không cần package.json hay thư viện ngoài; chỉ cần Node 18+.
import { existsSync, readFileSync } from 'node:fs';

const FILE = process.argv[2] ?? 'production/BACKLOG.md';
if (!existsSync(FILE)) {
  console.error(`Không thấy ${FILE}. Chạy lệnh từ gốc repo hoặc truyền đường dẫn.`);
  process.exit(1);
}
const STATUS = /^(todo|doing|review|done|blocked|loại|gộp vào T-\d{3})$/;
const text = readFileSync(FILE, 'utf8');
const errors = [];
const rows = new Map();

text.split(/\r?\n/).forEach((line, i) => {
  if (!/^\|\s*T-\d{3}\s*\|/.test(line)) return;
  const cells = line
    .split('|')
    .slice(1, -1)
    .map((c) => c.trim());
  const id = cells[0];
  const where = `${FILE}:${i + 1} ${id}`;
  if (cells.length !== 7) errors.push(`${where}: có ${cells.length} cột, cần 7`);
  if (rows.has(id)) errors.push(`${where}: trùng mã với dòng ${rows.get(id).line}`);
  rows.set(id, { line: i + 1, deps: cells[3] ?? '', status: cells[5] ?? '' });
  if (cells[5] !== undefined && !STATUS.test(cells[5]))
    errors.push(`${where}: trạng thái "${cells[5]}" không hợp lệ`);
});

for (const [id, r] of rows) {
  // Hỗ trợ dải "T-001..T-005"
  const deps = new Set();
  for (const m of r.deps.matchAll(/T-(\d{3})(?:\.\.T-(\d{3}))?/g)) {
    const a = Number(m[1]);
    const b = m[2] ? Number(m[2]) : a;
    for (let n = a; n <= b; n++) deps.add(`T-${String(n).padStart(3, '0')}`);
  }
  for (const d of deps) {
    if (d === id) errors.push(`${id}: phụ thuộc chính nó`);
    else if (!rows.has(d)) errors.push(`${id}: phụ thuộc ${d} không tồn tại`);
  }
  r.depSet = deps;
}

// Phụ thuộc vòng
const state = new Map();
const visit = (id, path) => {
  if (state.get(id) === 'done') return;
  if (state.get(id) === 'open') {
    errors.push(`Phụ thuộc vòng: ${[...path, id].join(' → ')}`);
    return;
  }
  state.set(id, 'open');
  for (const d of rows.get(id)?.depSet ?? []) if (rows.has(d)) visit(d, [...path, id]);
  state.set(id, 'done');
};
for (const id of rows.keys()) visit(id, []);

// Mã lớn nhất ghi ở đầu file phải khớp
const declared = text.match(/Mã task lớn nhất đã cấp:\s*\*\*T-(\d{3})\*\*/);
// BACKLOG rỗng: mã lớn nhất là T-000
const max = rows.size ? Math.max(...[...rows.keys()].map((k) => Number(k.slice(2)))) : 0;
if (!declared) errors.push('Thiếu dòng "Mã task lớn nhất đã cấp: **T-###**"');
else if (Number(declared[1]) !== max)
  errors.push(
    `Dòng "Mã task lớn nhất" ghi T-${declared[1]} nhưng mã lớn nhất thực tế là T-${String(max).padStart(3, '0')}`,
  );

const count = {};
for (const r of rows.values()) count[r.status] = (count[r.status] ?? 0) + 1;

if (errors.length) {
  console.error(`BACKLOG lỗi (${errors.length}):\n- ` + errors.join('\n- '));
  process.exit(1);
}
console.log(
  [`BACKLOG ổn: ${rows.size} task`, ...Object.entries(count).map(([k, v]) => `${k} ${v}`)].join(
    ' · ',
  ),
);
