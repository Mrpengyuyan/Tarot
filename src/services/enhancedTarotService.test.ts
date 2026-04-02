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

  it('requests /health/ai with validateStatus to preserve degraded payload', async () => {
    mockApiGet.mockResolvedValue({
      status: 'degraded',
      is_healthy: false,
      is_configured: true,
      message: 'upstream unavailable',
    });

    await enhancedTarotService.checkAIServiceHealth();

    expect(mockApiGet).toHaveBeenCalledWith(
      '/health/ai',
      expect.objectContaining({
        validateStatus: expect.any(Function),
      }),
    );

    const validateStatus = mockApiGet.mock.calls[0][1].validateStatus as (status: number) => boolean;
    expect(validateStatus(200)).toBe(true);
    expect(validateStatus(503)).toBe(true);
    expect(validateStatus(600)).toBe(false);
  });
});
