// 自定义钩子统一导出

export { default as useAuth, useRequireAuth, useUserPermissions } from './useAuth';
export { default as useApi, useGet, usePost, usePut, useDelete, usePaginatedApi, useUpload } from './useApi';
export {
  default as useLocalStorage,
  useLocalStorageWithCallback,
  useUserPreferences,
  useGameCache,
  useFormCache,
  useStorage,
} from './useLocalStorage';
export {
  default as useDebounce,
  useDebouncedCallback,
  useImmediateDebounce,
  useSearchDebounce,
  useInputDebounce,
  useApiDebounce,
  useScrollDebounce,
  useWindowSizeDebounce,
} from './useDebounce';
export {
  default as useInterval,
  useControllableInterval,
  useCountdown,
  useStopwatch,
  useClock,
  useAnimationFrame,
  useThrottle,
} from './useInterval';
export {
  default as use3DScene,
  use3DAnimation,
  useTarotCardFlip,
  use3DFloating,
  use3DRotation,
  use3DMouseInteraction,
  use3DParticleSystem,
} from './use3D';

// 类型导出
export type { LoginCredentials, RegisterData, UseAuthReturn } from './useAuth';
export type { ApiState, UseApiOptions, UseApiReturn } from './useApi';
export type { UseLocalStorageOptions } from './useLocalStorage';