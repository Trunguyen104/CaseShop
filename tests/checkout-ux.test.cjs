const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('delivery address uses service and detailed address without map interop', () => {
  const source = read('CaseShop.Web/Components/Pages/Checkout/Components/DeliveryAddressSection.razor');
  assert.match(source, /@inject IVietnamAddressService/);
  assert.match(source, /EventCallback<DeliveryAddressSelection>/);
  assert.match(source, /Số nhà, tên đường, thông tin bổ sung/);
  assert.doesNotMatch(source, /IJSRuntime|checkout-delivery-map|OnMarkerChanged/);
  assert.doesNotMatch(source, /AppDbContext/);
});

test('checkout contract does not carry delivery coordinates', () => {
  const checkout = read('CaseShop.Web/DTOs/CheckoutDto.cs');
  const order = read('CaseShop.Web/Entities/Order.cs');
  assert.doesNotMatch(checkout, /Latitude|Longitude|IsLocationConfirmed/);
  assert.doesNotMatch(order, /Latitude|Longitude|IsLocationConfirmed/);
});

test('PayOS page renders QR, polls status and handles expiration', () => {
  const source = read('CaseShop.Web/Components/Pages/Checkout/PayOsPayment.razor');
  assert.match(source, /PeriodicTimer/);
  assert.match(source, /PaymentStatus\.Paid/);
  assert.match(source, /IsExpired/);
  assert.match(source, /CreatePayOsPaymentAsync/);
  assert.match(source, /OnAfterRenderAsync\(bool firstRender\)/);
  assert.match(source, /ClearCartOnSuccess/);
  assert.match(source, /CartService\.ClearAsync\(\)/);
  assert.match(source, /if \(_order\?\.PaymentStatus == PaymentStatus\.Paid\)/);
});

test('payment method radio supplies a value expression', () => {
  const source = read('CaseShop.Web/Components/Pages/Checkout/Components/PaymentMethodSection.razor');
  assert.match(source, /ValueExpression="@\(\(\) => Value\)"/);
});

test('PayOS checkout defers cart clearing until payment confirmation', () => {
  const source = read('CaseShop.Web/Components/Pages/Checkout/Checkout.razor');
  assert.match(source, /\?clearCart=true/);
  assert.match(source, /result\.PaymentMethod == PaymentMethodType\.PayOSQr/);
  assert.doesNotMatch(source, /if \(_checkoutFromCart\) await CartService\.ClearAsync\(\)/);
});

test('checkout clears stale submit errors and logs unexpected failures', () => {
  const source = read('CaseShop.Web/Components/Pages/Checkout/Checkout.razor');
  assert.match(source, /SubmitError="@_submitError"/);
  assert.doesNotMatch(source, /SubmitError="_submitError"/);
  assert.match(source, /@bind-Value:after="ClearSubmitError"/);
  assert.match(source, /HandlePaymentMethodChangedAsync/);
  assert.match(source, /Logger\.LogError\(ex, "Unexpected checkout submission failure\."\)/);
  assert.match(source, /private void ClearSubmitError\(\) => _submitError = null/);
});

test('checkout disables ordering until all required fields are valid', () => {
  const checkout = read('CaseShop.Web/Components/Pages/Checkout/Checkout.razor');
  const summary = read('CaseShop.Web/Components/Pages/Checkout/Components/CheckoutOrderSummary.razor');
  assert.match(checkout, /Validator\.TryValidateObject/);
  assert.match(checkout, /IsSubmitDisabled="@\(!IsCheckoutReady\)"/);
  assert.match(summary, /IsDisabled="@\(IsSubmitDisabled \|\| IsSubmitting\)"/);
  assert.match(summary, /Điền đầy đủ thông tin bắt buộc để đặt hàng\./);
});

test('content security policy permits the configured Google image host', () => {
  const source = read('CaseShop.Web/Common/SecurityExtensions.cs');
  assert.match(source, /img-src[^;]+https:\/\/lh3\.googleusercontent\.com;/);
});

test('COD success page reminds the customer to answer the confirmation call', () => {
  const source = read('CaseShop.Web/Components/Pages/Checkout/CheckoutSuccess.razor');
  assert.match(source, /PaymentMethodType\.CashOnDelivery/);
  assert.match(source, /Vui lòng để ý điện thoại/);
  assert.match(source, /@_order\.Phone/);
  assert.match(source, /xác nhận đơn hàng trước khi tiến hành sản xuất và giao hàng/);
});
