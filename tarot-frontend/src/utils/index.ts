// 工具函数统一导出

export { default as constants } from './constants';
export { default as dateUtils } from './dateUtils';
export { default as formatUtils } from './formatUtils';
export { default as validationUtils } from './validationUtils';
export { default as storageUtils } from './storageUtils';
export { default as mathUtils } from './mathUtils';

// 具名导出常用函数
export { COLORS, ROUTES, API, TAROT, SPREADS, QUESTION_TYPES } from './constants';

export {
  formatDate,
  formatDateTime,
  formatTime,
  formatRelativeTime,
  formatSmartDate,
  isToday,
  getZodiacSign,
  calculateAge,
} from './dateUtils';

export {
  formatNumber,
  formatCurrency,
  formatPercentage,
  formatFileSize,
  formatDuration,
  truncateText,
  capitalize,
  formatJSON,
} from './formatUtils';

export {
  required,
  email,
  phone,
  password,
  minLength,
  maxLength,
  validateObject,
  isObjectValid,
} from './validationUtils';

export {
  setItem,
  getItem,
  removeItem,
  clearAll,
  hasItem,
  cache,
} from './storageUtils';

export {
  randomInt,
  randomChoice,
  shuffle,
  clamp,
  lerp,
  distance,
  average,
  generateTarotDrawSequence,
  generateCardOrientation,
} from './mathUtils';

// 类型导出
export type { ValidationResult, ValidationRule } from './validationUtils';
export type { StorageConfig } from './storageUtils';