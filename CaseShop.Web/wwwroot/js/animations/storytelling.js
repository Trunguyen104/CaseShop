export function initStorytelling(gsap, root, reducedMotion) {
    if (reducedMotion) return () => {};
    const contexts = [];

    root.querySelectorAll('[data-motion-story]').forEach((story) => {
        const steps = story.querySelectorAll('[data-motion-story-step]');
        const context = gsap.context(() => {
            steps.forEach((step, index) => {
                gsap.fromTo(step,
                    { autoAlpha: 0.55, y: 24, scale: 0.985 },
                    {
                        autoAlpha: 1,
                        y: 0,
                        scale: 1,
                        duration: 0.65,
                        ease: 'power2.out',
                        clearProps: 'transform,opacity,visibility',
                        scrollTrigger: {
                            id: `meism-story-${index}`,
                            trigger: step,
                            start: 'top 76%',
                            once: true
                        }
                    });
            });
        }, story);
        contexts.push(context);
    });

    return () => contexts.forEach((context) => context.revert());
}
