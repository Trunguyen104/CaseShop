const fs = require('node:fs');
const path = require('node:path');

const source = path.resolve(__dirname, '../node_modules/cropperjs/dist/cropper.min.js');
const destination = path.resolve(__dirname, '../wwwroot/vendor/cropperjs/cropper.min.js');

fs.mkdirSync(path.dirname(destination), { recursive: true });
fs.copyFileSync(source, destination);
