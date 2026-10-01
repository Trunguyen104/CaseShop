const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = (relativePath) => fs.readFileSync(path.join(root, relativePath), 'utf8');

test('home loads the latest active products through the product service', () => {
  const component = read('CaseShop.Web/Components/Pages/HomeSections/HomeTrendingSection.razor');
  const repository = read('CaseShop.Web/Repositories/ProductRepository.cs');

  assert.match(component, /@inject IProductService ProductService/);
  assert.match(component, /@rendermode InteractiveServer/);
  assert.match(component, /GetLatestActiveProductsAsync\(4\)/);
  assert.match(component, /catch \(Exception ex\)/);
  assert.match(component, /<AppSpinner/);
  assert.match(component, /<AppEmptyState/);
  assert.match(repository, /Where\(product => product\.IsActive\)/);
  assert.match(repository, /OrderByDescending\(product => product\.CreatedAt\)/);
  assert.match(repository, /Take\(count\)/);
  assert.match(repository, /AsNoTracking\(\)/);
});

test('storefront no longer exposes the public sticker collection page', () => {
  const home = read('CaseShop.Web/Components/Pages/Home.razor');
  const stickerPage = path.join(root, 'CaseShop.Web/Components/Pages/Stickers/Index.razor');

  assert.doesNotMatch(home, /Xem bộ sưu tập Sticker/);
  assert.doesNotMatch(home, /href="\/stickers"/);
  assert.equal(fs.existsSync(stickerPage), false);
});
