const fs = require('node:fs');
const path = require('node:path');

const files = [
  ['../node_modules/cropperjs/dist/cropper.min.js', '../wwwroot/vendor/cropperjs/cropper.min.js'],
  ['../node_modules/qrcode-generator/dist/qrcode.js', '../wwwroot/vendor/qrcode/qrcode.js'],
  ['../node_modules/gsap/dist/gsap.min.js', '../wwwroot/vendor/gsap/gsap.min.js'],
  ['../node_modules/gsap/dist/ScrollTrigger.min.js', '../wwwroot/vendor/gsap/ScrollTrigger.min.js'],
];

for (const [sourcePath, destinationPath] of files) {
  const source = path.resolve(__dirname, sourcePath);
  const destination = path.resolve(__dirname, destinationPath);
  fs.mkdirSync(path.dirname(destination), { recursive: true });
  fs.copyFileSync(source, destination);
}
