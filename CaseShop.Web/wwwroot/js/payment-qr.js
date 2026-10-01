export function render(elementId, payload) {
  const element = document.getElementById(elementId);
  if (!element || !window.qrcode || !payload) return;
  const qr = qrcode(0, 'M');
  qr.addData(payload);
  qr.make();
  element.innerHTML = qr.createSvgTag({ cellSize: 6, margin: 4, scalable: true });
  const svg = element.querySelector('svg');
  if (svg) svg.setAttribute('aria-label', 'Mã QR thanh toán PayOS');
}
