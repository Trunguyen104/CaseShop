export function initReveal(gsap, ScrollTrigger, root, reducedMotion) {
    if (reducedMotion) return () => {};
    const contexts = [];

    root.querySelectorAll('[data-motion-reveal]').forEach((element) => {
        const context = gsap.context(() => {
            gsap.fromTo(element,
                { autoAlpha: 0, y: 30, clipPath: 'inset(0 0 18% 0)' },
                {
                    autoAlpha: 1,
                    y: 0,
                    clipPath: 'inset(0 0 0% 0)',
                    duration: 0.9,
                    ease: 'power3.out',
                    clearProps: 'transform,opacity,visibility,clipPath',
                    scrollTrigger: { trigger: element, start: 'top 86%', once: true }
                });
        }, element);
        contexts.push(context);
    });

    root.querySelectorAll('[data-motion-stagger]').forEach((group) => {
        const context = gsap.context(() => {
            gsap.fromTo(Array.from(group.children),
                { autoAlpha: 0, y: 24 },
                {
                    autoAlpha: 1,
                    y: 0,
                    duration: 0.72,
                    stagger: 0.1,
                    ease: 'power2.out',
                    clearProps: 'transform,opacity,visibility',
                    scrollTrigger: { trigger: group, start: 'top 84%', once: true }
                });
        }, group);
        contexts.push(context);
    });

    return () => contexts.forEach((context) => context.revert());
}
