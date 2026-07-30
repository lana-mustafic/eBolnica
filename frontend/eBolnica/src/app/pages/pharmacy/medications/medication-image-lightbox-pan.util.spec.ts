import { clampLightboxPan } from './medication-image-lightbox-pan.util';

describe('clampLightboxPan', () => {
  it('returns zero offset at or below 100% scale', () => {
    expect(
      clampLightboxPan(50, -30, {
        viewportWidth: 200,
        viewportHeight: 200,
        imageWidth: 400,
        imageHeight: 400,
        scale: 1
      })
    ).toEqual({ translateX: 0, translateY: 0 });
  });

  it('clamps pan within scaled image bounds', () => {
    expect(
      clampLightboxPan(500, -500, {
        viewportWidth: 200,
        viewportHeight: 100,
        imageWidth: 200,
        imageHeight: 100,
        scale: 2
      })
    ).toEqual({ translateX: 100, translateY: 50 });
  });

  it('allows negative pan up to symmetric bounds', () => {
    expect(
      clampLightboxPan(-80, 10, {
        viewportWidth: 240,
        viewportHeight: 120,
        imageWidth: 200,
        imageHeight: 100,
        scale: 2
      })
    ).toEqual({ translateX: -80, translateY: 10 });
  });

  it('locks pan to center when scaled image fits viewport', () => {
    expect(
      clampLightboxPan(40, -20, {
        viewportWidth: 400,
        viewportHeight: 400,
        imageWidth: 200,
        imageHeight: 200,
        scale: 1.5
      })
    ).toEqual({ translateX: 0, translateY: 0 });
  });
});
