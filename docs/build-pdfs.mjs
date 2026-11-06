// Convierte docs/src/*.md en docs/*.pdf (Chrome/Edge headless). Sin dependencias: soporta el subconjunto de Markdown
// que usan los documentos (titulos, tablas, listas, codigo, negritas, codigo en linea, citas).
// Uso: node docs/build-pdfs.mjs [nombre ...]      (sin argumentos: todos)
import { readFileSync, writeFileSync, readdirSync, mkdtempSync, rmSync, existsSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import { join, dirname, resolve } from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const browser = process.env.BROWSER ?? [
  'C:/Program Files (x86)/Microsoft/Edge/Application/msedge.exe',
  'C:/Program Files/Google/Chrome/Application/chrome.exe',
  '/usr/bin/google-chrome', '/usr/bin/chromium',
].find(existsSync);
if (!browser) throw new Error('Define BROWSER con la ruta de Chrome o Edge');

const esc = (s) => s.replaceAll('&', '&amp;').replaceAll('<', '&lt;').replaceAll('>', '&gt;');
const inline = (s) => esc(s)
  .replace(/`([^`]+)`/g, '<code>$1</code>')
  .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');

const isOrdered = (l) => /^\d+\. /.test(l);
const isItem = (l) => /^\s*[-*] /.test(l) || isOrdered(l);
const cells = (r) => r.slice(1, r.endsWith('|') ? -1 : undefined).split('|').map((c) => c.trim());
const tr = (tag, row) => `<tr>${row.map((c) => `<${tag}>${inline(c)}</${tag}>`).join('')}</tr>`;

function md(src) {
  const lines = src.split('\n').map((x) => x.replace('\r', ''));
  const out = [];
  // Toma desde la linea i mientras se cumpla pred; devuelve [lineas, siguiente indice].
  const take = (i, pred) => { let j = i; while (j < lines.length && pred(lines[j])) j++; return [lines.slice(i, j), j]; };
  let i = 0;
  while (i < lines.length) {
    const l = lines[i];
    let block;
    let next = i + 1;
    if (l.startsWith('```')) {
      const end = lines.indexOf('```', i + 1);
      const stop = end < 0 ? lines.length : end;
      block = `<pre>${esc(lines.slice(i + 1, stop).join('\n'))}</pre>`;
      next = stop + 1;
    } else if (/^#{1,4} /.test(l)) {
      const n = l.indexOf(' ');
      block = `<h${n}>${inline(l.slice(n + 1))}</h${n}>`;
    } else if (l.startsWith('|')) {
      let rows;
      [rows, next] = take(i, (x) => x.startsWith('|'));
      block = `<table><thead>${tr('th', cells(rows[0]))}</thead><tbody>${rows.slice(2).map((r) => tr('td', cells(r))).join('')}</tbody></table>`;
    } else if (isItem(l)) {
      let items;
      [items, next] = take(i, isItem);
      const tag = isOrdered(l) ? 'ol' : 'ul';
      block = `<${tag}>${items.map((x) => `<li>${inline(x.slice(x.indexOf(' ') + 1))}</li>`).join('')}</${tag}>`;
    } else if (l.startsWith('> ')) {
      block = `<blockquote>${inline(l.slice(2))}</blockquote>`;
    } else if (l.trim() !== '') {
      block = `<p>${inline(l)}</p>`;
    }
    if (block) out.push(block);
    i = next;
  }
  return out.join('\n');
}

const css = `body{font:11pt/1.45 Segoe UI,Arial,sans-serif;color:#1b1b1f;margin:0}h1{font-size:22pt;border-bottom:3px solid #512bd4;padding-bottom:6px}
h2{font-size:15pt;color:#512bd4;margin-top:22px;break-after:avoid}h3{font-size:12pt;break-after:avoid}table{border-collapse:collapse;width:100%;margin:8px 0;font-size:9.5pt}
th{background:#512bd4;color:#fff;text-align:left}th,td{border:1px solid #c9c9d3;padding:4px 6px;vertical-align:top}tr{break-inside:avoid}
code{background:#f0eefb;padding:0 3px;border-radius:3px;font-size:9.5pt}pre{background:#f6f6fa;border:1px solid #ddd;padding:8px;font-size:8.5pt;white-space:pre-wrap;break-inside:avoid}
blockquote{border-left:4px solid #512bd4;margin:8px 0;padding:2px 10px;background:#f6f4fd}@page{margin:18mm 16mm}`;

const wanted = process.argv.slice(2);
const srcDir = join(here, 'src');
for (const f of readdirSync(srcDir).filter((x) => x.endsWith('.md'))) {
  const name = f.slice(0, -3);
  if (wanted.length && !wanted.includes(name)) continue;
  const html = `<!doctype html><html lang="es"><meta charset="utf-8"><title>${name}</title><style>${css}</style><body>${md(readFileSync(join(srcDir, f), 'utf8'))}</body></html>`;
  const tmp = mkdtempSync(join(tmpdir(), 'pdf-'));
  const page = join(tmp, `${name}.html`);
  writeFileSync(page, html);
  const pdf = resolve(here, `${name}.pdf`);
  execFileSync(browser, ['--headless=new', '--disable-gpu', '--no-pdf-header-footer', `--user-data-dir=${join(tmp, 'p')}`, `--print-to-pdf=${pdf}`, pathToFileURL(page).href], { stdio: 'ignore' });
  rmSync(tmp, { recursive: true, force: true });
  console.log('OK', pdf);
}
