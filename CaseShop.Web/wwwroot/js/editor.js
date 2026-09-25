/**
 * CaseShop Custom Editor — JavaScript Module
 *
 * Coordinate system:
 *   Logical canvas: 600 x 1000 (fixed, represents the printable phone-case area)
 *   Display size:   driven by CSS (width: 100%; height: auto on the <canvas> element)
 *
 * Public API (Blazor JS interop):
 *   CaseShopEditor.init(canvasId)              -> bool
 *   CaseShopEditor.setBackground(url)          -> void
 *   CaseShopEditor.clearBackground()           -> void
 *   CaseShopEditor.addSticker(id, url, name)   -> void
 *   CaseShopEditor.addText(text)               -> bool
 *   CaseShopEditor.removeSelected()            -> void
 *   CaseShopEditor.selectElement(id)           -> void
 *   CaseShopEditor.getSelectedElementId()      -> string|null
 *   CaseShopEditor.reset()                     -> void
 *   CaseShopEditor.dispose()                   -> void
 *   CaseShopEditor.toLogical(x, y)             -> {x, y}
 *   CaseShopEditor.render()                    -> void
 *   CaseShopEditor.exportDesign()           -> Promise<{success,error?}>
 */

window.CaseShopEditor = (() => {
    'use strict';

    // -------------------------------------------------------------------------
    // Variables — logical coordinate space (initialized from PhoneModel)
    // -------------------------------------------------------------------------
    let LOGICAL_WIDTH  = 350;
    let LOGICAL_HEIGHT = 700;

    let EDIT_X  = 0;
    let EDIT_Y  = 0;
    let EDIT_W  = 350;
    let EDIT_H  = 700;
    let EDIT_R  = 40;
    let CASE_R  = 40;  // physical case corner radius (from PhoneModel)

    const DEFAULT_STICKER_W = 140;
    const DEFAULT_STICKER_H = 140;
    const DEFAULT_TEXT_W    = 180;
    const DEFAULT_TEXT_H    = 50;

    const HANDLE_SIZE    = 14;
    const ROT_HANDLE_R   = 8;
    const ROT_HANDLE_OFF = 28;

    const COLOR_BG              = 'rgba(0, 0, 0, 0)';
    const COLOR_CASE_STROKE     = '#cbd5e1';
    const COLOR_EDITABLE_FILL   = '#ffffff';
    const COLOR_SELECTION       = '#4f81f0';
    const COLOR_HANDLE_FILL     = '#ffffff';
    const COLOR_HANDLE_STROKE   = '#4f81f0';
    const COLOR_ROT_HANDLE      = '#4f81f0';
    const COLOR_PLACEHOLDER_BG  = 'rgba(180,180,200,0.35)';
    const COLOR_PLACEHOLDER_FG  = 'rgba(120,120,150,0.7)';

    const MIN_W = 20;
    const MIN_H = 20;

    // -------------------------------------------------------------------------
    // Module-level variables
    // -------------------------------------------------------------------------
    let _canvas         = null;
    let _ctx            = null;
    let _resizeObserver = null;
    let _initialized    = false;
    let _exportInProgress = false;
    let _uploadGeneration = 0;
    let _designRevision = 0;
    let _observer = null;
    let _lastNotification = '';
    let _history = [];
    let _historyIndex = -1;
    let _restoring = false;
    let _showGrid = false;

    const _imageCache = new Map();

    const _state = {
        background:        null,
        elements:          [],
        selectedElementId: null,
        dirty:             false,
        _nextZIndex:       1,
        _stickerOffset:    0,
        _textOffset:       0,
        previewMode:       false,
        phoneModelConfig:  null,
    };

    // Physical configuration — separate from user design state.
    // Never serialized as design elements, never reset by user reset.
    const _physical = {
        maskImageUrl:       null,
        maskSession:        0,       // incremented on model change to abort stale loads
        safeAreaX:          0,
        safeAreaY:          0,
        safeAreaWidth:      0,
        safeAreaHeight:     0,
        printAreaX:         0,
        printAreaY:         0,
        printAreaWidth:     0,
        printAreaHeight:    0,
        maskImage:          null,    // HTMLImageElement | 'error' | null (loading)
        overlayImageUrl:    null,
        overlaySession:     0,
        overlayImage:       null,    // HTMLImageElement | 'error' | null (loading)
        cornerRadius:       40,      // logical px, from PhoneModel.CornerRadius
        cameraCutoutX:      0,
        cameraCutoutY:      0,
        cameraCutoutWidth:  0,
        cameraCutoutHeight: 0,
        cameraCutoutRadius: 0,
    };

    const _interaction = {
        mode:      null,
        pointerId: null,
        elementId: null,
        handle:    null,
        startLX:   0,
        startLY:   0,
        startEl:   null,
        startDirty: false,
        startRevision: 0,
    };

    // -------------------------------------------------------------------------
    // Configuration helper
    // -------------------------------------------------------------------------

    function _applyPhoneModelConfig(config) {
        if (!config) {
            LOGICAL_WIDTH  = 350;
            LOGICAL_HEIGHT = 700;
            CASE_R         = 40;
            _physical.maskImageUrl       = null;
            _physical.overlayImageUrl    = null;
            _physical.cornerRadius       = 40;
            _physical.safeAreaX          = 14;
            _physical.safeAreaY          = 14;
            _physical.safeAreaWidth      = 322;
            _physical.safeAreaHeight     = 672;
            _physical.printAreaX         = 0;
            _physical.printAreaY         = 0;
            _physical.printAreaWidth     = 350;
            _physical.printAreaHeight    = 700;
            _physical.cameraCutoutX      = 18;
            _physical.cameraCutoutY      = 18;
            _physical.cameraCutoutWidth  = 110;
            _physical.cameraCutoutHeight = 114;
            _physical.cameraCutoutRadius = 28;
            _physical.slug               = 'iphone-15-pro';
        } else {
            _state.phoneModelConfig = config;
            const cWidth = Number(config.canvasWidth ?? config.CanvasWidth);
            const cHeight = Number(config.canvasHeight ?? config.CanvasHeight);
            LOGICAL_WIDTH  = (cWidth > 0) ? cWidth : 350;
            LOGICAL_HEIGHT = (cHeight > 0) ? cHeight : 700;

            const cRadius = config.cornerRadius !== undefined ? config.cornerRadius : config.CornerRadius;
            _physical.cornerRadius    = cRadius !== undefined && cRadius !== null ? Number(cRadius) : 40;
            CASE_R                    = _physical.cornerRadius;

            _physical.maskImageUrl    = config.maskImageUrl ?? config.MaskImageUrl ?? null;
            _physical.overlayImageUrl = config.overlayImageUrl ?? config.OverlayImageUrl ?? null;
            _physical.slug            = String(config.slug ?? config.Slug ?? '').toLowerCase();

            _physical.safeAreaX      = Number(config.safeAreaX ?? config.SafeAreaX)      || 0;
            _physical.safeAreaY      = Number(config.safeAreaY ?? config.SafeAreaY)      || 0;
            _physical.safeAreaWidth  = Number(config.safeAreaWidth ?? config.SafeAreaWidth)  || 0;
            _physical.safeAreaHeight = Number(config.safeAreaHeight ?? config.SafeAreaHeight) || 0;

            _physical.printAreaX     = Number(config.printAreaX ?? config.PrintAreaX)     || 0;
            _physical.printAreaY     = Number(config.printAreaY ?? config.PrintAreaY)     || 0;
            _physical.printAreaWidth = Number(config.printAreaWidth ?? config.PrintAreaWidth) || 0;
            _physical.printAreaHeight= Number(config.printAreaHeight ?? config.PrintAreaHeight)|| 0;

            let camX = Number(config.cameraCutoutX ?? config.CameraCutoutX) || 0;
            let camY = Number(config.cameraCutoutY ?? config.CameraCutoutY) || 0;
            let camW = Number(config.cameraCutoutWidth ?? config.CameraCutoutWidth) || 0;
            let camH = Number(config.cameraCutoutHeight ?? config.CameraCutoutHeight) || 0;
            let camR = Number(config.cameraCutoutRadius ?? config.CameraCutoutRadius) || 0;

            // Tự động phân bổ thông số dự phòng chuẩn theo slug nếu chưa có
            if (camW <= 0 || camH <= 0) {
                const s = _physical.slug;
                if (s.includes('pixel')) {
                    camX = 10; camY = 72; camW = LOGICAL_WIDTH - 20; camH = 68; camR = 16;
                } else if (s.includes('ultra') && s.includes('xiaomi')) {
                    camW = 210; camH = 210; camX = (LOGICAL_WIDTH - camW) / 2; camY = 45; camR = 105;
                } else if (s.includes('galaxy') || s.includes('s24') || s.includes('s25')) {
                    camX = 20; camY = 22; camW = 60; camH = 155; camR = 20;
                } else if (s.includes('16') && !s.includes('pro')) {
                    camX = 18; camY = 18; camW = 72; camH = 130; camR = 36;
                } else if (s.includes('15') && !s.includes('pro')) {
                    camX = 18; camY = 18; camW = 96; camH = 96; camR = 26;
                } else {
                    camX = 18; camY = 18; camW = 110; camH = 114; camR = 28;
                }
            }

            _physical.cameraCutoutX      = camX;
            _physical.cameraCutoutY      = camY;
            _physical.cameraCutoutWidth  = camW;
            _physical.cameraCutoutHeight = camH;
            _physical.cameraCutoutRadius = camR;
        }

        if (_physical.printAreaWidth > 0 && _physical.printAreaHeight > 0) {
            EDIT_X = _physical.printAreaX;
            EDIT_Y = _physical.printAreaY;
            EDIT_W = _physical.printAreaWidth;
            EDIT_H = _physical.printAreaHeight;
        } else {
            EDIT_X = 0;
            EDIT_Y = 0;
            EDIT_W = LOGICAL_WIDTH;
            EDIT_H = LOGICAL_HEIGHT;
        }
        EDIT_R = CASE_R;

        if (_canvas) {
            _canvas.width  = LOGICAL_WIDTH;
            _canvas.height = LOGICAL_HEIGHT;
            if (_canvas.parentElement) {
                _canvas.parentElement.style.aspectRatio = `${LOGICAL_WIDTH} / ${LOGICAL_HEIGHT}`;
            }
        }
    }

    // -------------------------------------------------------------------------
    // Initialisation & Model Switching
    // -------------------------------------------------------------------------

    function init(canvasId, config) {
        _canvas = document.getElementById(canvasId);
        if (!_canvas) {
            console.error('[CaseShopEditor] Canvas element not found:', canvasId);
            return { success: false, draftRestored: false, elementCount: 0, lastModelSlug: null };
        }

        _ctx = _canvas.getContext('2d');
        if (!_ctx) {
            console.error('[CaseShopEditor] Failed to get 2D context.');
            return { success: false, draftRestored: false, elementCount: 0, lastModelSlug: null };
        }

        if (_initialized) {
            setPhoneModel(config);
            return {
                success: true,
                draftRestored: false,
                elementCount: _state.elements.length,
                lastModelSlug: _state.phoneModelConfig?.slug || null
            };
        }

        _applyPhoneModelConfig(config);

        _setupResizeObserver();
        _attachPointerListeners();
        _canvas.addEventListener('touchstart', _preventDefaultTouch, { passive: false });

        window.addEventListener('beforeunload', () => {
            saveDraft();
        });

        _initialized = true;
        _loadMaskImage();     // async; does not block init
        _loadOverlayImage();  // async; does not block init

        // Attempt to restore any temporary design draft saved in browser
        const restored = loadDraft(config?.slug);

        render();

        return {
            success: true,
            draftRestored: !!restored,
            elementCount: _state.elements.length,
            lastModelSlug: (restored && restored.modelSlug) || _state.phoneModelConfig?.slug || null
        };
    }

    function setPhoneModel(config) {
        if (!_canvas) {
            _canvas = document.getElementById('editor-canvas');
            if (_canvas) _ctx = _canvas.getContext('2d');
        }

        // Save current phone model's draft before switching
        if (_state.phoneModelConfig?.slug) {
            saveDraft(_state.phoneModelConfig.slug);
        }

        _applyPhoneModelConfig(config);
        _loadMaskImage();
        _loadOverlayImage();

        // Clear transient in-memory elements then load draft for the target model if exists
        _state.background = null;
        _state.elements = [];
        _state.selectedElementId = null;
        _state.dirty = false;
        _state._nextZIndex = 1;

        const restored = loadDraft(config?.slug);
        if (!restored) {
            render();
        }
    }

    function _preventDefaultTouch(e) {
        if (_interaction.mode !== null) e.preventDefault();
    }

    // -------------------------------------------------------------------------
    // Resize observer
    // -------------------------------------------------------------------------

    function _setupResizeObserver() {
        if (!_canvas.parentElement) return;
        _resizeObserver = new ResizeObserver(() => { render(); });
        _resizeObserver.observe(_canvas.parentElement);
    }

    // -------------------------------------------------------------------------
    // Pointer listeners
    // -------------------------------------------------------------------------

    function _attachPointerListeners() {
        _canvas.addEventListener('pointerdown',   _onPointerDown);
        _canvas.addEventListener('pointermove',   _onPointerMove);
        _canvas.addEventListener('pointerup',     _onPointerUp);
        _canvas.addEventListener('pointercancel', _onPointerCancel);
    }

    function _detachPointerListeners() {
        if (!_canvas) return;
        _canvas.removeEventListener('pointerdown',   _onPointerDown);
        _canvas.removeEventListener('pointermove',   _onPointerMove);
        _canvas.removeEventListener('pointerup',     _onPointerUp);
        _canvas.removeEventListener('pointercancel', _onPointerCancel);
        _canvas.removeEventListener('touchstart',    _preventDefaultTouch);
    }

    function _eventToLogical(e) {
        const rect = _canvas.getBoundingClientRect();
        return {
            x: (e.clientX - rect.left) * (LOGICAL_WIDTH  / rect.width),
            y: (e.clientY - rect.top)  * (LOGICAL_HEIGHT / rect.height),
        };
    }

    // ---- pointerdown ----

    function _onPointerDown(e) {
        if (_interaction.mode !== null) return;
        if (_state.previewMode) return; // No interaction in preview mode

        const lp  = _eventToLogical(e);
        const sel = _state.selectedElementId
            ? _state.elements.find(el => el.id === _state.selectedElementId)
            : null;

        // 1. Check handles on selected element first
        if (sel && !sel.locked && !sel.hidden) {
            const hit = _hitTestHandles(sel, lp.x, lp.y);
            if (hit) {
                _startInteraction(e, hit, sel.id, lp);
                return;
            }
        }

        // 2. Hit-test element bodies, topmost first
        const sorted = [..._state.elements].sort((a, b) => b.zIndex - a.zIndex);
        for (const el of sorted) {
            if (el.hidden || el.locked) continue;
            if (_hitTestElement(el, lp.x, lp.y)) {
                _state.selectedElementId = el.id;
                _startInteraction(e, 'move', el.id, lp);
                render();
                return;
            }
        }

        // 3. Click on empty space — deselect
        _state.selectedElementId = null;
        render();
    }

    function _startInteraction(e, handle, elementId, lp) {
        const el = _state.elements.find(el => el.id === elementId);
        if (!el) return;

        _interaction.mode      = handle === 'move' ? 'move' : handle === 'rot' ? 'rotate' : 'resize';
        _interaction.handle    = (_interaction.mode === 'resize') ? handle : null;
        _interaction.pointerId = e.pointerId;
        _interaction.elementId = elementId;
        _interaction.startLX   = lp.x;
        _interaction.startLY   = lp.y;
        _interaction.startEl   = { x: el.x, y: el.y, width: el.width, height: el.height, rotation: el.rotation };
        _interaction.startDirty = _state.dirty;
        _interaction.startRevision = _designRevision;

        try { _canvas.setPointerCapture(e.pointerId); } catch (_) {}
        render();
    }

    // ---- pointermove ----

    function _onPointerMove(e) {
        if (_interaction.mode === null || e.pointerId !== _interaction.pointerId) return;
        e.preventDefault();

        const lp   = _eventToLogical(e);
        const dx   = lp.x - _interaction.startLX;
        const dy   = lp.y - _interaction.startLY;
        const el   = _state.elements.find(el => el.id === _interaction.elementId);
        if (!el) return;

        const snap = _interaction.startEl;

        if (_interaction.mode === 'move') {
            el.x = _clamp(snap.x + dx, EDIT_X, EDIT_X + EDIT_W - el.width);
            el.y = _clamp(snap.y + dy, EDIT_Y, EDIT_Y + EDIT_H - el.height);
        } else if (_interaction.mode === 'resize') {
            _applyResize(el, snap, _interaction.handle, dx, dy);
        } else if (_interaction.mode === 'rotate') {
            const cx    = snap.x + snap.width  / 2;
            const cy    = snap.y + snap.height / 2;
            const angle = Math.atan2(lp.y - cy, lp.x - cx) * 180 / Math.PI + 90;
            el.rotation = ((angle % 360) + 360) % 360;
        }

        _state.dirty = true;
        render();
    }

    // ---- pointerup / pointercancel ----

    function _onPointerUp(e) {
        if (e.pointerId !== _interaction.pointerId) return;
        _endInteraction(e);
    }

    function _onPointerCancel(e) {
        if (e.pointerId !== _interaction.pointerId) return;
        _restoreInteraction();
        _endInteraction(e);
    }

    function _restoreInteraction() {
        if (_interaction.startEl && _interaction.elementId) {
            const el = _state.elements.find(el => el.id === _interaction.elementId);
            if (el) Object.assign(el, _interaction.startEl);
            // Preserve other edits (for example a completed upload) made during the drag.
            _state.dirty = _interaction.startDirty || _designRevision !== _interaction.startRevision;
        }
    }

    function _endInteraction(e) {
        _clearInteraction();
        render();
    }

    function _clearInteraction() {
        try {
            if (_canvas && _interaction.pointerId !== null)
                _canvas.releasePointerCapture(_interaction.pointerId);
        } catch (_) {}
        _interaction.mode      = null;
        _interaction.pointerId = null;
        _interaction.elementId = null;
        _interaction.handle    = null;
        _interaction.startEl   = null;
        _interaction.startDirty = false;
        _interaction.startRevision = 0;
    }

    // ---- Resize ----

    function _applyResize(el, snap, handle, dx, dy) {
        const isSticker = el.type === 'sticker';
        const ar = snap.height > 0 ? snap.width / snap.height : 1;
        let newX = snap.x, newY = snap.y, newW = snap.width, newH = snap.height;

        if (isSticker) {
            let delta = 0;
            switch (handle) {
                case 'tl': delta = Math.min(-dx, -dy * ar); newW = Math.max(MIN_W, snap.width + delta); newH = newW / ar; newX = snap.x + snap.width - newW; newY = snap.y + snap.height - newH; break;
                case 'tr': delta = Math.max( dx, -dy * ar); newW = Math.max(MIN_W, snap.width + delta); newH = newW / ar; newY = snap.y + snap.height - newH; break;
                case 'bl': delta = Math.min(-dx,  dy * ar); newW = Math.max(MIN_W, snap.width + delta); newH = newW / ar; newX = snap.x + snap.width - newW; break;
                case 'br': delta = Math.max( dx,  dy * ar); newW = Math.max(MIN_W, snap.width + delta); newH = newW / ar; break;
            }
        } else {
            switch (handle) {
                case 'tl': newW = Math.max(MIN_W, snap.width - dx); newH = Math.max(MIN_H, snap.height - dy); newX = snap.x + snap.width - newW; newY = snap.y + snap.height - newH; break;
                case 'tr': newW = Math.max(MIN_W, snap.width + dx); newH = Math.max(MIN_H, snap.height - dy); newY = snap.y + snap.height - newH; break;
                case 'bl': newW = Math.max(MIN_W, snap.width - dx); newH = Math.max(MIN_H, snap.height + dy); newX = snap.x + snap.width - newW; break;
                case 'br': newW = Math.max(MIN_W, snap.width + dx); newH = Math.max(MIN_H, snap.height + dy); break;
            }
        }

        // Keep the opposite corner anchored, using the same editable-area constants as movement.
        const leftHandle = handle === 'tl' || handle === 'bl';
        const topHandle = handle === 'tl' || handle === 'tr';
        const anchorX = leftHandle ? snap.x + snap.width : snap.x;
        const anchorY = topHandle ? snap.y + snap.height : snap.y;
        const maxW = leftHandle ? anchorX - EDIT_X : EDIT_X + EDIT_W - anchorX;
        const maxH = topHandle ? anchorY - EDIT_Y : EDIT_Y + EDIT_H - anchorY;
        const minW = isSticker ? Math.max(MIN_W, MIN_H * ar) : MIN_W;
        const minH = isSticker ? minW / ar : MIN_H;
        if (isSticker) {
            newW = _clamp(newW, minW, Math.max(minW, Math.min(maxW, maxH * ar)));
            newH = newW / ar;
        } else {
            newW = _clamp(newW, minW, Math.max(minW, maxW));
            newH = _clamp(newH, minH, Math.max(minH, maxH));
        }

        // Conservative rotated bounding box; retain the existing approximate drag geometry.
        const rad = el.rotation * Math.PI / 180;
        const c = Math.abs(Math.cos(rad)), s = Math.abs(Math.sin(rad));
        const bw = c * newW + s * newH, bh = s * newW + c * newH;
        const minBW = c * minW + s * minH, minBH = s * minW + c * minH;
        const fit = Math.min(1,
            bw > minBW ? (EDIT_W - minBW) / (bw - minBW) : 1,
            bh > minBH ? (EDIT_H - minBH) / (bh - minBH) : 1);
        newW = minW + (newW - minW) * Math.max(0, fit);
        newH = minH + (newH - minH) * Math.max(0, fit);
        newX = leftHandle ? anchorX - newW : anchorX;
        newY = topHandle ? anchorY - newH : anchorY;
        const halfW = Math.max(newW, c * newW + s * newH) / 2;
        const halfH = Math.max(newH, s * newW + c * newH) / 2;
        const cx = _clamp(newX + newW / 2, EDIT_X + halfW, EDIT_X + EDIT_W - halfW);
        const cy = _clamp(newY + newH / 2, EDIT_Y + halfH, EDIT_Y + EDIT_H - halfH);
        el.x = cx - newW / 2; el.y = cy - newH / 2;
        el.width = newW; el.height = newH;
    }

    // -------------------------------------------------------------------------
    // Hit testing
    // -------------------------------------------------------------------------

    /** Rotation-aware AABB hit test against element local space. */
    function _hitTestElement(el, lx, ly) {
        const cx  = el.x + el.width  / 2;
        const cy  = el.y + el.height / 2;
        const rad = -(el.rotation * Math.PI / 180);
        const cos = Math.cos(rad);
        const sin = Math.sin(rad);
        const rx  = lx - cx;
        const ry  = ly - cy;
        const localX = rx * cos - ry * sin;
        const localY = rx * sin + ry * cos;
        return Math.abs(localX) <= el.width / 2 && Math.abs(localY) <= el.height / 2;
    }

    /** Returns handle id or null. */
    function _hitTestHandles(el, lx, ly) {
        if (!el) return null;
        const handles   = _getHandles(el);
        const hitRadius = HANDLE_SIZE * 1.5;

        const rh  = handles.rot;
        const rdx = lx - rh.x, rdy = ly - rh.y;
        if (Math.sqrt(rdx * rdx + rdy * rdy) <= ROT_HANDLE_R * 1.8) return 'rot';

        for (const [name, h] of Object.entries(handles)) {
            if (name === 'rot') continue;
            if (Math.abs(lx - h.x) <= hitRadius && Math.abs(ly - h.y) <= hitRadius) return name;
        }
        return null;
    }

    /** Compute world positions of the 4 corner + 1 rotation handles. */
    function _getHandles(el) {
        const cx  = el.x + el.width  / 2;
        const cy  = el.y + el.height / 2;
        const hw  = el.width  / 2;
        const hh  = el.height / 2;
        const rad = el.rotation * Math.PI / 180;
        const cos = Math.cos(rad);
        const sin = Math.sin(rad);
        const _r  = (lx, ly) => ({ x: cx + lx * cos - ly * sin, y: cy + lx * sin + ly * cos });
        return {
            tl:  _r(-hw, -hh),
            tr:  _r( hw, -hh),
            bl:  _r(-hw,  hh),
            br:  _r( hw,  hh),
            rot: _r(  0, -hh - ROT_HANDLE_OFF),
        };
    }

    // -------------------------------------------------------------------------
    // Physical layer: mask + overlay image loading
    // -------------------------------------------------------------------------

    function _loadMaskImage() {
        const url = _physical.maskImageUrl;
        if (!url) { _physical.maskImage = null; return; }

        const cached = _imageCache.get(url);
        if (cached) { _physical.maskImage = cached; render(); return; }

        const session = ++_physical.maskSession;
        _physical.maskImage = null;

        const img = new Image();
        if (!url.startsWith('data:') && !url.startsWith('blob:')) {
            img.crossOrigin = 'anonymous';
        }
        img.onload = () => {
            if (!_initialized || session !== _physical.maskSession) return;
            _imageCache.set(url, img);
            _physical.maskImage = img;
            render();
        };
        img.onerror = () => {
            if (!_initialized || session !== _physical.maskSession) return;
            _imageCache.set(url, 'error');
            _physical.maskImage = 'error';
            render();
        };
        img.src = url;
    }

    function _loadOverlayImage() {
        const url = _physical.overlayImageUrl;
        if (!url) { _physical.overlayImage = null; return; }

        const cached = _imageCache.get(url);
        if (cached) { _physical.overlayImage = cached; render(); return; }

        const session = ++_physical.overlaySession;
        _physical.overlayImage = null;

        const img = new Image();
        if (!url.startsWith('data:') && !url.startsWith('blob:')) {
            img.crossOrigin = 'anonymous';
        }
        img.onload = () => {
            if (!_initialized || session !== _physical.overlaySession) return;
            _imageCache.set(url, img);
            _physical.overlayImage = img;
            render();
        };
        img.onerror = () => {
            if (!_initialized || session !== _physical.overlaySession) return;
            _imageCache.set(url, 'error');
            _physical.overlayImage = 'error';
            render();
        };
        img.src = url;
    }

    // -------------------------------------------------------------------------
    // Background
    // -------------------------------------------------------------------------

    function _compressDataUrlIfNeeded(dataUrl, maxDimension = 1200, quality = 0.85) {
        return new Promise((resolve) => {
            if (!dataUrl || !dataUrl.startsWith('data:image/') || dataUrl.length < 300000) {
                resolve(dataUrl);
                return;
            }
            const img = new Image();
            img.onload = () => {
                let w = img.naturalWidth;
                let h = img.naturalHeight;
                if (w <= maxDimension && h <= maxDimension && dataUrl.length < 500000) {
                    resolve(dataUrl);
                    return;
                }
                const scale = Math.min(maxDimension / Math.max(1, w), maxDimension / Math.max(1, h), 1);
                w = Math.max(1, Math.round(w * scale));
                h = Math.max(1, Math.round(h * scale));
                const cvs = document.createElement('canvas');
                cvs.width = w;
                cvs.height = h;
                const ctx = cvs.getContext('2d');
                if (ctx) {
                    ctx.drawImage(img, 0, 0, w, h);
                    try {
                        const webp = cvs.toDataURL('image/webp', quality);
                        if (webp && webp.startsWith('data:image/webp') && webp.length < dataUrl.length) {
                            resolve(webp);
                            return;
                        }
                    } catch (_) {}
                    try {
                        const jpeg = cvs.toDataURL('image/jpeg', quality);
                        if (jpeg && jpeg.startsWith('data:image/jpeg') && jpeg.length < dataUrl.length) {
                            resolve(jpeg);
                            return;
                        }
                    } catch (_) {}
                }
                resolve(dataUrl);
            };
            img.onerror = () => resolve(dataUrl);
            img.src = dataUrl;
        });
    }

    async function setBackground(url) {
        if (!url || !url.trim()) return;
        _designRevision++;
        let optimizedUrl = url.trim();
        if (optimizedUrl.startsWith('data:image/') && optimizedUrl.length > 350000) {
            try {
                optimizedUrl = await _compressDataUrlIfNeeded(optimizedUrl, 1200, 0.85);
            } catch (_) {}
        }
        _state.background = { sourceType: 'url', url: optimizedUrl };
        _state.dirty = true;
        _loadImage(optimizedUrl);
        render();
    }

    function clearBackground() {
        _designRevision++;
        _state.background = null;
        _state.dirty = true;
        render();
    }

    // -------------------------------------------------------------------------
    // Add sticker
    // -------------------------------------------------------------------------

    function addSticker(stickerId, imageUrl, name) {
        _designRevision++;
        const runtimeId = _generateId();
        const offset    = _state._stickerOffset * 20;
        _state._stickerOffset = (_state._stickerOffset + 1) % 10;

        const x = _clamp(EDIT_X + (EDIT_W - DEFAULT_STICKER_W) / 2 + offset, EDIT_X, EDIT_X + EDIT_W - DEFAULT_STICKER_W);
        const y = _clamp(EDIT_Y + 140 + offset, EDIT_Y, EDIT_Y + EDIT_H - DEFAULT_STICKER_H);

        _state.elements.push({
            id: runtimeId, type: 'sticker', x, y,
            width: DEFAULT_STICKER_W, height: DEFAULT_STICKER_H, rotation: 0,
            zIndex: _state._nextZIndex++,
            data: { stickerId, imageUrl: imageUrl || '', name: name || '' },
        });

        _state.selectedElementId = runtimeId;
        _state.dirty = true;
        if (imageUrl) _loadImage(imageUrl);
        render();
    }

    // -------------------------------------------------------------------------
    // Add text
    // -------------------------------------------------------------------------

    function addText(text) {
        const trimmed = (text || '').trim();
        if (!trimmed) return false;
        _designRevision++;

        const runtimeId = _generateId();
        const offset    = _state._textOffset * 20;
        _state._textOffset = (_state._textOffset + 1) % 10;

        const x = _clamp(EDIT_X + (EDIT_W - DEFAULT_TEXT_W) / 2 + offset, EDIT_X, EDIT_X + EDIT_W - DEFAULT_TEXT_W);
        const y = _clamp(EDIT_Y + 240 + offset, EDIT_Y, EDIT_Y + EDIT_H - DEFAULT_TEXT_H);

        _state.elements.push({
            id: runtimeId, type: 'text', x, y,
            width: DEFAULT_TEXT_W, height: DEFAULT_TEXT_H, rotation: 0,
            zIndex: _state._nextZIndex++,
            data: { text: trimmed, fontSize: 48, fontFamily: 'Arial, sans-serif', fontWeight: '400', color: '#111111' },
        });

        _state.selectedElementId = runtimeId;
        _state.dirty = true;
        render();
        return true;
    }

    // -------------------------------------------------------------------------
    // Remove selected
    // -------------------------------------------------------------------------

    function removeSelected() {
        if (!_state.selectedElementId || _state.previewMode || _state.elements.find(e => e.id === _state.selectedElementId)?.locked) return;
        _designRevision++;
        if (_interaction.elementId === _state.selectedElementId) _clearInteraction();
        _state.elements          = _state.elements.filter(el => el.id !== _state.selectedElementId);
        _state.selectedElementId = null;
        _state.dirty             = true;
        render();
    }

    // -------------------------------------------------------------------------
    // Selection
    // -------------------------------------------------------------------------

    function selectElement(elementId) {
        _state.selectedElementId = _state.elements.some(el => el.id === elementId) ? elementId : null;
        render();
    }

    function getSelectedElementId() {
        return _state.selectedElementId;
    }

    // -------------------------------------------------------------------------
    // Image loading
    // -------------------------------------------------------------------------

    function _loadImage(url) {
        if (!url) return null;
        if (_imageCache.has(url)) return _imageCache.get(url);
        const img = new Image();
        const isDataOrBlob = url.startsWith('data:') || url.startsWith('blob:');
        if (!isDataOrBlob) {
            img.crossOrigin = 'anonymous';
        }
        img.onload  = () => { render(); };
        img.onerror = () => {
            if (img.crossOrigin) {
                // Retry without crossOrigin so at least image can render on editor canvas
                const retryImg = new Image();
                retryImg.onload = () => { render(); };
                retryImg.onerror = () => { _imageCache.set(url, 'error'); render(); };
                _imageCache.set(url, retryImg);
                retryImg.src = url;
            } else {
                _imageCache.set(url, 'error');
                render();
            }
        };
        _imageCache.set(url, img);
        img.src = url;
        return img;
    }

    function _getReadyImage(url) {
        if (!url) return null;
        const cached = _imageCache.get(url);
        if (!cached || cached === 'error') return null;
        if (cached.complete && cached.naturalWidth > 0) return cached;
        return null;
    }

    // -------------------------------------------------------------------------
    // Rendering
    // -------------------------------------------------------------------------

    function render() {
        if (!_ctx || !_canvas) return;

        _renderDesign(_ctx, _state, !_state.previewMode);
        if (!_interaction.mode && !_restoring) {
            const snapshot = JSON.stringify({ background: _state.background, elements: _state.elements });
            if (_history[_historyIndex] !== snapshot) {
                _history = _history.slice(0, _historyIndex + 1);
                _history.push(snapshot);
                if (_history.length > 60) _history.shift();
                _historyIndex = _history.length - 1;
            }
            _scheduleDraftSave();
        }
        if (_observer && !_interaction.mode) {
            const status = getState();
            const json = JSON.stringify(status);
            if (json !== _lastNotification) {
                _lastNotification = json;
                _observer.invokeMethodAsync('EditorChanged', status).catch(() => {});
            }
        }
    }

    function _renderDesign(ctx, state, includeEditorGuides, includeMask = true, includeOverlay = true) {
        const w = LOGICAL_WIDTH, h = LOGICAL_HEIGHT;
        ctx.clearRect(0, 0, w, h);

        // 1. User design — clipped to the case editable area
        ctx.save();
        _roundRect(ctx, EDIT_X, EDIT_Y, EDIT_W, EDIT_H, EDIT_R);
        ctx.clip();
        _drawEditableAreaFill(ctx);
        if (state.background) _drawBackground(ctx, state.background, includeEditorGuides);
        if (includeEditorGuides && _showGrid) {
            ctx.save();
            ctx.strokeStyle = 'rgba(0, 0, 0, 0.06)';
            ctx.lineWidth = 1;
            ctx.beginPath();
            for (let x = EDIT_X; x <= EDIT_X + EDIT_W; x += 20) { ctx.moveTo(x, EDIT_Y); ctx.lineTo(x, EDIT_Y + EDIT_H); }
            for (let y = EDIT_Y; y <= EDIT_Y + EDIT_H; y += 20) { ctx.moveTo(EDIT_X, y); ctx.lineTo(EDIT_X + EDIT_W, y); }
            ctx.stroke();
            ctx.restore();
        }
        const sorted = [...state.elements].sort((a, b) => a.zIndex - b.zIndex);
        for (const el of sorted) _drawElement(ctx, el, includeEditorGuides);
        ctx.restore();   // end design clip

        // 2. Case outer shell line art (chỉ hiển thị khi includeMask, không xuất ra PNG in ấn)
        if (includeMask) {
            _drawCaseOuter(ctx, w, h);
        }

        // 3. Physical overlay (material appearance) — editor + preview; NOT in export
        if (includeOverlay) _drawPhysicalOverlay(ctx);

        // 4. Camera cutout / mask — editor + preview; NOT in export
        if (includeMask) {
            _drawCameraMask(ctx);
        } else if (_physical.cameraCutoutWidth > 0 && _physical.cameraCutoutHeight > 0) {
            // Khi xuất file in ấn: khoét lỗ camera rỗng trong suốt (vì ốp không in cụm camera)
            ctx.save();
            ctx.globalCompositeOperation = 'destination-out';
            _roundRect(ctx, _physical.cameraCutoutX, _physical.cameraCutoutY, _physical.cameraCutoutWidth, _physical.cameraCutoutHeight, _physical.cameraCutoutRadius);
            ctx.fill();
            ctx.restore();
        }

        // 5. Editor-only guides (Print Area & Safe Area)
        if (includeEditorGuides) {
            _drawPrintAreaOverlay(ctx);
            _drawSafeAreaOverlay(ctx);
        }

        // 6. Selection handles
        if (includeEditorGuides && state.selectedElementId) {
            const sel = state.elements.find(e => e.id === state.selectedElementId);
            if (sel && !sel.hidden && !sel.locked) _drawSelectionOverlay(ctx, sel);
        }
    }

    function _drawCaseOuter(ctx, w, h) {
        ctx.save();
        // Main Outer Phone Case Body (Đường line trong suốt)
        _roundRect(ctx, 0, 0, w, h, CASE_R);
        ctx.strokeStyle = '#cbd5e1';
        ctx.lineWidth = 2.5;
        ctx.stroke();

        // Inner Bezel Accent Line (Đường line viền mép trong suốt)
        if (w > 10 && h > 10) {
            _roundRect(ctx, 5, 5, w - 10, h - 10, Math.max(2, CASE_R - 4));
            ctx.strokeStyle = 'rgba(0, 0, 0, 0.08)';
            ctx.lineWidth = 1.2;
            ctx.stroke();
        }
        ctx.restore();
    }

    // Draw only the clean white fill for the editable area (used inside design clip).
    function _drawEditableAreaFill(ctx) {
        _roundRect(ctx, EDIT_X, EDIT_Y, EDIT_W, EDIT_H, EDIT_R);
        ctx.fillStyle = COLOR_EDITABLE_FILL;
        ctx.fill();
    }

    // Kept for backward compat with any external calls (safe area guide fallback).
    function _drawEditableArea(ctx) {
        _drawEditableAreaFill(ctx);
    }

    function _drawBackground(ctx, background, includeEditorGuides) {
        if (!background) return;
        if (background.color) {
            ctx.save(); _roundRect(ctx, EDIT_X, EDIT_Y, EDIT_W, EDIT_H, EDIT_R);
            ctx.fillStyle = background.color; ctx.fill(); ctx.restore(); return;
        }
        const img = _getReadyImage(background.url);
        ctx.save();
        _roundRect(ctx, EDIT_X, EDIT_Y, EDIT_W, EDIT_H, EDIT_R);
        ctx.clip();
        if (img) {
            const scale = Math.max(EDIT_W / img.naturalWidth, EDIT_H / img.naturalHeight);
            const dw = img.naturalWidth * scale, dh = img.naturalHeight * scale;
            ctx.drawImage(img, EDIT_X + (EDIT_W - dw) / 2, EDIT_Y + (EDIT_H - dh) / 2, dw, dh);
        } else if (includeEditorGuides) {
            ctx.fillStyle = 'rgba(220,225,235,0.6)'; ctx.fillRect(EDIT_X, EDIT_Y, EDIT_W, EDIT_H);
            ctx.font = '16px system-ui, sans-serif'; ctx.fillStyle = COLOR_PLACEHOLDER_FG;
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            ctx.fillText('Đang tải hình nền...', EDIT_X + EDIT_W / 2, EDIT_Y + EDIT_H / 2);
        }
        ctx.restore();
    }

    function _drawElement(ctx, el, includeEditorGuides) {
        if (el.hidden) return;
        ctx.save(); ctx.globalAlpha = el.opacity ?? 1;
        if      (el.type === 'sticker') _drawStickerElement(ctx, el, includeEditorGuides);
        else if (el.type === 'text')    _drawTextElement(ctx, el);
        ctx.restore();
    }

    function _drawStickerElement(ctx, el, includeEditorGuides) {
        const { x, y, width, height, rotation, data } = el;
        const cx = x + width / 2, cy = y + height / 2;
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(rotation * Math.PI / 180);
        const img = data.imageUrl ? _getReadyImage(data.imageUrl) : null;
        if (img) {
            ctx.drawImage(img, -width / 2, -height / 2, width, height);
        } else {
            // Draw clean placeholder even when previewMode is on so sticker never disappears
            ctx.fillStyle = COLOR_PLACEHOLDER_BG; ctx.strokeStyle = COLOR_PLACEHOLDER_FG;
            ctx.lineWidth = 1.5;
            if (includeEditorGuides) ctx.setLineDash([5, 4]);
            _roundRect(ctx, -width / 2, -height / 2, width, height, 10);
            ctx.fill();
            ctx.stroke();
            ctx.setLineDash([]);
            ctx.font = 'bold 13px system-ui, sans-serif'; ctx.fillStyle = COLOR_PLACEHOLDER_FG;
            ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
            ctx.fillText(data.name || 'Sticker', 0, 0);
        }
        ctx.restore();
    }

    function _drawTextElement(ctx, el) {
        const { x, y, width, height, rotation, data } = el;
        const cx = x + width / 2, cy = y + height / 2;
        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(rotation * Math.PI / 180);
        ctx.beginPath(); ctx.rect(-width/2,-height/2,width,height); ctx.clip();
        ctx.font = `${data.fontWeight||'400'} ${data.fontSize||48}px ${data.fontFamily||'Arial, sans-serif'}`;
        const measuredWidth = ctx.measureText(data.text || '')?.width || 1;
        const fittedSize = Math.min(data.fontSize || 48, height * .8, (data.fontSize || 48) * Math.max(1, width - 8) / measuredWidth);
        ctx.font = `${data.fontWeight||'400'} ${fittedSize}px ${data.fontFamily||'Arial, sans-serif'}`;
        ctx.fillStyle = data.color || '#111111';
        ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.fillText(data.text || '', 0, 0);
        ctx.restore();
    }

    function _drawSafeAreaGuide(ctx, w, h) {
        if (_physical.safeAreaWidth > 0 && _physical.safeAreaHeight > 0) return;
        ctx.save();
        ctx.setLineDash([6, 4]);
        ctx.strokeStyle = '#38bdf8';
        ctx.lineWidth = 1.5;
        _roundRect(ctx, 14, 14, w - 28, h - 28, Math.max(CASE_R - 10, 8));
        ctx.stroke();
        ctx.restore();
    }

    // ---- Physical overlay (material layer — above design, below mask, not a user element) ----

    function _drawPhysicalOverlay(ctx) {
        const img = _physical.overlayImage;
        if (!img || img === 'error') return;
        if (!img.complete || img.naturalWidth === 0) return;
        ctx.save();
        ctx.drawImage(img, 0, 0, LOGICAL_WIDTH, LOGICAL_HEIGHT);
        ctx.restore();
    }

    // ---- Camera Cutout / Mask (physical layer — above design, not a user element) ----

    function _drawCameraMask(ctx) {
        // Priority 1: Parametric 4-sided geometric camera cutout
        if (_physical.cameraCutoutWidth > 0 && _physical.cameraCutoutHeight > 0) {
            _drawGeometricCameraCutout(ctx);
            return;
        }

        // Priority 2: Bitmap mask image if configured and loaded
        const img = _physical.maskImage;
        if (img && img !== 'error' && img.complete && img.naturalWidth > 0) {
            ctx.save();
            ctx.drawImage(img, 0, 0, LOGICAL_WIDTH, LOGICAL_HEIGHT);
            ctx.restore();
            return;
        }
    }

    function _drawGeometricCameraCutout(ctx) {
        const { cameraCutoutX: x, cameraCutoutY: y, cameraCutoutWidth: w, cameraCutoutHeight: h, cameraCutoutRadius: r } = _physical;
        if (w <= 0 || h <= 0) return;

        ctx.save();

        // 1. Thân cụm camera (Tô nền tối sang trọng để che phủ sticker / màu nền bên dưới)
        _roundRect(ctx, x, y, w, h, r);
        ctx.fillStyle = '#0f172a';
        ctx.fill();

        // 2. Khung viền ngoài cụm camera (Đường line hồng đỏ)
        ctx.strokeStyle = '#f43f5e';
        ctx.lineWidth = 2.2;
        ctx.stroke();

        // 3. Đường viền vát cạnh bên trong
        if (w > 5 && h > 5) {
            _roundRect(ctx, x + 2.5, y + 2.5, w - 5, h - 5, Math.max(1, r - 2));
            ctx.strokeStyle = 'rgba(244, 63, 94, 0.4)';
            ctx.lineWidth = 1;
            ctx.stroke();
        }

        // 4. Các ống kính camera mô phỏng theo 4 layout
        _drawCutoutLenses(ctx, x, y, w, h, r);

        ctx.restore();
    }

    function _drawCutoutLenses(ctx, x, y, w, h, r) {
        const slug = (_physical.slug || '').toLowerCase();
        const isCircle = (Math.abs(w - h) <= 12 && r >= w * 0.38) || (slug.includes('ultra') && slug.includes('xiaomi'));
        const isVertical = !isCircle && (h / Math.max(1, w) >= 1.35 || slug.includes('galaxy') || (slug.includes('16') && !slug.includes('pro')));
        const isHorizontal = !isCircle && (w / Math.max(1, h) >= 1.55 || slug.includes('pixel'));
        const isIPhone16 = isVertical && (slug.includes('16') && !slug.includes('pro'));
        const isDiagonal2Lens = !isCircle && !isVertical && !isHorizontal && (slug.includes('15') && !slug.includes('pro') || (w <= 100 && h <= 100 && slug.includes('iphone') && !slug.includes('pro')));

        if (isCircle) {
            // Module camera tròn lớn (Xiaomi 14 Ultra)
            const rad = w / 2;
            const cx = x + rad;
            const cy = y + rad;
            const lensR = rad * 0.25;

            // Vòng tròn định vị nét đứt
            ctx.save();
            ctx.setLineDash([4, 2]);
            ctx.beginPath();
            ctx.arc(cx, cy, rad * 0.82, 0, Math.PI * 2);
            ctx.strokeStyle = 'rgba(244, 63, 94, 0.4)';
            ctx.lineWidth = 1;
            ctx.stroke();
            ctx.restore();

            // Vòng tròn trung tâm
            ctx.beginPath();
            ctx.arc(cx, cy, rad * 0.45, 0, Math.PI * 2);
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.25)';
            ctx.lineWidth = 1;
            ctx.stroke();

            // 4 ống kính
            const positions = [
                { lx: cx - rad * 0.38, ly: cy - rad * 0.38 },
                { lx: cx + rad * 0.38, ly: cy - rad * 0.38 },
                { lx: cx - rad * 0.38, ly: cy + rad * 0.38 },
                { lx: cx + rad * 0.38, ly: cy + rad * 0.38 },
            ];
            for (const p of positions) {
                ctx.beginPath();
                ctx.arc(p.lx, p.ly, lensR, 0, Math.PI * 2);
                ctx.strokeStyle = '#fb7185';
                ctx.lineWidth = 1.8;
                ctx.stroke();

                ctx.beginPath();
                ctx.arc(p.lx, p.ly, lensR * 0.5, 0, Math.PI * 2);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.45)';
                ctx.lineWidth = 1;
                ctx.stroke();

                // Tâm quang học
                ctx.beginPath();
                ctx.moveTo(p.lx - 3, p.ly); ctx.lineTo(p.lx + 3, p.ly);
                ctx.moveTo(p.lx, p.ly - 3); ctx.lineTo(p.lx, p.ly + 3);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.6)';
                ctx.lineWidth = 0.8;
                ctx.stroke();
            }

            // Tâm quang học chính giữa module
            ctx.beginPath();
            ctx.moveTo(cx - 5, cy); ctx.lineTo(cx + 5, cy);
            ctx.moveTo(cx, cy - 5); ctx.lineTo(cx, cy + 5);
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.7)';
            ctx.lineWidth = 1;
            ctx.stroke();

        } else if (isIPhone16) {
            // Dạng dọc 2 mắt (iPhone 16 thường)
            const lensRadius = Math.min(w * 0.34, 24);
            const centerX = x + w / 2;
            const offsets = [y + h * 0.28, y + h * 0.72];

            // Viền pill bên trong
            const pillPad = 3;
            _roundRect(ctx, x + pillPad, y + pillPad, w - pillPad * 2, h - pillPad * 2, Math.max(1, r - pillPad));
            ctx.strokeStyle = 'rgba(244, 63, 94, 0.35)';
            ctx.lineWidth = 1;
            ctx.stroke();

            for (const cy of offsets) {
                ctx.beginPath();
                ctx.arc(centerX, cy, lensRadius, 0, Math.PI * 2);
                ctx.strokeStyle = '#fb7185';
                ctx.lineWidth = 1.8;
                ctx.stroke();

                ctx.beginPath();
                ctx.arc(centerX, cy, lensRadius * 0.55, 0, Math.PI * 2);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.45)';
                ctx.lineWidth = 1;
                ctx.stroke();

                ctx.beginPath();
                ctx.moveTo(centerX - 3, cy); ctx.lineTo(centerX + 3, cy);
                ctx.moveTo(centerX, cy - 3); ctx.lineTo(centerX, cy + 3);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.6)';
                ctx.lineWidth = 0.8;
                ctx.stroke();
            }

        } else if (isVertical) {
            // Dạng dọc 3 mắt (Galaxy S24 / S25, Galaxy Ultra)
            const lensRadius = Math.min(w * 0.32, 22);
            const centerX = x + w / 2;
            const offsets = [y + h * 0.22, y + h * 0.48, y + h * 0.74];

            for (const cy of offsets) {
                // Vòng ống kính ngoài
                ctx.beginPath();
                ctx.arc(centerX, cy, lensRadius, 0, Math.PI * 2);
                ctx.strokeStyle = '#fb7185';
                ctx.lineWidth = 1.8;
                ctx.stroke();

                // Vòng phản xạ kính trong
                ctx.beginPath();
                ctx.arc(centerX, cy, lensRadius * 0.55, 0, Math.PI * 2);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.45)';
                ctx.lineWidth = 1;
                ctx.stroke();

                // Chữ thập tâm quang học
                ctx.beginPath();
                ctx.moveTo(centerX - 3, cy); ctx.lineTo(centerX + 3, cy);
                ctx.moveTo(centerX, cy - 3); ctx.lineTo(centerX, cy + 3);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.6)';
                ctx.lineWidth = 0.8;
                ctx.stroke();
            }

        } else if (isHorizontal) {
            // Dạng dải ngang (Google Pixel visor)
            const lensRadius = Math.min(h * 0.32, 20);
            const centerY = y + h / 2;
            const leftOffset = x + w * 0.25;
            const midOffset = x + w * 0.42;
            const rightOffset = x + w * 0.75;

            // Khung thuốc oval bao quanh cụm 2 camera
            const pillW = w * 0.38;
            const pillH = lensRadius * 2.7;
            const pillX = x + w * 0.14;
            const pillY = centerY - lensRadius * 1.35;
            const pillR = lensRadius * 1.35;
            _roundRect(ctx, pillX, pillY, pillW, pillH, pillR);
            ctx.strokeStyle = 'rgba(244, 63, 94, 0.4)';
            ctx.lineWidth = 1.2;
            ctx.stroke();

            // Lens 1
            ctx.beginPath();
            ctx.arc(leftOffset, centerY, lensRadius, 0, Math.PI * 2);
            ctx.strokeStyle = '#fb7185';
            ctx.lineWidth = 1.8;
            ctx.stroke();
            ctx.beginPath();
            ctx.arc(leftOffset, centerY, lensRadius * 0.55, 0, Math.PI * 2);
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.45)';
            ctx.lineWidth = 1;
            ctx.stroke();

            // Lens 2
            ctx.beginPath();
            ctx.arc(midOffset, centerY, lensRadius, 0, Math.PI * 2);
            ctx.strokeStyle = '#fb7185';
            ctx.lineWidth = 1.8;
            ctx.stroke();
            ctx.beginPath();
            ctx.arc(midOffset, centerY, lensRadius * 0.55, 0, Math.PI * 2);
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.45)';
            ctx.lineWidth = 1;
            ctx.stroke();

            // Vòng đèn Flash nét đứt vàng
            ctx.save();
            ctx.setLineDash([3, 2]);
            ctx.beginPath();
            ctx.arc(rightOffset, centerY, lensRadius * 0.55, 0, Math.PI * 2);
            ctx.strokeStyle = '#facc15';
            ctx.lineWidth = 1.5;
            ctx.stroke();
            ctx.restore();

        } else if (isDiagonal2Lens) {
            // Dạng 2 mắt chéo (iPhone 15 thường, iPhone 14/13)
            const lensRadius = Math.min(Math.min(w, h) * 0.24, 25);
            const topLeftX = x + w * 0.33;
            const topLeftY = y + h * 0.33;
            const botRightX = x + w * 0.67;
            const botRightY = y + h * 0.67;
            const topRightX = x + w * 0.68;
            const topRightY = y + h * 0.30;
            const botLeftX = x + w * 0.30;
            const botLeftY = y + h * 0.70;

            const diagonalLenses = [
                { lx: topLeftX, ly: topLeftY },
                { lx: botRightX, ly: botRightY },
            ];

            for (const l of diagonalLenses) {
                ctx.beginPath();
                ctx.arc(l.lx, l.ly, lensRadius, 0, Math.PI * 2);
                ctx.strokeStyle = '#fb7185';
                ctx.lineWidth = 1.8;
                ctx.stroke();

                ctx.beginPath();
                ctx.arc(l.lx, l.ly, lensRadius * 0.55, 0, Math.PI * 2);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.45)';
                ctx.lineWidth = 1;
                ctx.stroke();

                ctx.beginPath();
                ctx.moveTo(l.lx - 3, l.ly); ctx.lineTo(l.lx + 3, l.ly);
                ctx.moveTo(l.lx, l.ly - 3); ctx.lineTo(l.lx, l.ly + 3);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.6)';
                ctx.lineWidth = 0.8;
                ctx.stroke();
            }

            // Đèn Flash nét đứt vàng ở góc trên phải
            ctx.save();
            ctx.setLineDash([3, 2]);
            ctx.beginPath();
            ctx.arc(topRightX, topRightY, lensRadius * 0.45, 0, Math.PI * 2);
            ctx.strokeStyle = '#facc15';
            ctx.lineWidth = 1.5;
            ctx.stroke();
            ctx.restore();

            // Microphone / Cảm biến ở góc dưới trái
            ctx.beginPath();
            ctx.arc(botLeftX, botLeftY, lensRadius * 0.28, 0, Math.PI * 2);
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.4)';
            ctx.lineWidth = 1;
            ctx.stroke();

        } else {
            // Dạng vuông 3 mắt tam giác (iPhone 15 Pro, iPhone 16 Pro, Xiaomi 14)
            const lensRadius = Math.min(Math.min(w, h) * 0.21, 24);
            const col1X = x + w * 0.32;
            const col2X = x + w * 0.68;
            const row1Y = y + h * 0.30;
            const row2Y = y + h * 0.70;
            const midY = y + h * 0.50;

            const lenses = [
                { lx: col1X, ly: row1Y },
                { lx: col1X, ly: row2Y },
                { lx: col2X, ly: midY },
            ];

            for (const l of lenses) {
                ctx.beginPath();
                ctx.arc(l.lx, l.ly, lensRadius, 0, Math.PI * 2);
                ctx.strokeStyle = '#fb7185';
                ctx.lineWidth = 1.8;
                ctx.stroke();

                ctx.beginPath();
                ctx.arc(l.lx, l.ly, lensRadius * 0.55, 0, Math.PI * 2);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.45)';
                ctx.lineWidth = 1;
                ctx.stroke();

                ctx.beginPath();
                ctx.moveTo(l.lx - 3, l.ly); ctx.lineTo(l.lx + 3, l.ly);
                ctx.moveTo(l.lx, l.ly - 3); ctx.lineTo(l.lx, l.ly + 3);
                ctx.strokeStyle = 'rgba(255, 255, 255, 0.6)';
                ctx.lineWidth = 0.8;
                ctx.stroke();
            }

            // Vòng đèn Flash nét đứt vàng
            ctx.save();
            ctx.setLineDash([3, 2]);
            ctx.beginPath();
            ctx.arc(col2X, row1Y, lensRadius * 0.42, 0, Math.PI * 2);
            ctx.strokeStyle = '#facc15';
            ctx.lineWidth = 1.5;
            ctx.stroke();
            ctx.restore();

            // Cảm biến phụ / LiDAR
            ctx.beginPath();
            ctx.arc(col2X, row2Y, lensRadius * 0.32, 0, Math.PI * 2);
            ctx.strokeStyle = 'rgba(255, 255, 255, 0.35)';
            ctx.lineWidth = 1;
            ctx.stroke();
        }
    }

    // ---- Safe Area overlay (editor guide — shown in edit mode only) ----

    function _drawSafeAreaOverlay(ctx) {
        const { safeAreaX: x, safeAreaY: y, safeAreaWidth: w, safeAreaHeight: h } = _physical;
        if (w <= 0 || h <= 0) return;
        ctx.save();
        ctx.setLineDash([6, 4]);
        ctx.strokeStyle = '#38bdf8';
        ctx.lineWidth = 2;
        ctx.strokeRect(x, y, w, h);
        ctx.restore();
    }

    // ---- Print Area overlay (editor guide — shown in edit mode only) ----

    function _drawPrintAreaOverlay(ctx) {
        const { printAreaX: x, printAreaY: y, printAreaWidth: w, printAreaHeight: h } = _physical;
        if (w <= 0 || h <= 0) return;
        if (x === 0 && y === 0 && w === LOGICAL_WIDTH && h === LOGICAL_HEIGHT) return;
        ctx.save();
        ctx.setLineDash([8, 5]);
        ctx.strokeStyle = '#f59e0b';
        ctx.lineWidth = 2;
        ctx.strokeRect(x, y, w, h);
        ctx.restore();
    }

    function _drawSelectionOverlay(ctx, el) {
        const handles = _getHandles(el);
        const cx = el.x + el.width/2, cy = el.y + el.height/2;
        const hw = el.width/2, hh = el.height/2;

        ctx.save();
        ctx.translate(cx, cy);
        ctx.rotate(el.rotation * Math.PI / 180);
        ctx.strokeStyle = COLOR_SELECTION; ctx.lineWidth = 2; ctx.setLineDash([6,4]);
        ctx.strokeRect(-hw-2, -hh-2, el.width+4, el.height+4);
        ctx.setLineDash([]);
        // Line to rotation handle
        ctx.strokeStyle = COLOR_SELECTION; ctx.lineWidth = 1.5;
        ctx.beginPath(); ctx.moveTo(0, -hh); ctx.lineTo(0, -hh - ROT_HANDLE_OFF); ctx.stroke();
        ctx.restore();

        // Corner handles
        for (const [name, h] of Object.entries(handles)) {
            if (name === 'rot') continue;
            ctx.save();
            ctx.fillStyle = COLOR_HANDLE_FILL; ctx.strokeStyle = COLOR_HANDLE_STROKE; ctx.lineWidth = 2;
            ctx.fillRect(h.x - HANDLE_SIZE/2, h.y - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE);
            ctx.strokeRect(h.x - HANDLE_SIZE/2, h.y - HANDLE_SIZE/2, HANDLE_SIZE, HANDLE_SIZE);
            ctx.restore();
        }

        // Rotation handle
        const rh = handles.rot;
        ctx.save();
        ctx.beginPath(); ctx.arc(rh.x, rh.y, ROT_HANDLE_R, 0, Math.PI * 2);
        ctx.fillStyle = COLOR_ROT_HANDLE; ctx.fill();
        ctx.strokeStyle = '#ffffff'; ctx.lineWidth = 2; ctx.stroke();
        ctx.restore();
    }

    function _drawCameraNotch(ctx, w) {
        const nw = 120, nh = 24;
        const nx = (w - nw) / 2, ny = CASE_OUTER_MARGIN + 12;
        _roundRect(ctx, nx, ny, nw, nh, 12);
        ctx.fillStyle = '#1a1a24'; ctx.fill();
        const lx = nx + nw - 28, ly = ny + nh / 2;
        ctx.beginPath(); ctx.arc(lx, ly, 7, 0, Math.PI*2); ctx.fillStyle = '#3a3a4a'; ctx.fill();
        ctx.beginPath(); ctx.arc(lx, ly, 4, 0, Math.PI*2); ctx.fillStyle = '#555570'; ctx.fill();
    }

    // -------------------------------------------------------------------------
    // Coordinate conversion
    // -------------------------------------------------------------------------

    function toLogical(displayX, displayY) {
        if (!_canvas) return { x: 0, y: 0 };
        const rect = _canvas.getBoundingClientRect();
        return { x: displayX * (LOGICAL_WIDTH / rect.width), y: displayY * (LOGICAL_HEIGHT / rect.height) };
    }

    // -------------------------------------------------------------------------
    // Draft Persistence (Browser localStorage)
    // -------------------------------------------------------------------------
    const DRAFT_KEY_PREFIX = 'caseshop.draft.design.';
    const LAST_SLUG_KEY    = 'caseshop.draft.last_model_slug';
    let _saveDraftTimer    = null;

    function _scheduleDraftSave() {
        if (!_initialized || _restoring || _state.previewMode) return;
        if (_saveDraftTimer) clearTimeout(_saveDraftTimer);
        _saveDraftTimer = setTimeout(() => {
            saveDraft();
        }, 400);
    }

    function saveDraft(customSlug) {
        try {
            const slug = customSlug || _state.phoneModelConfig?.slug || 'default';
            const key  = DRAFT_KEY_PREFIX + slug;

            // If completely empty (no background and no elements), delete draft key
            if (!_state.background && (!_state.elements || _state.elements.length === 0)) {
                localStorage.removeItem(key);
                return;
            }

            const draft = {
                version: 1,
                timestamp: Date.now(),
                modelSlug: slug,
                modelId: _state.phoneModelConfig?.id || null,
                background: _state.background ? { ..._state.background } : null,
                elements: _state.elements.map(el => ({
                    id: el.id,
                    type: el.type,
                    x: el.x,
                    y: el.y,
                    width: el.width,
                    height: el.height,
                    rotation: el.rotation,
                    opacity: el.opacity,
                    locked: !!el.locked,
                    hidden: !!el.hidden,
                    zIndex: el.zIndex,
                    data: { ...el.data }
                })),
                nextZIndex: _state._nextZIndex
            };

            const json = JSON.stringify(draft);
            try {
                localStorage.setItem(key, json);
                localStorage.setItem(LAST_SLUG_KEY, slug);
            } catch (quotaErr) {
                console.warn('[CaseShopEditor] LocalStorage quota reached, pruning old drafts...', quotaErr);
                _pruneOldDrafts(key);
                try {
                    localStorage.setItem(key, json);
                    localStorage.setItem(LAST_SLUG_KEY, slug);
                } catch (_) {
                    // Fallback: save draft without large background data URL
                    if (draft.background?.url && draft.background.url.startsWith('data:')) {
                        draft.background = { color: '#ffffff' };
                        localStorage.setItem(key, JSON.stringify(draft));
                    }
                }
            }
        } catch (e) {
            console.error('[CaseShopEditor] Error saving draft:', e);
        }
    }

    function _pruneOldDrafts(currentKey) {
        try {
            const now = Date.now();
            const keysToRemove = [];
            for (let i = 0; i < localStorage.length; i++) {
                const k = localStorage.key(i);
                if (k && k.startsWith(DRAFT_KEY_PREFIX) && k !== currentKey) {
                    try {
                        const parsed = JSON.parse(localStorage.getItem(k) || '{}');
                        if (!parsed.timestamp || (now - parsed.timestamp > 7 * 86400 * 1000)) {
                            keysToRemove.push(k);
                        }
                    } catch (_) {
                        keysToRemove.push(k);
                    }
                }
            }
            keysToRemove.forEach(k => localStorage.removeItem(k));
        } catch (_) {}
    }

    function loadDraft(customSlug) {
        try {
            const slug = customSlug || _state.phoneModelConfig?.slug || 'default';
            const key  = DRAFT_KEY_PREFIX + slug;
            const raw  = localStorage.getItem(key);
            if (!raw) return null;

            const draft = JSON.parse(raw);
            if (!draft) return null;

            const hasElements = Array.isArray(draft.elements) && draft.elements.length > 0;
            const hasBg       = !!draft.background;
            if (!hasElements && !hasBg) return null;

            _restoring = true;
            _uploadGeneration++;
            _designRevision++;

            _state.background = draft.background || null;
            _state.elements   = Array.isArray(draft.elements) ? draft.elements : [];
            _state._nextZIndex = draft.nextZIndex || (Math.max(0, ..._state.elements.map(e => e.zIndex || 0)) + 1);
            _state.selectedElementId = null;
            _state.dirty = true;

            // Pre-cache all restored images
            if (_state.background?.url) {
                _loadImage(_state.background.url);
            }
            for (const el of _state.elements) {
                if (el.type === 'sticker' && el.data?.imageUrl) {
                    _loadImage(el.data.imageUrl);
                }
            }

            _restoring = false;
            return {
                restored: true,
                elementCount: _state.elements.length,
                modelSlug: draft.modelSlug || slug
            };
        } catch (e) {
            console.error('[CaseShopEditor] Error loading draft:', e);
            _restoring = false;
            return null;
        }
    }

    function clearDraft(customSlug) {
        try {
            const slug = customSlug || _state.phoneModelConfig?.slug || 'default';
            localStorage.removeItem(DRAFT_KEY_PREFIX + slug);
        } catch (_) {}
    }

    // -------------------------------------------------------------------------
    // Reset
    // -------------------------------------------------------------------------

    function reset() {
        _uploadGeneration++;
        _designRevision++;
        _clearInteraction();
        clearDraft(_state.phoneModelConfig?.slug);
        _state.background = null; _state.elements = []; _state.selectedElementId = null;
        _state.dirty = false; _state._nextZIndex = 1; _state._stickerOffset = 0; _state._textOffset = 0;
        _state.previewMode = false;
        render();
    }

    // -------------------------------------------------------------------------
    // Dispose
    // -------------------------------------------------------------------------

    function dispose() {
        _observer = null; _lastNotification = ''; _history = []; _historyIndex = -1; _showGrid = false;
        _clearInteraction();
        if (_saveDraftTimer) { clearTimeout(_saveDraftTimer); _saveDraftTimer = null; }
        if (_resizeObserver) { _resizeObserver.disconnect(); _resizeObserver = null; }
        _detachPointerListeners();
        _canvas = null; _ctx = null; _initialized = false;
        // Invalidate any in-flight mask/overlay loads.
        _physical.maskSession++;
        _physical.maskImage = null;
        _physical.overlaySession++;
        _physical.overlayImage = null;
        for (const img of _imageCache.values()) {
            if (img !== 'error') { img.onload = null; img.onerror = null; }
        }
        _imageCache.clear();
        // Reset in-memory state cleanly without deleting user's saved draft from localStorage
        _uploadGeneration++;
        _designRevision++;
        _state.background = null; _state.elements = []; _state.selectedElementId = null;
        _state.dirty = false; _state._nextZIndex = 1; _state._stickerOffset = 0; _state._textOffset = 0;
        _state.previewMode = false;
    }

    // -------------------------------------------------------------------------
    // Utilities
    // -------------------------------------------------------------------------

    function _generateId() {
        return 'el_' + Math.random().toString(36).slice(2, 9) + '_' + Date.now().toString(36);
    }

    function _clamp(v, min, max) { return Math.max(min, Math.min(max, v)); }

    function _roundRect(ctx, x, y, w, h, r) {
        if (typeof r === 'number') r = { tl: r, tr: r, br: r, bl: r };
        ctx.beginPath();
        ctx.moveTo(x + r.tl, y);
        ctx.lineTo(x + w - r.tr, y);
        ctx.quadraticCurveTo(x + w, y, x + w, y + r.tr);
        ctx.lineTo(x + w, y + h - r.br);
        ctx.quadraticCurveTo(x + w, y + h, x + w - r.br, y + h);
        ctx.lineTo(x + r.bl, y + h);
        ctx.quadraticCurveTo(x, y + h, x, y + h - r.bl);
        ctx.lineTo(x, y + r.tl);
        ctx.quadraticCurveTo(x, y, x + r.tl, y);
        ctx.closePath();
    }
    // -------------------------------------------------------------------------
    // Export Design
    // -------------------------------------------------------------------------

    // Wait on the shared cache without replacing its editor load handlers.
    function _waitForImage(url, timeoutMs) {
        return new Promise(resolve => {
            const img = _loadImage(url);
            if (!img || img === 'error') { resolve(false); return; }
            if (img.complete) { resolve(img.naturalWidth > 0); return; }
            const finish = ready => {
                clearTimeout(timer);
                img.removeEventListener('load', onLoad);
                img.removeEventListener('error', onError);
                resolve(ready);
            };
            const onLoad = () => finish(img.naturalWidth > 0);
            const onError = () => finish(false);
            const timer = setTimeout(() => finish(false), timeoutMs);
            img.addEventListener('load', onLoad);
            img.addEventListener('error', onError);
            if (img.complete) finish(img.naturalWidth > 0);
        });
    }

    async function exportDesign() {
        if (!_initialized || !_canvas || !_ctx || _exportInProgress) {
            return { success: false, error: 'Editor is not ready to export. Please try again.' };
        }
        _exportInProgress = true;
        const editorCanvas = _canvas;
        let link;
        try {
            // Capture the design at click time; edits during image loading stay independent.
            const design = {
                background: _state.background ? { ..._state.background } : null,
                elements: _state.elements.map(el => ({ ...el, data: { ...el.data } })),
            };
            const urls = [...new Set([
                design.background?.url,
                ...design.elements.filter(el => el.type === 'sticker').map(el => el.data.imageUrl),
            ].filter(Boolean))];
            const ready = await Promise.all(urls.map(url => _waitForImage(url, 10000)));
            if (!_initialized || _canvas !== editorCanvas) {
                return { success: false, error: 'Editor was closed. Please try again.' };
            }

            // Same renderer and logical dimensions, on a separate browser-only canvas.
            // includeMask=false, includeOverlay=false: physical layers must not appear in the exported PNG.
            const canvas = document.createElement('canvas');
            canvas.width = LOGICAL_WIDTH;
            canvas.height = LOGICAL_HEIGHT;
            const ctx = canvas.getContext('2d');
            if (!ctx) throw new Error('Canvas unavailable');
            _renderDesign(ctx, design, false, false, false);
            const dataUrl = canvas.toDataURL('image/png');
            link = document.createElement('a');
            link.href = dataUrl;
            link.download = 'caseshop-design.png';
            document.body.appendChild(link);
            link.click();
            const missingImages = ready.some(value => !value) ||
                design.elements.some(el => el.type === 'sticker' && !el.data.imageUrl);
            return {
                success: true,
                warning: missingImages ? 'Some images could not be loaded and were omitted from the PNG.' : null,
            };
        } catch (_) {
            return { success: false, error: 'Unable to export PNG. Check image access permissions and try again.' };
        } finally {
            link?.remove();
            _exportInProgress = false;
        }
    }

    // -------------------------------------------------------------------------
    // Preview mode
    // -------------------------------------------------------------------------

    /**
     * Enable or disable preview mode.
     * In preview mode: pointer interactions are suppressed, selection overlay is hidden.
     * @param {boolean} enabled
     */
    function setPreviewMode(enabled) {
        if (enabled) _uploadGeneration++;
        // If entering preview while dragging, cancel the active interaction cleanly
        if (enabled && _interaction.mode !== null) {
            _restoreInteraction();
            _clearInteraction();
        }
        _state.previewMode = !!enabled;
        render();
    }

    function isPreviewMode() {
        return _state.previewMode;
    }

    // -------------------------------------------------------------------------
    // Add uploaded sticker
    // -------------------------------------------------------------------------

    /**
     * Add an image uploaded by the user (not a database Sticker) as a sticker element.
     * stickerId is null because this is not a DB entity.
     * @param {string} imageUrl  Cloudinary HTTPS URL
     * @param {string} name      Display name
     */
    function beginUpload() {
        return _initialized && !_state.previewMode ? ++_uploadGeneration : null;
    }

    function addUploadedSticker(imageUrl, name, generation) {
        if (!_initialized || _state.previewMode || generation !== _uploadGeneration || !imageUrl) return false;
        _uploadGeneration++; // Consume the token so duplicate completions cannot add twice.
        // Reuse the existing addSticker path with stickerId = null
        addSticker(null, imageUrl, name || 'Uploaded sticker');
        return true;
    }


    // -------------------------------------------------------------------------
    // UI-only editing commands. History and selection stay in browser memory.
    function getState() {
        return {
            elements: _state.elements.slice().sort((a, b) => b.zIndex - a.zIndex).map(el => ({
                id: el.id, type: el.type, name: el.type === 'text' ? el.data.text : el.data.name,
                x: Math.round(el.x), y: Math.round(el.y), width: Math.round(el.width), height: Math.round(el.height),
                rotation: Math.round(el.rotation), opacity: Math.round((el.opacity ?? 1) * 100),
                hidden: !!el.hidden, locked: !!el.locked, color: el.data.color || '#111111',
                fontSize: el.data.fontSize || 48, fontFamily: el.data.fontFamily || 'Arial, sans-serif',
                bold: el.data.fontWeight === '700'
            })),
            selectedId: _state.selectedElementId, canUndo: _historyIndex > 0,
            canRedo: _historyIndex < _history.length - 1, grid: _showGrid
        };
    }

    function observe(observer) { _observer = observer; _lastNotification = ''; render(); }

    function command(action, value, id) {
        if (!_initialized || _interaction.mode || _exportInProgress) return;
        if (action === 'grid') { _showGrid = !_showGrid; render(); return; }
        if (_state.previewMode) return;
        if (action === 'undo' || action === 'redo') {
            const next = _historyIndex + (action === 'undo' ? -1 : 1);
            if (next < 0 || next >= _history.length) return;
            _uploadGeneration++; _designRevision++;
            _historyIndex = next;
            const restored = JSON.parse(_history[next]);
            _state.background = restored.background; _state.elements = restored.elements;
            _state.selectedElementId = null;
            _state._nextZIndex = Math.max(0, ...restored.elements.map(el => el.zIndex)) + 1;
            _state.dirty = true; _restoring = true; render(); _restoring = false; return;
        }
        if (action === 'backgroundColor' && /^#[0-9a-f]{6}$/i.test(value)) {
            _state.background = { color: value }; _state.dirty = true; _designRevision++; render(); return;
        }
        const el = _state.elements.find(item => item.id === (id || _state.selectedElementId));
        if (!el) return;
        if (action === 'select') { selectElement(el.id); return; }
        if (action === 'lock') el.locked = !el.locked;
        else if (el.locked) return;
        else if (action === 'visibility') el.hidden = !el.hidden;
        else if (action === 'delete') { _state.elements = _state.elements.filter(item => item !== el); _state.selectedElementId = null; }
        else if (action === 'duplicate') {
            const copy = JSON.parse(JSON.stringify(el)); copy.id = _generateId(); copy.zIndex = _state._nextZIndex++;
            copy.x = Math.min(el.x + 20, EDIT_X + EDIT_W - el.width); copy.y = Math.min(el.y + 20, EDIT_Y + EDIT_H - el.height);
            _state.elements.push(copy); _state.selectedElementId = copy.id;
        } else if (action === 'forward' || action === 'backward') {
            const ordered = _state.elements.slice().sort((a, b) => a.zIndex - b.zIndex);
            const index = ordered.indexOf(el), other = ordered[index + (action === 'forward' ? 1 : -1)];
            if (other) [el.zIndex, other.zIndex] = [other.zIndex, el.zIndex];
        } else if (action === 'center') {
            el.x = EDIT_X + (EDIT_W - el.width) / 2; el.y = EDIT_Y + (EDIT_H - el.height) / 2;
        } else if (action === 'text' && el.type === 'text' && String(value).trim()) el.data.text = String(value).trim().slice(0, 100);
        else if (action === 'color' && /^#[0-9a-f]{6}$/i.test(value)) el.data.color = value;
        else if (action === 'bold') el.data.fontWeight = el.data.fontWeight === '700' ? '400' : '700';
        else if (action === 'fontFamily' && ['Arial, sans-serif', 'Georgia, serif', 'Courier New, monospace'].includes(value)) el.data.fontFamily = value;
        else {
            const number = Number(value); if (!Number.isFinite(number)) return;
            if (action === 'opacity') el.opacity = _clamp(number, 0, 100) / 100;
            else if (action === 'rotation') el.rotation = _clamp(number, -180, 180);
            else if (action === 'fontSize') el.data.fontSize = _clamp(number, 8, 180);
            else if (action === 'x') el.x = _clamp(number, EDIT_X, EDIT_X + EDIT_W - el.width);
            else if (action === 'y') el.y = _clamp(number, EDIT_Y, EDIT_Y + EDIT_H - el.height);
            else if (action === 'width') {
                const ratio = el.height / el.width;
                el.width = _clamp(number, Math.max(MIN_W, MIN_H / ratio), Math.min(EDIT_W, EDIT_H / ratio)); el.height = el.width * ratio;
                el.x = _clamp(el.x, EDIT_X, EDIT_X + EDIT_W - el.width); el.y = _clamp(el.y, EDIT_Y, EDIT_Y + EDIT_H - el.height);
            } else return;
        }
        _designRevision++; _state.dirty = true; render();
    }

    /**
     * Generate snapshot for direct ordering:
     * - previewImageUrl: realistic phone case with shell & camera bump
     * - printImageUrl: clean printable artwork (no guides, camera cutout punched out)
     * - designData: JSON stringified state
     */
    async function getDesignSnapshot() {
        if (!_initialized || !_canvas) {
            return { success: false, error: 'Trình chỉnh sửa chưa sẵn sàng.' };
        }

        try {
            // Wait for pending images if any
            const urls = [...new Set([
                _state.background?.url,
                ..._state.elements.filter(el => el.type === 'sticker').map(el => el.data.imageUrl),
            ].filter(Boolean))];
            try {
                await Promise.all(urls.map(url => _waitForImage(url, 3000)));
            } catch (_) {}

            function _safeToDataUrl(c, fallbackRenderFn, quality = 0.82) {
                try {
                    const webp = c.toDataURL('image/webp', quality);
                    if (webp && webp.startsWith('data:image/webp')) return webp;
                    return c.toDataURL('image/png');
                } catch (taintErr) {
                    console.warn('[CaseShopEditor] Canvas tainted during snapshot export, falling back to clean render:', taintErr);
                    if (fallbackRenderFn) {
                        try {
                            const safeCanvas = document.createElement('canvas');
                            safeCanvas.width = LOGICAL_WIDTH;
                            safeCanvas.height = LOGICAL_HEIGHT;
                            const safeCtx = safeCanvas.getContext('2d');
                            if (safeCtx) {
                                fallbackRenderFn(safeCtx);
                                const safeWebp = safeCanvas.toDataURL('image/webp', quality);
                                if (safeWebp && safeWebp.startsWith('data:image/webp')) return safeWebp;
                                return safeCanvas.toDataURL('image/png');
                            }
                        } catch (_) {}
                    }
                    // Absolute fallback: transparent 1x1 image so order flow never crashes
                    return 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==';
                }
            }

            // 1. Preview image: realistic phone case with shell & camera bump
            const previewCanvas = document.createElement('canvas');
            previewCanvas.width = LOGICAL_WIDTH;
            previewCanvas.height = LOGICAL_HEIGHT;
            const pCtx = previewCanvas.getContext('2d');
            if (pCtx) {
                _renderDesign(pCtx, _state, false, true, true);
            }
            const previewImageUrl = _safeToDataUrl(previewCanvas, (ctx) => {
                _renderDesign(ctx, _state, false, false, false);
            }, 0.82);

            // 2. Printable artwork: without case lines, camera cutout punched out
            const printCanvas = document.createElement('canvas');
            printCanvas.width = LOGICAL_WIDTH;
            printCanvas.height = LOGICAL_HEIGHT;
            const prCtx = printCanvas.getContext('2d');
            if (prCtx) {
                _renderDesign(prCtx, _state, false, false, false);
            }
            const printImageUrl = _safeToDataUrl(printCanvas, (ctx) => {
                const minimalState = { background: _state.background, elements: [] };
                _renderDesign(ctx, minimalState, false, false, false);
            }, 0.85);

            return {
                success: true,
                previewImageUrl,
                printImageUrl,
                designData: JSON.stringify({
                    phoneModelId: _state.phoneModelConfig?.id,
                    phoneModelName: _state.phoneModelConfig?.name,
                    canvasWidth: LOGICAL_WIDTH,
                    canvasHeight: LOGICAL_HEIGHT,
                    background: _state.background,
                    elements: _state.elements
                })
            };
        } catch (err) {
            console.error('[CaseShopEditor] getDesignSnapshot error:', err);
            return {
                success: false,
                error: (err && err.message) || 'Lỗi khi chụp snapshot thiết kế.'
            };
        }
    }

    // Public API
    // -------------------------------------------------------------------------
    return {
        init, setPhoneModel, render, reset, dispose, toLogical,
        setBackground, clearBackground,
        addSticker, beginUpload, addUploadedSticker, addText, removeSelected,
        selectElement, getSelectedElementId,
        setPreviewMode, isPreviewMode,
        exportDesign, getDesignSnapshot, observe, getState, command,
        saveDraft, loadDraft, clearDraft,
    };
})();


