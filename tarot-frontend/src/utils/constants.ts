// API 相关常量
export const API = {
  BASE_URL: process.env.REACT_APP_API_BASE_URL || 'http://localhost:8000/api/v1',
  ENDPOINTS: {
    AUTH: {
      LOGIN: '/api/v1/login',
      REGISTER: '/api/v1/register',
      LOGOUT: '/api/v1/logout',
      REFRESH: '/api/v1/refresh',
      PROFILE: '/api/v1/users/me',
    },
    CARDS: {
      ALL: '/api/v1/cards',
      DETAIL: '/api/v1/cards/:id',
      SEARCH: '/api/v1/cards/search',
    },
    SPREADS: {
      ALL: '/api/v1/spreads',
      DETAIL: '/api/v1/spreads/:id',
      POPULAR: '/api/v1/spreads/popular',
    },
    READINGS: {
      ALL: '/api/v1/records',
      CREATE: '/api/v1/records',
      DETAIL: '/api/v1/records/:id',
      HISTORY: '/api/v1/records',
    },
    AI: {
      INTERPRET: '/api/v1/records/:id/interpret',
      FEEDBACK: '/api/v1/records/:id/interpretation',
    },
  },
} as const;

// 路由常量
export const ROUTES = {
  HOME: '/',
  DASHBOARD: '/dashboard',
  AUTH: {
    LOGIN: '/auth/login',
    REGISTER: '/auth/register',
  },
  READING: {
    NEW: '/reading/new',
    DETAIL: '/reading/:id',
    LIST: '/reading',
  },
  HISTORY: '/history',
  PROFILE: '/profile',
  CARDS: {
    LIST: '/cards',
    DETAIL: '/cards/:id',
  },
  SPREADS: {
    LIST: '/spreads',
    DETAIL: '/spreads/:id',
  },
} as const;

// 主题颜色常量
export const COLORS = {
  PRIMARY: {
    MAIN: '#D4AF37',
    LIGHT: '#FFD700',
    DARK: '#B8860B',
    CONTRAST: '#000000',
  },
  SECONDARY: {
    MAIN: '#4A0E4E',
    LIGHT: '#7B1FA2',
    DARK: '#2E003A',
    CONTRAST: '#FFFFFF',
  },
  MYSTICAL: {
    PURPLE: '#6A0572',
    INDIGO: '#AB83A1',
    GOLD: '#D4AF37',
    SILVER: '#C0C0C0',
    COSMIC: '#2D1B69',
  },
  BACKGROUND: {
    DEFAULT: '#0A0A0F',
    PAPER: '#1A1A2E',
    CARD: '#16213E',
  },
  TEXT: {
    PRIMARY: '#E8E8E8',
    SECONDARY: '#B8B8B8',
    DISABLED: '#707070',
  },
  STATUS: {
    SUCCESS: '#4ECDC4',
    ERROR: '#FF6B6B',
    WARNING: '#FFE66D',
    INFO: '#6DD5FA',
  },
} as const;

// 塔罗牌相关常量
export const TAROT = {
  MAJOR_ARCANA: {
    COUNT: 22,
    NAMES: [
      '愚者', '魔术师', '女祭司', '皇后', '皇帝', '教皇', '恋人',
      '战车', '力量', '隐士', '命运之轮', '正义', '倒吊人', '死神',
      '节制', '恶魔', '塔', '星星', '月亮', '太阳', '审判', '世界'
    ],
  },
  MINOR_ARCANA: {
    COUNT: 56,
    SUITS: {
      CUPS: { name: '圣杯', element: 'water', symbol: '♥', color: '#4ECDC4' },
      WANDS: { name: '权杖', element: 'fire', symbol: '♠', color: '#FF6B6B' },
      SWORDS: { name: '宝剑', element: 'air', symbol: '♠', color: '#6DD5FA' },
      PENTACLES: { name: '星币', element: 'earth', symbol: '♦', color: '#FFE66D' },
    },
    COURT_CARDS: ['侍从', '骑士', '王后', '国王'],
    NUMBER_CARDS: [1, 2, 3, 4, 5, 6, 7, 8, 9, 10],
  },
  POSITIONS: {
    UPRIGHT: 'upright',
    REVERSED: 'reversed',
  },
} as const;

// 牌阵类型常量
export const SPREADS = {
  SINGLE_CARD: {
    id: 'single',
    name: '单牌占卜',
    description: '简单直接的单张牌占卜',
    cardCount: 1,
    positions: [
      { id: 'present', name: '当前状况', x: 0, y: 0 },
    ],
  },
  THREE_CARD: {
    id: 'three-card',
    name: '三牌占卜',
    description: '过去、现在、未来的时间线占卜',
    cardCount: 3,
    positions: [
      { id: 'past', name: '过去', x: -1, y: 0 },
      { id: 'present', name: '现在', x: 0, y: 0 },
      { id: 'future', name: '未来', x: 1, y: 0 },
    ],
  },
  CELTIC_CROSS: {
    id: 'celtic-cross',
    name: '凯尔特十字',
    description: '最经典的十张牌占卜法',
    cardCount: 10,
    positions: [
      { id: 'present', name: '现状', x: 0, y: 0 },
      { id: 'challenge', name: '挑战', x: 0, y: 0 },
      { id: 'distant_past', name: '远因', x: 0, y: -1 },
      { id: 'possible_future', name: '可能未来', x: 0, y: 1 },
      { id: 'recent_past', name: '近因', x: -1, y: 0 },
      { id: 'near_future', name: '近期未来', x: 1, y: 0 },
      { id: 'approach', name: '应对方式', x: 2, y: 2 },
      { id: 'external', name: '外在影响', x: 2, y: 1 },
      { id: 'hopes_fears', name: '内心期望', x: 2, y: 0 },
      { id: 'outcome', name: '最终结果', x: 2, y: -1 },
    ],
  },
  RELATIONSHIP: {
    id: 'relationship',
    name: '爱情关系',
    description: '专门用于感情问题的牌阵',
    cardCount: 5,
    positions: [
      { id: 'you', name: '你的状态', x: -1, y: 0 },
      { id: 'partner', name: '对方状态', x: 1, y: 0 },
      { id: 'relationship', name: '关系现状', x: 0, y: 0 },
      { id: 'challenge', name: '面临挑战', x: 0, y: -1 },
      { id: 'outcome', name: '发展趋势', x: 0, y: 1 },
    ],
  },
} as const;

// 问题类型常量
export const QUESTION_TYPES = {
  LOVE: {
    id: 'love',
    name: '感情恋爱',
    icon: '💕',
    color: '#FF69B4',
    description: '关于爱情、感情、人际关系的问题',
    keywords: ['爱情', '恋爱', '婚姻', '关系', '情感'],
  },
  CAREER: {
    id: 'career',
    name: '事业工作',
    icon: '💼',
    color: '#4169E1',
    description: '关于工作、事业、职场发展的问题',
    keywords: ['工作', '事业', '职场', '升职', '跳槽'],
  },
  FINANCE: {
    id: 'finance',
    name: '财运投资',
    icon: '💰',
    color: '#FFD700',
    description: '关于金钱、投资、财务状况的问题',
    keywords: ['金钱', '投资', '理财', '财运', '收入'],
  },
  HEALTH: {
    id: 'health',
    name: '健康养生',
    icon: '🏥',
    color: '#32CD32',
    description: '关于身体健康、心理状态的问题',
    keywords: ['健康', '身体', '心理', '疾病', '康复'],
  },
  GENERAL: {
    id: 'general',
    name: '综合运势',
    icon: '🔮',
    color: '#9370DB',
    description: '综合性问题或不确定分类的问题',
    keywords: ['运势', '命运', '未来', '综合', '整体'],
  },
} as const;

// 本地存储键名常量
export const STORAGE_KEYS = {
  AUTH_TOKEN: 'auth_token',
  USER_DATA: 'user_data',
  PREFERENCES: 'user_preferences',
  GAME_CACHE: 'game_cache',
  FORM_CACHE: 'form_cache',
  THEME: 'theme_preference',
  LANGUAGE: 'language_preference',
  READING_HISTORY: 'reading_history',
  FAVORITE_CARDS: 'favorite_cards',
  RECENT_SPREADS: 'recent_spreads',
} as const;

// 动画持续时间常量
export const ANIMATIONS = {
  FAST: 200,
  NORMAL: 300,
  SLOW: 500,
  VERY_SLOW: 800,
  CARD_FLIP: 1000,
  PARTICLE_FADE: 1500,
  PAGE_TRANSITION: 400,
  MODAL_FADE: 300,
  TOOLTIP_FADE: 150,
} as const;

// 断点常量
export const BREAKPOINTS = {
  XS: 0,
  SM: 600,
  MD: 900,
  LG: 1200,
  XL: 1536,
} as const;

// Z-Index 层级常量
export const Z_INDEX = {
  BACKGROUND: -1,
  BASE: 0,
  DROPDOWN: 1000,
  STICKY: 1020,
  FIXED: 1030,
  MODAL_BACKDROP: 1040,
  MODAL: 1050,
  POPOVER: 1060,
  TOOLTIP: 1070,
  NOTIFICATION: 9999,
} as const;

// 错误消息常量
export const ERROR_MESSAGES = {
  NETWORK_ERROR: '网络连接异常，请检查您的网络设置',
  AUTH_REQUIRED: '请先登录后再进行此操作',
  PERMISSION_DENIED: '您没有执行此操作的权限',
  INVALID_INPUT: '输入的数据格式不正确',
  SERVER_ERROR: '服务器暂时无法响应，请稍后重试',
  VALIDATION_FAILED: '数据验证失败，请检查输入内容',
  TOKEN_EXPIRED: '登录已过期，请重新登录',
  RATE_LIMIT: '操作过于频繁，请稍后再试',
  RESOURCE_NOT_FOUND: '请求的资源不存在',
  FILE_TOO_LARGE: '文件大小超出限制',
  UNSUPPORTED_FORMAT: '不支持的文件格式',
} as const;

// 成功消息常量
export const SUCCESS_MESSAGES = {
  LOGIN_SUCCESS: '登录成功！欢迎回来',
  REGISTER_SUCCESS: '注册成功！欢迎加入塔罗世界',
  PROFILE_UPDATED: '个人信息更新成功',
  READING_SAVED: '占卜记录已保存',
  SETTINGS_SAVED: '设置已保存',
  CARD_ADDED_TO_FAVORITES: '已添加到收藏',
  CARD_REMOVED_FROM_FAVORITES: '已从收藏中移除',
  DATA_EXPORTED: '数据导出成功',
  DATA_IMPORTED: '数据导入成功',
} as const;

// 配置常量
export const CONFIG = {
  MAX_FILE_SIZE: 10 * 1024 * 1024, // 10MB
  SUPPORTED_IMAGE_FORMATS: ['jpg', 'jpeg', 'png', 'gif', 'webp'],
  PAGINATION: {
    DEFAULT_PAGE_SIZE: 10,
    MAX_PAGE_SIZE: 100,
  },
  DEBOUNCE_DELAY: 300,
  API_TIMEOUT: 10000,
  RETRY_ATTEMPTS: 3,
  CACHE_DURATION: 5 * 60 * 1000, // 5分钟
} as const;

// 默认值常量
export const DEFAULTS = {
  AVATAR_URL: '/images/default-avatar.png',
  CARD_BACK_IMAGE: '/images/card-back.jpg',
  CARD_PLACEHOLDER: '/images/card-placeholder.jpg',
  USER_PREFERENCES: {
    theme: 'dark',
    language: 'zh-CN',
    fontSize: 'medium',
    soundEnabled: true,
    animationsEnabled: true,
    autoSave: true,
    tarotDeckStyle: 'mystical',
  },
  GAME_CACHE: {
    favoriteCards: [],
    recentSpreads: [],
    completedReadings: 0,
  },
} as const;

// 正则表达式常量
export const REGEX = {
  EMAIL: /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/,
  PHONE: /^1[3-9]\d{9}$/,
  PASSWORD: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)[a-zA-Z\d@$!%*?&]{8,}$/,
  USERNAME: /^[a-zA-Z0-9_]{3,16}$/,
  CHINESE_NAME: /^[\u4e00-\u9fa5]{2,8}$/,
  URL: /^https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)$/,
  HEX_COLOR: /^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$/,
} as const;

export default {
  API,
  ROUTES,
  COLORS,
  TAROT,
  SPREADS,
  QUESTION_TYPES,
  STORAGE_KEYS,
  ANIMATIONS,
  BREAKPOINTS,
  Z_INDEX,
  ERROR_MESSAGES,
  SUCCESS_MESSAGES,
  CONFIG,
  DEFAULTS,
  REGEX,
};
