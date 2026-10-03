const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '..');
const read = relative => fs.readFileSync(path.join(root, relative), 'utf8');

test('motion orchestrator follows Blazor render lifecycle and disposes navigation state', () => {
  const source = read('CaseShop.Web/Components/Shared/UI/MotionOrchestrator.razor');
  assert.match(source, /@rendermode InteractiveServer/);
  assert.match(source, /OnAfterRenderAsync\(bool firstRender\)/);
  assert.match(source, /LocationChanged \+= HandleLocationChanged/);
  assert.match(source, /LocationChanged -= HandleLocationChanged/);
  assert.match(source, /InvokeVoidAsync\("destroy"\)/);
  assert.match(source, /JSDisconnectedException/);
});

test('motion core owns cleanup and respects reduced motion', () => {
  const source = read('CaseShop.Web/wwwroot/js/animations/motion-core.js');
  assert.match(source, /prefers-reduced-motion: reduce/);
  assert.match(source, /cleanups\.splice\(0\)\.reverse\(\)/);
  assert.match(source, /MutationObserver/);
  assert.match(source, /observer\.disconnect\(\)/);
  assert.match(source, /cancelAnimationFrame/);
  assert.doesNotMatch(source, /killAll\(/);
});

test('premium motion is scoped to storefront storytelling markers', () => {
  const hero = read('CaseShop.Web/Components/Pages/HomeSections/HomeHeroSection.razor');
  const home = read('CaseShop.Web/Components/Pages/Home.razor');
  const about = read('CaseShop.Web/Components/Pages/Company/About.razor');
  assert.match(hero, /data-motion-hero/);
  assert.match(hero, /data-motion-image/);
  assert.match(hero, /min-h-\[calc\(100svh-4rem\)\]/);
  assert.match(hero, /Mang ý tưởng/);
  assert.match(hero, /Custom phone case studio/);
  assert.match(hero, /data-motion-hero-stage/);
  assert.match(hero, /data-motion-hero-depth/);
  assert.match(hero, /data-motion-hero-shine/);
  const heroMotion = read('CaseShop.Web/wwwroot/js/animations/hero.js');
  assert.match(heroMotion, /\(hover: hover\) and \(pointer: fine\)/);
  assert.match(heroMotion, /gsap\.quickTo/);
  assert.match(heroMotion, /removeEventListener\('pointermove'/);
  assert.match(home, /data-motion-story/);
  assert.match(home, /lg:sticky lg:top-28/);
  assert.match(about, /data-motion-stagger/);
});

test('GSAP and ScrollTrigger are served locally and reduced motion has a CSS fallback', () => {
  const app = read('CaseShop.Web/Components/App.razor');
  const vendor = read('CaseShop.Web/scripts/copy-vendor.cjs');
  const css = read('CaseShop.Web/wwwroot/app.css');
  assert.match(app, /vendor\/gsap\/gsap\.min\.js/);
  assert.match(app, /vendor\/gsap\/ScrollTrigger\.min\.js/);
  assert.match(vendor, /node_modules\/gsap\/dist\/gsap\.min\.js/);
  assert.match(css, /prefers-reduced-motion: reduce/);
});
