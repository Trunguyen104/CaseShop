export function initImageReveal(gsap, root, reducedMotion) {
    if (reducedMotion) return () => {};
    const contexts = [];

    root.querySelectorAll('[data-motion-image]').forEach((wrapper) => {
        const image = wrapper.querySelector('img');
        if (!image) return;
        const context = gsap.context(() => {
            const timeline = gsap.timeline({
                scrollTrigger: { trigger: wrapper, start: 'top 84%', once: true }
            });
            timeline.fromTo(wrapper,
                { clipPath: 'inset(10% 0 10% 0)', autoAlpha: 0 },
                { clipPath: 'inset(0% 0 0% 0)', autoAlpha: 1, duration: 1, ease: 'power3.out', clearProps: 'clipPath,opacity,visibility' })
                .fromTo(image, { scale: 1.08 }, { scale: 1, duration: 1.2, ease: 'power3.out', clearProps: 'transform' }, 0);
        }, wrapper);
        contexts.push(context);
    });

    return () => contexts.forEach((context) => context.revert());
}
