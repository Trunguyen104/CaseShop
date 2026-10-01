const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const pagesRoot = path.resolve(__dirname, '../CaseShop.Web/Components/Pages');

function razorFiles(directory) {
  return fs.readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) return razorFiles(fullPath);
    return entry.name.endsWith('.razor') ? [fullPath] : [];
  });
}

test('all browser tab titles use the ME-ISM brand', () => {
  const titles = razorFiles(pagesRoot).flatMap((file) => {
    const source = fs.readFileSync(file, 'utf8');
    return [...source.matchAll(/<PageTitle>([\s\S]*?)<\/PageTitle>/g)]
      .map((match) => ({ file, title: match[1] }));
  });

  assert.ok(titles.length > 0);
  for (const { file, title } of titles) {
    assert.doesNotMatch(title, /CaseShop|CASESHOP/, `${file} still uses the old browser title brand`);
    assert.match(title, /ME-ISM/, `${file} browser title is missing the ME-ISM brand`);
  }
});
