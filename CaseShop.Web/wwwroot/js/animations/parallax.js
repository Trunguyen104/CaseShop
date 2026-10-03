export function initParallax(gsap, ScrollTrigger, root, reducedMotion) {
    if (reducedMotion || !window.matchMedia('(min-width: 768px)').matches) return () => {};
    const contexts = [];

    root.querySelectorAll('[data-motion-parallax]').forEach((element) => {
        const distance = Number(element.dataset.motionParallax) || 48;
        const context = gsap.context(() => {
            gsap.fromTo(element, { y: -distance / 2 }, {
                y: distance / 2,
                ease: 'none',
                scrollTrigger: {
                    trigger: element.closest('section') || element,
                    start: 'top bottom',
                    end: 'bottom top',
                    scrub: 0.8
                }
            });
        }, element);
        contexts.push(context);
    });

    return () => contexts.forEach((context) => context.revert());
}
