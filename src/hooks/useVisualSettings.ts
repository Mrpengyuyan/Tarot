import { useEffect, useMemo, useState } from 'react';
import config from '../config/env';
import { useUserPreferences } from './useLocalStorage';

export type VisualQuality = 'full' | 'lite' | 'minimal';

export interface VisualSettings {
  quality: VisualQuality;
  enableAnimations: boolean;
  enable3D: boolean;
  enableAmbientMotion: boolean;
  enableHoverMotion: boolean;
  enableSparkles: boolean;
  cardMotionPreset: 'full' | 'lite' | 'flat';
  backgroundMode: 'full' | 'lite' | 'minimal';
}

const getViewportWidth = (): number => {
  if (typeof window === 'undefined') {
    return 1440;
  }

  return window.innerWidth || 1440;
};

const getHardwareProfile = () => {
  if (typeof navigator === 'undefined') {
    return {
      hardwareConcurrency: 8,
      deviceMemory: 8,
      narrowViewport: false,
      coarsePointer: false,
    };
  }

  const nav = navigator as Navigator & { deviceMemory?: number };
  const coarsePointer =
    typeof window !== 'undefined' && typeof window.matchMedia === 'function'
      ? window.matchMedia('(pointer: coarse)').matches
      : false;

  return {
    hardwareConcurrency: navigator.hardwareConcurrency || 8,
    deviceMemory: nav.deviceMemory || 8,
    narrowViewport: getViewportWidth() < 960,
    coarsePointer,
  };
};

export const useVisualSettings = (): VisualSettings => {
  const { preferences } = useUserPreferences();
  const [prefersReducedMotion, setPrefersReducedMotion] = useState(false);

  useEffect(() => {
    if (typeof window === 'undefined' || typeof window.matchMedia !== 'function') {
      return;
    }

    const mediaQuery = window.matchMedia('(prefers-reduced-motion: reduce)');
    const updatePreference = () => setPrefersReducedMotion(mediaQuery.matches);

    updatePreference();
    mediaQuery.addEventListener('change', updatePreference);
    return () => mediaQuery.removeEventListener('change', updatePreference);
  }, []);

  return useMemo(() => {
    const hardware = getHardwareProfile();
    const animationsEnabled = config.features.enableAnimations && preferences.animationsEnabled !== false;
    const allow3D = config.features.enable3D && animationsEnabled;
    const lowPowerDevice = hardware.hardwareConcurrency <= 4 || hardware.deviceMemory <= 4;
    const veryLowPowerDevice = hardware.hardwareConcurrency <= 2 || hardware.deviceMemory <= 2;

    let quality: VisualQuality = 'full';

    if (!animationsEnabled || prefersReducedMotion) {
      quality = 'minimal';
    } else if (veryLowPowerDevice || (lowPowerDevice && hardware.narrowViewport) || hardware.coarsePointer) {
      quality = 'lite';
    }

    return {
      quality,
      enableAnimations: animationsEnabled && quality !== 'minimal',
      enable3D: allow3D && quality !== 'minimal',
      enableAmbientMotion: quality === 'full',
      enableHoverMotion: quality !== 'minimal',
      enableSparkles: allow3D && quality === 'full',
      cardMotionPreset: quality === 'full' ? 'full' : quality === 'lite' ? 'lite' : 'flat',
      backgroundMode: quality,
    };
  }, [preferences.animationsEnabled, prefersReducedMotion]);
};
