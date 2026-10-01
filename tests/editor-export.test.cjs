// Run with: node tests/editor-export.test.cjs
const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync('CaseShop.Web/wwwroot/js/editor.js', 'utf8');
function setup() {
    const canvases = [], images = [], links = [];
    let failCapture = false, failDownload = false;
    function canvas() {
        const calls = [];
        const ctx = new Proxy({}, { get: (o, k) => k in o ? o[k] : (...args) => calls.push([k, ...args]) });
        const c = { calls, width: 0, height: 0, parentElement: {}, getContext: () => ctx,
            addEventListener() {}, removeEventListener() {},
            getBoundingClientRect: () => ({width: 300, height: 500}),
            toDataURL(type) { assert.equal(type, 'image/png'); if (failCapture) throw Error(); return 'data:image/png;base64,test'; } };
        canvases.push(c); return c;
    }
    const main = canvas();
    class Image extends EventTarget {
        constructor() { super(); this.complete = false; this.naturalWidth = 0; this.naturalHeight = 0; this.listeners = 0; images.push(this); }
        set src(value) { if (this.crossOrigin !== undefined) assert.equal(this.crossOrigin, 'anonymous'); this.url = value; }
        addEventListener(...args) { this.listeners++; super.addEventListener(...args); }
        removeEventListener(...args) { this.listeners--; super.removeEventListener(...args); }
        finish(ok = true) { this.complete = true; this.naturalWidth = ok ? 100 : 0; this.naturalHeight = ok ? 200 : 0;
            this[ok ? 'onload' : 'onerror']?.(); this.dispatchEvent(new Event(ok ? 'load' : 'error')); }
    }
    const sandbox = {window: {}, Image, console,
        setTimeout: (fn, ms) => setTimeout(fn, Math.min(ms, 30)), clearTimeout,
        localStorage: {getItem:()=>null,setItem(){},removeItem(){},key:()=>null,get length(){return 0;}},
        ResizeObserver: class {observe() {} disconnect() {}},
        document: {getElementById: () => main, body: {appendChild() {}},
            createElement(tag) { if(tag === 'canvas') return canvas();
                const link = {click() { if(failDownload) throw Error(); this.clicked = true; }, remove() {this.removed = true;}};
                links.push(link); return link;
            }}
    };
    vm.runInNewContext(source.replace('init, setPhoneModel, render, reset, dispose, toLogical,', '_testState: _state, init, setPhoneModel, render, reset, dispose, toLogical,'), sandbox);
    const api = sandbox.window.CaseShopEditor;
    api.init('canvas', {canvasWidth:600,canvasHeight:1000,printAreaX:34,printAreaY:94,printAreaWidth:532,printAreaHeight:852});
    return {api, main, canvases, images, links, failCapture: () => failCapture = true, failDownload: () => failDownload = true};
}
(async () => {
    const t = setup(), {api} = t;
    assert(!t.main.calls.some(c => c[0] === 'fillText'));
    assert.equal((await api.exportDesign()).success, true);
    assert(!t.canvases.at(-1).calls.some(c => c[0] === 'fillText'));
    const backgroundSet = api.setBackground('background');
    t.images[0].finish();
    await backgroundSet;
    const stickerAdded = api.addSticker('database-id', 'sticker', 'Database');
    const uploadAdded = api.addUploadedSticker('upload', 'Uploaded', api.beginUpload());
    t.images.slice(1).forEach(i => i.finish());
    await Promise.all([stickerAdded, uploadAdded]);
    api.addText('Hello');
    const el = api._testState.elements[0];
    Object.assign(el, {x: 80, y: 170, width: 180, height: 240, rotation: 45});
    const before = JSON.stringify(api._testState);
    const pending = api.exportDesign();
    assert.equal((await api.exportDesign()).success, false);
    t.images.forEach(i => i.finish());
    assert.equal((await pending).success, true);
    assert.equal(JSON.stringify(api._testState), before);
    const exported = t.canvases.at(-1);
    assert.equal(exported.width, 600); assert.equal(exported.height, 1000);
    assert.deepEqual(exported.calls.filter(c => c[0] === 'drawImage').map(c => c[1].url), ['background', 'sticker', 'upload']);
    assert(exported.calls.some(c => c[0] === 'translate' && c[1] === 170 && c[2] === 290));
    assert(exported.calls.some(c => c[0] === 'drawImage' && c[1].url === 'sticker' && c[4] === 180 && c[5] === 240));
    assert(exported.calls.some(c => c[0] === 'rotate' && c[1] === Math.PI / 4));
    assert(!exported.calls.some(c => c[0] === 'strokeRect' || c[0] === 'setLineDash'));
    assert.equal(t.links.at(-1).download, 'caseshop-design.png');
    assert(t.links.at(-1).clicked && t.links.at(-1).removed);
    api.setPreviewMode(true);
    await api.exportDesign();
    assert.deepEqual(t.canvases.at(-1).calls, exported.calls);
    api.setPreviewMode(false);
    api.addText('Still editable');
    assert.equal(api._testState.elements.length, 4);
    const slow = setup();
    const slowBackground = slow.api.setBackground('slow');
    slow.images[0].finish();
    await slowBackground;
    slow.images[0].complete = false;
    slow.images[0].naturalWidth = 0;
    slow.images[0].naturalHeight = 0;
    const snapshotExport = slow.api.exportDesign();
    slow.api.addText('After click');
    assert((await snapshotExport).warning);
    assert.equal(slow.images[0].listeners, 0);
    assert(!slow.canvases.at(-1).calls.some(c => c[0] === 'fillText'));
    slow.images[0].finish(false);
    assert((await slow.api.exportDesign()).warning);
    const bad = setup(); bad.failCapture();
    assert.equal((await bad.api.exportDesign()).success, false);
    const blocked = setup(); blocked.failDownload();
    assert.equal((await blocked.api.exportDesign()).success, false);
    assert(blocked.links[0].removed);
    const disposed = setup(); const disposedBackground = disposed.api.setBackground('slow');
    disposed.images[0].finish(); await disposedBackground;
    disposed.images[0].complete = false; disposed.images[0].naturalWidth = 0; disposed.images[0].naturalHeight = 0;
    const leaving = disposed.api.exportDesign(); disposed.api.dispose();
    assert.equal((await leaving).success, false); assert.equal(disposed.links.length, 0);
    console.log('PASS: clean rendering, snapshot/state preservation, image wait/error/timeout cleanup, CORS setup, transforms/order, preview, logical size, duplicate guard, capture/download failure, disposal.');
})().catch(error => { console.error(error); process.exitCode = 1; });
