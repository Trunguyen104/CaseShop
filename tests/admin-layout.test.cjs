const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = (relativePath) => fs.readFileSync(path.join(root, relativePath), 'utf8');

test('admin pages use a dedicated layout without storefront chrome', () => {
  const imports = read('CaseShop.Web/Components/Pages/Admin/_Imports.razor');
  const layout = read('CaseShop.Web/Components/Layout/AdminLayout.razor');

  assert.match(imports, /@layout\s+AdminLayout/);
  assert.doesNotMatch(layout, /<Header\b/);
  assert.doesNotMatch(layout, /<Footer\b/);
  assert.match(layout, /min-h-screen/);
});

test('admin shell no longer reserves space for the storefront header', () => {
  const shell = read('CaseShop.Web/Components/Shared/AdminShell.razor');

  assert.match(shell, /min-h-screen/);
  assert.match(shell, /sticky top-0/);
  assert.doesNotMatch(shell, /top-16/);
  assert.match(shell, /CASESHOP Admin/);
});

test('storefront layout keeps its header and footer', () => {
  const layout = read('CaseShop.Web/Components/Layout/MainLayout.razor');

  assert.match(layout, /<Header\s*\/>/);
  assert.match(layout, /<Footer\s*\/>/);
});
