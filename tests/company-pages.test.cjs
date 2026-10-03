const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('about page presents the project, product journey and complete team', () => {
  const source = read('CaseShop.Web/Components/Pages/Company/About.razor');
  assert.match(source, /@page "\/about"/);
  assert.match(source, /Print-on-Demand/);
  assert.match(source, /TeamMemberCard/);
  assert.match(source, /Nguyễn Thị Hải Hân/);
  assert.match(source, /Nguyễn Phạm Thiên Phú/);
  assert.match(source, /Bùi Thái Huy/);
  assert.match(source, /Nguyễn Thị Yến Vy/);
  assert.match(source, /Nguyễn Thị Quỳnh Như/);
  assert.match(source, /Hồ Thị Thu Ngân/);
});

test('contact page exposes usable support channels without a fake form', () => {
  const source = read('CaseShop.Web/Components/Pages/Company/Contact.razor');
  assert.match(source, /@page "\/contact"/);
  assert.match(source, /mailto:meismexe101@gmail.com/);
  assert.match(source, /tel:\+84949723852/);
  assert.match(source, /href="\/track"/);
  assert.doesNotMatch(source, /<EditForm|<form/);
});

test('storefront navigation links to the company pages', () => {
  const header = read('CaseShop.Web/Components/Shared/Header.razor');
  const footer = read('CaseShop.Web/Components/Shared/Footer.razor');
  for (const route of ['/about', '/contact']) {
    assert.match(header, new RegExp(`href="${route}"`));
    assert.match(footer, new RegExp(`href="${route}`));
  }
});
