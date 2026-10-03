export function initHero(gsap, root, reducedMotion) {
    const heroes = root.querySelectorAll('[data-motion-hero]');
    if (!heroes.length || reducedMotion) return () => {};

    const contexts = [];
    const interactionCleanups = [];
    heroes.forEach((hero) => {
        const context = gsap.context(() => {
            const items = hero.querySelectorAll('[data-motion-hero-item]');
            const visual = hero.querySelector('[data-motion-hero-visual]');
            const glow = hero.querySelector('[data-motion-hero-glow]');
            const timeline = gsap.timeline({ defaults: { ease: 'power3.out' } });

            timeline.fromTo(items,
                { autoAlpha: 0, y: 36 },
                { autoAlpha: 1, y: 0, duration: 0.9, stagger: 0.16, clearProps: 'transform,opacity,visibility' });

            if (visual) {
                timeline.fromTo(visual,
                    { autoAlpha: 0, scale: 1.06, y: 36, rotate: 3 },
                    { autoAlpha: 1, scale: 1, y: 0, rotate: 0, duration: 1.25, clearProps: 'transform,opacity,visibility' },
                    0.2);
            }

            if (glow) {
                gsap.to(glow, { xPercent: -5, yPercent: 4, scale: 1.08, duration: 7, repeat: -1, yoyo: true, ease: 'sine.inOut' });
            }

            const stage = hero.querySelector('[data-motion-hero-stage]');
            const layers = hero.querySelectorAll('[data-motion-hero-depth]');
            const shine = hero.querySelector('[data-motion-hero-shine]');
            const canHover = window.matchMedia('(hover: hover) and (pointer: fine)').matches;
            if (visual && stage && canHover) {
                const rotateX = gsap.quickTo(stage, 'rotationX', { duration: 0.55, ease: 'power3.out' });
                const rotateY = gsap.quickTo(stage, 'rotationY', { duration: 0.55, ease: 'power3.out' });
                const layerMotion = Array.from(layers, (layer) => ({
                    depth: Number(layer.dataset.motionHeroDepth) || 0,
                    x: gsap.quickTo(layer, 'x', { duration: 0.6, ease: 'power3.out' }),
                    y: gsap.quickTo(layer, 'y', { duration: 0.6, ease: 'power3.out' })
                }));
                const shineX = shine ? gsap.quickTo(shine, 'xPercent', { duration: 0.4, ease: 'power2.out' }) : null;
                const shineY = shine ? gsap.quickTo(shine, 'yPercent', { duration: 0.4, ease: 'power2.out' }) : null;

                const handlePointerMove = (event) => {
                    const bounds = visual.getBoundingClientRect();
                    const x = ((event.clientX - bounds.left) / bounds.width - 0.5) * 2;
                    const y = ((event.clientY - bounds.top) / bounds.height - 0.5) * 2;
                    rotateX(-y * 6);
                    rotateY(x * 8);
                    layerMotion.forEach((layer) => {
                        layer.x(x * 18 * layer.depth);
                        layer.y(y * 14 * layer.depth);
                    });
                    if (shineX && shineY && shine) {
                        shineX(x * 26);
                        shineY(y * 26);
                        gsap.to(shine, { autoAlpha: 0.75, duration: 0.25, overwrite: true });
                    }
                };

                const resetPointer = () => {
                    rotateX(0);
                    rotateY(0);
                    layerMotion.forEach((layer) => { layer.x(0); layer.y(0); });
                    if (shine) gsap.to(shine, { autoAlpha: 0, duration: 0.4, overwrite: true });
                };

                visual.addEventListener('pointermove', handlePointerMove, { passive: true });
                visual.addEventListener('pointerleave', resetPointer);
                interactionCleanups.push(() => {
                    visual.removeEventListener('pointermove', handlePointerMove);
                    visual.removeEventListener('pointerleave', resetPointer);
                });
            }
        }, hero);
        contexts.push(context);
    });

    return () => {
        interactionCleanups.splice(0).forEach((cleanup) => cleanup());
        contexts.forEach((context) => context.revert());
    };
}
