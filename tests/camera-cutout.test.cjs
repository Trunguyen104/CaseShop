const fs = require('node:fs');
const avm = require('node:vm');
const assert = require('node:assert/strict');

const source = fs.readFileSync('CaseShop.Web/wwwroot/js/editor.js', 'utf8');

function setupTest(config) {
    const handlers = new Map();
    const calls = [];
    const ctx = new Proxy({}, {
        get: (o, k) => {
            return (...args) => {
                calls.push({ method: k, args });
            };
        }
    });

    const canvas = {
        parentElement: {},
        getContext: () => ctx,
        getBoundingClientRect: () => ({ left: 0, top: 0, width: 350, height: 700 }),
        addEventListener: (k, v) => handlers.set(k, v),
        removeEventListener: k => handlers.delete(k),
        toDataURL: () => 'data:image/png;base64,fakePngData'
    };

    const s = {
        window: {},
        document: {
            getElementById: () => canvas,
            createElement: (tag) => {
                if (tag === 'canvas') return { ...canvas, width: 350, height: 700 };
                return { click: () => {}, remove: () => {} };
            },
            body: { appendChild: () => {} }
        },
        ResizeObserver: class { observe() {} disconnect() {} },
        console,
        Image: class { set src(v) {} },
        setTimeout,
        clearTimeout
    };

    avm.runInNewContext(
        source.replace(
            'init, render, reset, dispose, toLogical,',
            '_s:_state, _p:_physical, init, render, reset, dispose, toLogical,'
        ),
        s
    );

    const api = s.window.CaseShopEditor;
    const ok = api.init('canvas', config);
    return { api, calls, ok };
}

// 1. Test geometric camera cutout config initialization
const config = {
    canvasWidth: 350,
    canvasHeight: 700,
    cornerRadius: 40,
    cameraCutoutX: 20,
    cameraCutoutY: 25,
    cameraCutoutWidth: 100,
    cameraCutoutHeight: 110,
    cameraCutoutRadius: 22,
    safeAreaX: 15,
    safeAreaY: 15,
    safeAreaWidth: 320,
    safeAreaHeight: 670,
    printAreaX: 0,
    printAreaY: 0,
    printAreaWidth: 350,
    printAreaHeight: 700
};

const test1 = setupTest(config);
assert.equal(test1.ok, true, 'Editor should initialize successfully');
assert.equal(test1.api._p.cameraCutoutX, 20);
assert.equal(test1.api._p.cameraCutoutY, 25);
assert.equal(test1.api._p.cameraCutoutWidth, 100);
assert.equal(test1.api._p.cameraCutoutHeight, 110);
assert.equal(test1.api._p.cameraCutoutRadius, 22);

// 2. Test rendering with camera cutout
test1.api.render();
assert(test1.calls.length > 0, 'Rendering should generate canvas draw calls');

// 3. Test exportDesign completes cleanly
test1.api.exportDesign().then(res => {
    assert.equal(res.success, true, 'Export should succeed');
    console.log('PASS: Camera cutout initialization, rendering and clean export verified.');
}).catch(err => {
    console.error('FAIL:', err);
    process.exit(1);
});
