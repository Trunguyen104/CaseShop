(function () {
    const storageKey = "caseshop.cart.v1";

    function read() {
        try {
            const value = JSON.parse(localStorage.getItem(storageKey) || "[]");
            return Array.isArray(value) ? value : [];
        } catch {
            return [];
        }
    }

    function count(items) {
        return items.reduce((total, item) => total + (Number(item.quantity || item.Quantity) || 0), 0);
    }

    function refreshBadge(items) {
        const badge = document.getElementById("cart-count");
        if (!badge) return;
        const total = count(items);
        badge.textContent = total > 99 ? "99+" : String(total);
        badge.classList.toggle("hidden", total === 0);
    }

    function write(items) {
        try {
            localStorage.setItem(storageKey, JSON.stringify(items));
        } catch (e) {
            console.warn('[CaseShopCart] LocalStorage write error, attempting compression fallback:', e);
            try {
                // If quota exceeded, strip large OriginalImageUrl from custom design to save space
                const slimmed = items.map(it => {
                    const cd = it.customDesign || it.CustomDesign;
                    if (cd && cd.OriginalImageUrl && cd.OriginalImageUrl.length > 50000) {
                        const copy = { ...it };
                        const cdCopy = { ...cd, OriginalImageUrl: null };
                        if (copy.customDesign) copy.customDesign = cdCopy;
                        if (copy.CustomDesign) copy.CustomDesign = cdCopy;
                        return copy;
                    }
                    return it;
                });
                localStorage.setItem(storageKey, JSON.stringify(slimmed));
            } catch (_) {
                try {
                    const trimmed = items.slice(-5);
                    localStorage.setItem(storageKey, JSON.stringify(trimmed));
                } catch (finalErr) {
                    console.error('[CaseShopCart] Unable to save cart to localStorage:', finalErr);
                }
            }
        }
        refreshBadge(items);
        window.dispatchEvent(new CustomEvent("caseshop-cart-changed", { detail: count(items) }));
    }

    window.CaseShopCart = {
        getItems: function () {
            const items = read();
            refreshBadge(items);
            return items;
        },
        addItem: function (item) {
            if (!item) return;
            const items = read();
            const customDesign = item.customDesign || item.CustomDesign;
            const productId = item.productId || item.ProductId;
            const variant = item.variant || item.Variant;
            const quantity = Number(item.quantity || item.Quantity) || 1;

            const mergeTarget = !customDesign
                ? items.find(existing => {
                    const exPid = existing.productId || existing.ProductId;
                    const exCustom = existing.customDesign || existing.CustomDesign;
                    const exVariant = existing.variant || existing.Variant;
                    return exPid === productId && !exCustom && exVariant === variant;
                })
                : null;

            if (mergeTarget) {
                const curQty = Number(mergeTarget.quantity || mergeTarget.Quantity) || 0;
                mergeTarget.quantity = Math.min(100, curQty + quantity);
            } else {
                item.quantity = Math.max(1, Math.min(100, quantity));
                items.push(item);
            }
            write(items);
        },
        updateQuantity: function (key, quantity) {
            const items = read();
            const item = items.find(entry => entry.key === key || entry.Key === key);
            if (item) {
                item.quantity = Math.max(1, Math.min(100, Number(quantity) || 1));
                write(items);
            }
        },
        removeItem: function (key) {
            write(read().filter(item => item.key !== key && item.Key !== key));
        },
        clear: function () {
            write([]);
        },
        refreshBadge: function () {
            refreshBadge(read());
        }
    };

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", window.CaseShopCart.refreshBadge);
    } else {
        window.CaseShopCart.refreshBadge();
    }
})();
