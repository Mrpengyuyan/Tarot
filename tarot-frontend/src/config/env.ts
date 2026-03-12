/**
 * Frontend environment configuration.
 */

export interface AppConfig {
  apiBaseUrl: string;
  coze: {
    apiUrl: string;
    botId: string;
    accessToken: string;
  };
  app: {
    name: string;
    version: string;
    description: string;
  };
  features: {
    enable3D: boolean;
    enableAnimations: boolean;
    enableSound: boolean;
    debugMode: boolean;
  };
  services: {
    googleAnalyticsId?: string;
    sentryDsn?: string;
  };
}

const getEnvVar = (key: string, defaultValue = ''): string => {
  return process.env[key] || defaultValue;
};

const getEnvBool = (key: string, defaultValue = false): boolean => {
  const value = process.env[key];
  if (!value) return defaultValue;
  return value.toLowerCase() === 'true';
};

export const config: AppConfig = {
  apiBaseUrl: getEnvVar('REACT_APP_API_BASE_URL', 'http://localhost:8000/api/v1'),
  coze: {
    apiUrl: getEnvVar('REACT_APP_COZE_API_URL', 'https://api.coze.cn/open_api/v2'),
    botId: getEnvVar('REACT_APP_COZE_BOT_ID'),
    accessToken: getEnvVar('REACT_APP_COZE_ACCESS_TOKEN'),
  },
  app: {
    name: getEnvVar('REACT_APP_NAME', 'Tarot Game'),
    version: getEnvVar('REACT_APP_VERSION', '1.0.0'),
    description: getEnvVar('REACT_APP_DESCRIPTION', 'AI-powered tarot reading game'),
  },
  features: {
    enable3D: getEnvBool('REACT_APP_ENABLE_3D', true),
    enableAnimations: getEnvBool('REACT_APP_ENABLE_ANIMATIONS', true),
    enableSound: getEnvBool('REACT_APP_ENABLE_SOUND', false),
    debugMode: getEnvBool('REACT_APP_DEBUG_MODE', false),
  },
  services: {
    googleAnalyticsId: getEnvVar('REACT_APP_GOOGLE_ANALYTICS_ID'),
    sentryDsn: getEnvVar('REACT_APP_SENTRY_DSN'),
  },
};

export const validateConfig = (): { isValid: boolean; errors: string[] } => {
  const errors: string[] = [];

  if (!config.apiBaseUrl) {
    errors.push('API base URL is missing');
  }

  return {
    isValid: errors.length === 0,
    errors,
  };
};

export const isDevelopment = process.env.NODE_ENV === 'development';
export const isProduction = process.env.NODE_ENV === 'production';
export const isTest = process.env.NODE_ENV === 'test';

export const debugLog = (...args: any[]) => {
  if (config.features.debugMode || isDevelopment) {
    console.log('[DEBUG]', ...args);
  }
};

export const buildApiUrl = (endpoint: string): string => {
  const baseUrl = config.apiBaseUrl.replace(/\/$/, '');
  const cleanEndpoint = endpoint.replace(/^\//, '');
  return `${baseUrl}/${cleanEndpoint}`;
};

if (isDevelopment) {
  const validation = validateConfig();
  if (!validation.isValid) {
    console.warn('[Config] validation failed:', validation.errors);
  }
}

export default config;
