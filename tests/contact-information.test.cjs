const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('public pages consistently expose ME-ISM contact information', () => {
  const files = [
    'CaseShop.Web/Components/Pages/Company/Contact.razor',
    'CaseShop.Web/Components/Shared/Footer.razor'
  ];

  for (const file of files) {
    const source = read(file);
    assert.match(source, /meismexe101@gmail\.com/);
    assert.match(source, /\+84 949 723 852/);
    assert.match(source, /tel:\+84949723852/);
  }

  const tracking = read('CaseShop.Web/Components/Pages/Track.razor');
  assert.match(tracking, /mailto:meismexe101@gmail\.com/);
  assert.doesNotMatch(tracking, /support@caseshop\.vn/);
});

test('default email template support address matches the public project address', () => {
  const settings = JSON.parse(read('CaseShop.Web/appsettings.json'));
  const example = read('.env.example');
  assert.equal(settings.Smtp.SupportEmail, 'meismexe101@gmail.com');
  assert.match(example, /^SMTP_FROM_EMAIL=meismexe101@gmail\.com$/m);
  assert.match(example, /^SMTP_SUPPORT_EMAIL=meismexe101@gmail\.com$/m);
});
