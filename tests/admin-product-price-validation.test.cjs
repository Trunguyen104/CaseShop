const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = (relativePath) => fs.readFileSync(path.join(root, relativePath), 'utf8');

test('product DTO and service enforce a clear bounded price range', () => {
  const dto = read('CaseShop.Web/DTOs/ProductCreateUpdateDto.cs');
  const service = read('CaseShop.Web/Services/ProductService.cs');

  assert.match(dto, /Range\(typeof\(decimal\), "1", "1000000000"/);
  assert.match(dto, /Giá sản phẩm phải từ 1₫ đến 1\.000\.000\.000₫\./);
  assert.match(dto, /public int PriceInThousands/);
  assert.match(dto, /set => Price = value \* 1000m/);
  assert.match(service, /ValidatePrice\(dto\.Price\)/);
  assert.match(service, /price is < 1m or > 1_000_000_000m/);
});

for (const page of ['Create', 'Edit']) {
  test(`${page} product form shows meaningful price and business errors`, () => {
    const component = read(`CaseShop.Web/Components/Pages/Admin/Products/${page}.razor`);

    assert.match(component, /<InputNumber[\s\S]*?@bind-Value="dto\.PriceInThousands"/);
    assert.match(component, /ParsingErrorMessage="Giá sản phẩm phải là một số nguyên hợp lệ\."/);
    assert.match(component, /min="1"/);
    assert.match(component, /max="1000000"/);
    assert.match(component, /nhập 180, hệ thống sẽ lưu giá 180\.000₫/);
    assert.match(component, /catch \(ArgumentException ex\)/);
    assert.match(component, /GetBusinessErrorMessage\(ex\)/);
    assert.match(component, /Logger\.LogError\(ex/);
  });
}
