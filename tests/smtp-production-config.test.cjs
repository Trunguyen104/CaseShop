const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('production compose maps SMTP aliases to ASP.NET Core configuration', () => {
  const compose = read('docker-compose.prod.yml');
  const requiredMappings = [
    'Smtp__Host=${SMTP_HOST:',
    'Smtp__Port=${SMTP_PORT:-587}',
    'Smtp__EnableSsl=${SMTP_ENABLE_SSL:-true}',
    'Smtp__Username=${SMTP_USERNAME:',
    'Smtp__Password=${SMTP_PASSWORD:',
    'Smtp__FromEmail=${SMTP_FROM_EMAIL:',
    'Smtp__FromName=${SMTP_FROM_NAME:-ME-ISM}',
    'Smtp__SupportEmail=${SMTP_SUPPORT_EMAIL:'
  ];

  for (const mapping of requiredMappings) assert.ok(compose.includes(mapping), `Missing ${mapping}`);
});

test('local environment loader maps SMTP TLS setting consistently', () => {
  const loader = read('CaseShop.Web/Common/EnvLoader.cs');
  const example = read('.env.example');
  assert.match(loader, /case "SMTP_ENABLE_SSL".*"Smtp__EnableSsl"/);
  assert.match(example, /^SMTP_ENABLE_SSL=true$/m);
});
