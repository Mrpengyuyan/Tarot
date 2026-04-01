import { api } from './api';
import { enhancedTarotService } from './enhancedTarotService';

jest.mock('./api', () => ({
  api: {
    get: jest.fn(),
  },
}));

describe('enhancedTarotService health mapping', () => {
  const mockApiGet = api.get as jest.Mock;
  let errorSpy: jest.SpyInstance;

  beforeEach(() => {
    mockApiGet.mockReset();
    errorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    errorSpy.mockRestore();
  });

  it('returns explicit is_configured from backend in health payload', async () => {
    mockApiGet.mockResolvedValue({
      status: 'degraded',
      is_healthy: false,
      is_configured: true,
      message: 'upstream unavailable',
    });

    const health = await enhancedTarotService.checkAIServiceHealth();

    expect(health.status).toBe('degraded');
    expect(health.is_healthy).toBe(false);
    expect(health.is_configured).toBe(true);
  });

  it('validateAIConfiguration uses configuration status instead of health status', async () => {
    mockApiGet.mockResolvedValue({
      status: 'degraded',
      is_healthy: false,
      is_configured: true,
      message: 'provider timeout',
    });

    const result = await enhancedTarotService.validateAIConfiguration();

    expect(result.isConfigured).toBe(true);
    expect(result.status).toBe('degraded');
    expect(result.message).toContain('provider timeout');
  });
});
