export interface LightboxPanBounds {
  viewportWidth: number;
  viewportHeight: number;
  imageWidth: number;
  imageHeight: number;
  scale: number;
}

export function clampLightboxPan(
  translateX: number,
  translateY: number,
  bounds: LightboxPanBounds
): { translateX: number; translateY: number } {
  if (bounds.scale <= 1) {
    return { translateX: 0, translateY: 0 };
  }

  const scaledWidth = bounds.imageWidth * bounds.scale;
  const scaledHeight = bounds.imageHeight * bounds.scale;
  const maxX = Math.max(0, (scaledWidth - bounds.viewportWidth) / 2);
  const maxY = Math.max(0, (scaledHeight - bounds.viewportHeight) / 2);

  return {
    translateX: Math.min(maxX, Math.max(-maxX, translateX)),
    translateY: Math.min(maxY, Math.max(-maxY, translateY))
  };
}
