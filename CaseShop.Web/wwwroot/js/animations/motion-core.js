import { initHero } from './hero.js';
import { initReveal } from './reveal.js';
import { initParallax } from './parallax.js';
import { initImageReveal } from './image-reveal.js';
import { initStorytelling } from './storytelling.js';

let cleanups = [];
let navigationFrame = 0;

function getRuntime() {
    const gsap = window.gsap;
    const ScrollTrigger = window.ScrollTrigger;
    if (!gsap || !ScrollTrigger) return null;
    gsap.registerPlugin(ScrollTrigger);
    return { gsap, ScrollTrigger };
}

export function destroy() {
    if (navigationFrame) {
        window.cancelAnimationFrame(navigationFrame);
        navigationFrame = 0;
    }
    cleanups.splice(0).reverse().forEach((cleanup) => cleanup());
}

export function init() {
    destroy();
    const runtime = getRuntime();
    if (!runtime) return;

    const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
    cleanups.push(initHero(runtime.gsap, document, reducedMotion));
    cleanups.push(initReveal(runtime.gsap, runtime.ScrollTrigger, document, reducedMotion));
    cleanups.push(initImageReveal(runtime.gsap, document, reducedMotion));
    cleanups.push(initParallax(runtime.gsap, runtime.ScrollTrigger, document, reducedMotion));
    cleanups.push(initStorytelling(runtime.gsap, document, reducedMotion));
    runtime.ScrollTrigger.refresh();

    const motionRoot = document.querySelector('main');
    if (motionRoot) {
        let refreshFrame = 0;
        const observer = new MutationObserver((mutations) => {
            const hasNewContent = mutations.some((mutation) => mutation.addedNodes.length > 0);
            if (!hasNewContent || refreshFrame) return;
            refreshFrame = window.requestAnimationFrame(() => {
                refreshFrame = 0;
                init();
            });
        });
        observer.observe(motionRoot, { childList: true, subtree: true });
        cleanups.push(() => {
            observer.disconnect();
            if (refreshFrame) window.cancelAnimationFrame(refreshFrame);
        });
    }
}

export function refresh() {
    if (navigationFrame) window.cancelAnimationFrame(navigationFrame);
    navigationFrame = window.requestAnimationFrame(() => {
        navigationFrame = 0;
        init();
    });
}
