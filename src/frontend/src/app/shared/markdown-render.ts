/**
 * Minimal, dependency-free Markdown renderer for the Smart Study Assistant page.
 * Escapes HTML first, then converts a safe subset of Markdown
 * (headings, bold/italic, code, lists, tables, blockquotes, links, hr).
 */
export function renderMarkdown(source: string): string {
  let text = escapeHtml(source ?? '');
  const lines = text.split(/\r?\n/);
  const out: string[] = [];
  let inList: 'ul' | 'ol' | null = null;
  let inCode = false;
  let tableRows: string[][] = [];

  const closeList = () => {
    if (inList) {
      out.push(`</${inList}>`);
      inList = null;
    }
  };
  const flushTable = () => {
    if (!tableRows.length) return;
    const [head, ...body] = tableRows;
    const cell = (c: string, tag: string) => `<${tag}>${inline(c)}</${tag}>`;
    out.push('<table><thead><tr>' + head.map((c) => cell(c, 'th')).join('') + '</tr></thead>');
    if (body.length) {
      out.push('<tbody>' + body.map((r) => '<tr>' + r.map((c) => cell(c, 'td')).join('') + '</tr>').join('') + '</tbody>');
    }
    out.push('</table>');
    tableRows = [];
  };

  for (const rawLine of lines) {
    const line = rawLine.trimEnd();

    if (/^```/.test(line.trim())) {
      flushTable();
      closeList();
      out.push(inCode ? '</code></pre>' : '<pre><code>');
      inCode = !inCode;
      continue;
    }
    if (inCode) {
      out.push(line);
      continue;
    }

    if (/^\|.*\|/.test(line)) {
      const cells = line.split('|').slice(1, -1).map((c) => c.trim());
      if (cells.every((c) => /^:?-{2,}:?$/.test(c))) continue; // separator row
      tableRows.push(cells);
      continue;
    }
    flushTable();

    const heading = line.match(/^(#{1,6})\s+(.*)$/);
    if (heading) {
      closeList();
      const level = heading[1].length;
      out.push(`<h${level}>${inline(heading[2])}</h${level}>`);
      continue;
    }

    if (/^(---+|\*\*\*+)\s*$/.test(line.trim())) {
      closeList();
      out.push('<hr>');
      continue;
    }

    const quote = line.match(/^&gt;\s?(.*)$/);
    if (quote) {
      closeList();
      out.push(`<blockquote>${inline(quote[1])}</blockquote>`);
      continue;
    }

    const ul = line.match(/^[-*+]\s+(.*)$/);
    if (ul) {
      if (inList !== 'ul') {
        closeList();
        out.push('<ul>');
        inList = 'ul';
      }
      out.push(`<li>${inline(ul[1])}</li>`);
      continue;
    }

    const ol = line.match(/^\d+[.)]\s+(.*)$/);
    if (ol) {
      if (inList !== 'ol') {
        closeList();
        out.push('<ol>');
        inList = 'ol';
      }
      out.push(`<li>${inline(ol[1])}</li>`);
      continue;
    }

    closeList();
    if (line.trim().length > 0) {
      out.push(`<p>${inline(line)}</p>`);
    }
  }

  flushTable();
  closeList();
  if (inCode) out.push('</code></pre>');
  return out.join('\n');
}

function inline(text: string): string {
  return text
    .replace(/`([^`]+)`/g, '<code>$1</code>')
    .replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>')
    .replace(/\*([^*]+)\*/g, '<em>$1</em>')
    .replace(/!\[([^\]]*)\]\(([^)\s]+)\)/g, '<span>$1</span>') // strip images (untrusted)
    .replace(/\[([^\]]+)\]\(([^)\s]+)\)/g, (_m, label: string, href: string) =>
      /^https?:\/\//i.test(href) ? `<a href="${href}" target="_blank" rel="noopener noreferrer">${label}</a>` : label)
    .replace(/ {2}$/, '<br>');
}

function escapeHtml(value: string): string {
  return (value ?? '')
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}
