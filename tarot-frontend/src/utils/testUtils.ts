/**
 * Test helpers for API / AI / data connectivity checks.
 */

import { config, debugLog } from '../config/env';
import { api } from '../services/api';
import { aiService } from '../services/aiService';
import { spreadService } from '../services/spreadService';

export const testAPIConnection = async (): Promise<{
  success: boolean;
  message: string;
  details?: any;
}> => {
  try {
    debugLog('Testing API connection...');
    const response = await api.get('/health/');
    return { success: true, message: 'API reachable', details: response };
  } catch (error: any) {
    debugLog('API connection failed:', error);
    return {
      success: false,
      message: `API connection failed: ${error.message || 'unknown error'}`,
      details: error.response?.data || error,
    };
  }
};

export const testAIServiceConnection = async (): Promise<{
  success: boolean;
  message: string;
  details?: any;
}> => {
  try {
    debugLog('Testing AI service health...');
    const healthCheck = await aiService.checkHealth();

    if (!healthCheck.is_healthy) {
      return {
        success: false,
        message: 'AI service is not healthy',
        details: healthCheck,
      };
    }

    return {
      success: true,
      message: 'AI service reachable',
      details: { health: healthCheck },
    };
  } catch (error: any) {
    debugLog('AI service check failed:', error);
    return {
      success: false,
      message: `AI service check failed: ${error.message || 'unknown error'}`,
      details: error,
    };
  }
};

export const testDatabaseConnection = async (): Promise<{
  success: boolean;
  message: string;
  details?: any;
}> => {
  try {
    debugLog('Testing spread data...');
    const spreads = await spreadService.getAllSpreads();

    if (!spreads || spreads.length === 0) {
      return {
        success: false,
        message: 'Database reachable but spread data is empty',
      };
    }

    return {
      success: true,
      message: `Database reachable, found ${spreads.length} spreads`,
      details: {
        spreadCount: spreads.length,
        firstSpread: spreads[0]?.name,
      },
    };
  } catch (error: any) {
    debugLog('Database check failed:', error);
    return {
      success: false,
      message: `Database check failed: ${error.message || 'unknown error'}`,
      details: error,
    };
  }
};

export const runSystemTests = async (): Promise<{
  overall: boolean;
  results: {
    api: Awaited<ReturnType<typeof testAPIConnection>>;
    database: Awaited<ReturnType<typeof testDatabaseConnection>>;
    ai: Awaited<ReturnType<typeof testAIServiceConnection>>;
  };
}> => {
  const results = {
    api: await testAPIConnection(),
    database: await testDatabaseConnection(),
    ai: await testAIServiceConnection(),
  };

  return {
    overall: results.api.success && results.database.success && results.ai.success,
    results,
  };
};

export const validateEnvironment = (): {
  isValid: boolean;
  errors: string[];
  warnings: string[];
} => {
  const errors: string[] = [];
  const warnings: string[] = [];

  if (!config.apiBaseUrl) {
    errors.push('API base URL is missing');
  }

  try {
    new URL(config.apiBaseUrl);
  } catch {
    errors.push('API base URL is invalid');
  }

  if (config.coze.apiUrl) {
    try {
      new URL(config.coze.apiUrl);
    } catch {
      errors.push('Coze API URL is invalid');
    }
  }

  return { isValid: errors.length === 0, errors, warnings };
};

export const getDebugInfo = () => ({
  environment: process.env.NODE_ENV,
  config: {
    apiBaseUrl: config.apiBaseUrl,
    cozeConfigured: !!(config.coze.botId && config.coze.accessToken),
    featuresEnabled: config.features,
  },
  browser: {
    userAgent: navigator.userAgent,
    language: navigator.language,
    cookieEnabled: navigator.cookieEnabled,
  },
  localStorage: {
    hasUserCache: !!localStorage.getItem('user'),
    available: typeof Storage !== 'undefined',
  },
  timestamp: new Date().toISOString(),
});
