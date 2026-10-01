const fs = require('node:fs');
const avm = require('node:vm');
const assert = require('node:assert/strict');
const auditSource = fs.readFileSync('CaseShop.Web/wwwroot/js/editor.js','utf8');
assert.equal(auditSource.includes('aspect-ratio="1"'), false, 'crop selection must allow free rectangular resizing');
assert(auditSource.includes('Circular-arrow glyph matches the selection frame'), 'rotation handle must match the selection frame');
function auditSetup() {
 const handlers = new Map(); const captures = new Set(); const ctx = new Proxy({}, {get:(o,k)=>o[k]||(()=>{})});
 const config = {canvasWidth:600,canvasHeight:1000,printAreaX:34,printAreaY:94,printAreaWidth:532,printAreaHeight:852};
 const canvas = {parentElement:{style:{}},style:{},naturalWidth:240,naturalHeight:120,getContext:()=>ctx,getBoundingClientRect:()=>({left:0,top:0,width:300,height:500}),
 addEventListener:(k,v)=>handlers.set(k,v),removeEventListener:k=>handlers.delete(k),
 setPointerCapture:id=>captures.add(id),releasePointerCapture:id=>captures.delete(id)};
 class CropperMock { constructor(){ this.canvas={style:{},getBoundingClientRect:()=>({width:400,height:400})}; this.image={$image:{naturalWidth:240,naturalHeight:120},$rotate(){},$scale(){},$resetTransform(){},$center(){},$ready(callback){callback();}}; this.selection={style:{},width:0,height:0,$reset(){},$change(x,y,width,height){this.width=width;this.height=height;},$toCanvas:async(options)=>{this.output=options;return {toDataURL:()=> 'data:image/png;base64,bGli'};}}; } getCropperCanvas(){return this.canvas;} getCropperImage(){return this.image;} getCropperSelection(){return this.selection;} destroy(){this.destroyed=true;} }
 const s = {window:{addEventListener(){},Cropper:{default:CropperMock}},document:{getElementById:()=>canvas},ResizeObserver:class{observe(){} disconnect(){}},console,
 Image:class{constructor(){this.complete=true;this.naturalWidth=240;this.naturalHeight=120;}set src(v){}},setTimeout,clearTimeout,localStorage:{getItem:()=>null,setItem(){},removeItem(){},key:()=>null,get length(){return 0;}}};
 avm.runInNewContext(auditSource.replace('init, setPhoneModel, render, reset, dispose, toLogical,','_s:_state, _i:_interaction, _cache:_imageCache, _resize:_applyResize, _hit:_hitTestElement, _handles:_getHandles, _hitHandle:_hitTestHandles, init, setPhoneModel, render, reset, dispose, toLogical,'),s);
 const api=s.window.CaseShopEditor; api.init('canvas', config);
 const pointer=(type,x,y)=>handlers.get(type)?.({pointerId:1,clientX:x/2,clientY:y/2,preventDefault(){}});
 return {api,pointer,captures,handlers,config,canvas};
}
const a = auditSetup();
a.api.addText('audit');
a.pointer('pointerdown',300,320);
a.pointer('pointermove',330,350);
a.api.dispose();
assert.equal(a.handlers.size,0);
assert.equal(a.captures.size,0);
assert.equal(a.api._i.mode,null);
a.api.init('canvas', a.config);
assert.equal(a.api._s.elements.length,0);
assert.equal(a.api.isPreviewMode(),false);
assert.equal(a.api._s.dirty,false);
const count=a.handlers.size;
a.api.init('canvas', a.config);
assert.equal(a.handlers.size,count);
a.api.addText('remove');
a.pointer('pointerdown',300,320);
a.api.removeSelected();
assert.equal(a.api._i.mode,null);
assert.equal(a.captures.size,0);
assert.equal(a.api.getSelectedElementId(),null);
a.api.addText('delete handle');
const actionHandles = a.api._handles(a.api._s.elements[0]);
const deleteTarget = actionHandles.delete;
assert.ok(Math.abs(Math.hypot(
  deleteTarget.x - actionHandles.rot.x,
  deleteTarget.y - actionHandles.rot.y
) - 28) < 0.001, 'delete handle should sit beside rotate handle');
a.pointer('pointermove', deleteTarget.x, deleteTarget.y);
assert.equal(a.canvas.style.cursor, 'pointer');
assert.equal(a.api._hitHandle(a.api._s.elements[0], deleteTarget.x, deleteTarget.y), 'delete');
a.pointer('pointerdown', deleteTarget.x, deleteTarget.y);
assert.equal(a.api._s.elements.length, 0);
assert.equal(a.api._i.mode, null);
assert.equal(a.api.getSelectedElementId(), null);
a.api.command('undo');
assert.equal(a.api._s.elements.length, 1);
a.api.selectElement('missing');
assert.equal(a.api.getSelectedElementId(),null);
a.api.addText('preview');
a.pointer('pointerdown',320,340);
const geometry=JSON.stringify(a.api._s.elements);
a.pointer('pointermove',350,370);
a.api.setPreviewMode(true);
assert.equal(JSON.stringify(a.api._s.elements),geometry);
assert.equal(a.captures.size,0);
a.api.dispose();
a.api.init('canvas', a.config);
assert.equal(a.api.isPreviewMode(),false);
a.api.addText('reset');
a.pointer('pointerdown',300,320);
a.api.reset();
assert.equal(a.captures.size,0);
assert.equal(a.api._i.mode,null);
assert.equal(a.api._s.elements.length,0);
assert.equal(a.api._s.dirty,false);
a.api.addText('lower');
a.api.addText('upper');
a.api.selectElement(null);
a.pointer('pointerdown',320,360);
assert.equal(a.api.getSelectedElementId(),a.api._s.elements[1].id);
a.pointer('pointerup',320,360);
assert.equal(a.api._hit({x:100,y:100,width:200,height:40,rotation:90},200,190),true);
assert.equal(a.api._hit({x:100,y:100,width:200,height:40,rotation:90},280,120),false);
console.log('PASS: lifecycle, capture cleanup, remove/reset, preview cancellation geometry, valid selection, topmost and rotated hit testing.');

function assertContained(el) {
    const eps = 1e-7;
    assert(el.width >= 20-eps && el.height >= 20-eps);
    assert(el.x >= 34-eps && el.y >= 94-eps);
    assert(el.x+el.width <= 566+eps && el.y+el.height <= 946+eps);
    const angle=el.rotation*Math.PI/180, c=Math.abs(Math.cos(angle)), s=Math.abs(Math.sin(angle));
    const halfW=(c*el.width+s*el.height)/2, halfH=(s*el.width+c*el.height)/2;
    const cx=el.x+el.width/2, cy=el.y+el.height/2;
    assert(cx-halfW >= 34-eps && cx+halfW <= 566+eps);
    assert(cy-halfH >= 94-eps && cy+halfH <= 946+eps);
}
for (const type of ['text','sticker']) {
    for (const rotation of [0,30,45,90,135,270]) {
        for (const handle of ['tl','tr','bl','br']) {
            for (const [dx,dy] of [[10,15],[1000,1000],[-1000,-1000],[1000,-1000],[-1000,1000]]) {
                const t=auditSetup();
                const el={type,x:150,y:200,width:160,height:80,rotation};
                t.api._resize(el,{...el},handle,dx,dy);
                assertContained(el);
                if(type==='sticker') assert(Math.abs(el.width/el.height-2)<1e-9);
            }
        }
    }
}
const resize=auditSetup();
const inside={type:'text',x:200,y:294,width:200,height:60,rotation:0};
resize.api._resize(inside,{...inside},'br',10,20);
assert.equal(inside.width,210); assert.equal(inside.height,80);
resize.api._resize(inside,{...inside},'br',1000,1000);
assert.equal(inside.x+inside.width,566); assert.equal(inside.y+inside.height,946);
for (const dirty of [false,true]) {
    for (const cancellation of ['pointercancel','preview']) {
        for (const [mode,x,y] of [['move',300,350],['resize',390,384],['rotate',300,306]]) {
            const t=auditSetup(); t.api.addText('dirty test'); t.api._s.dirty=dirty;
            const before=JSON.stringify(t.api._s.elements);
            t.pointer('pointerdown',x,y);
            assert.equal(t.api._i.mode,mode);
            t.pointer('pointermove',x+30,y+40);
            assert.equal(t.api._s.dirty,true);
            if(cancellation==='preview') t.api.setPreviewMode(true);
            else t.pointer('pointercancel',x+30,y+40);
            assert.equal(t.api._s.dirty,dirty);
            assert.equal(JSON.stringify(t.api._s.elements),before);
            assert.equal(t.captures.size,0);
        }
    }
}
for (const [x,y] of [[300,350],[390,384],[300,306]]) {
    const t=auditSetup(); t.api.addText('commit'); t.api._s.dirty=false;
    t.pointer('pointerdown',x,y); t.pointer('pointermove',x+30,y+40); t.pointer('pointerup',x+30,y+40);
    assert.equal(t.api._s.dirty,true);
}
const otherEdit=auditSetup(); otherEdit.api.addText('first'); otherEdit.api._s.dirty=false;
otherEdit.pointer('pointerdown',300,350); otherEdit.pointer('pointermove',330,390);
otherEdit.api.addText('another edit');
otherEdit.pointer('pointercancel',330,350);
assert.equal(otherEdit.api._s.dirty,true);
(async()=>{
    const ratioTest=auditSetup();
    await ratioTest.api.addSticker('ratio','ratio-image','landscape');
    assert.equal(ratioTest.api._s.elements[0].width / ratioTest.api._s.elements[0].height, 2);
    const cropTarget = ratioTest.api.getSelectedStickerForCrop();
    assert.equal(cropTarget.imageUrl, 'ratio-image');
    assert.equal(await ratioTest.api.applyCroppedSticker(cropTarget.id, 'data:image/png;base64,Y3JvcA=='), true);
    assert.equal(ratioTest.api._s.elements[0].width / ratioTest.api._s.elements[0].height, 2);
    assert.equal(ratioTest.api._s.elements[0].data.imageUrl, 'data:image/png;base64,Y3JvcA==');
    ratioTest.api.command('undo');
    assert.equal(ratioTest.api._s.elements[0].data.imageUrl, 'ratio-image');
    assert.equal(ratioTest.api.initImageCropper('image','host','dialog'), true);
    assert.equal(ratioTest.api.transformImageCropper('rotateRight'), true);
    assert.equal(await ratioTest.api.exportImageCropper(), 'data:image/png;base64,bGli');
    ratioTest.api.destroyImageCropper();
    const backgroundTest = auditSetup();
    await backgroundTest.api.setBackground('background-image');
    const background = backgroundTest.api._s.background;
    assert.equal(background.type, 'background');
    assert(background.width >= 532 && background.height >= 852);
    assert.equal(backgroundTest.api.getState().elements.at(-1).id, '__background__');
    backgroundTest.pointer('pointerdown', 300, 500);
    backgroundTest.pointer('pointermove', 500, 500);
    backgroundTest.pointer('pointerup', 500, 500);
    assert(background.x <= 34 && background.x + background.width >= 566);
    assert(background.y <= 94 && background.y + background.height >= 946);
    for(const invalidate of ['reset','preview','dispose','remount','preview-exit','none']) {
        const t=auditSetup(), token=t.api.beginUpload();
        // Model an upload completing asynchronously after the context changes.
        const complete=Promise.resolve().then(()=>t.api.addUploadedSticker('valid-image','upload',token));
        if(invalidate==='reset') t.api.reset();
        if(invalidate==='preview' || invalidate==='preview-exit') t.api.setPreviewMode(true);
        if(invalidate==='preview-exit') t.api.setPreviewMode(false);
        if(invalidate==='dispose' || invalidate==='remount') t.api.dispose();
        if(invalidate==='remount') t.api.init('canvas', t.config);
        assert.equal(await complete,invalidate==='none');
        assert.equal(t.api._s.elements.length,invalidate==='none'?1:0);
        if(invalidate==='none') {
            assert.equal(t.api._s.elements[0].data.stickerId,null);
            assert.equal(await t.api.addUploadedSticker('valid-image','duplicate',token),false);
            assert.equal(t.api._s.elements.length,1);
        }
    }
    console.log('PASS: upload/crop history, reset/preview/dispose/remount races, exactly-once completion, 240 resize bounds/ratio cases, clean/dirty cancellation and successful edits.');
})().catch(error=>{console.error(error);process.exitCode=1;});
