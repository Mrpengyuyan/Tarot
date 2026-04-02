import { aiService } from './aiService';
import { api } from './api';

jest.mock('./api', () => ({
  api: {
    get: jest.fn(),
  },
}));

describe('aiService health contract handling', () => {
  const mockApiGet = api.get as jest.Mock;
  let errorSpy: jest.SpyInstance;

  beforeEach(() => {
    mockApiGet.mockReset();
    errorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    errorSpy.mockRestore();
  });

  it('infers configured=true when backend omits is_configured but status is healthy', async () => {
    mockApiGet.mockResolvedValue({
      status: 'healthy',
      is_healthy: true,
      message: 'ok',
      details: { model_used: 'deepseek-chat' },
    });

    const health = await aiService.checkHealth();

    expect(health.is_healthy).toBe(true);
    expect(health.is_configured).toBe(true);
    expect(await aiService.isAIAvailable()).toBe(true);
  });

  it('infers configured=false when status is not_configured', async () => {
    mockApiGet.mockResolvedValue({
      status: 'not_configured',
      is_healthy: false,
      message: 'missing credentials',
    });

    const health = await aiService.checkHealth();

    expect(health.is_configured).toBe(false);
    expect(health.is_healthy).toBe(false);
    expect(await aiService.isAIAvailable()).toBe(false);
  });

  it('honors explicit is_configured when backend provides it', async () => {
    mockApiGet.mockResolvedValue({
      status: 'degraded',
      is_healthy: false,
      is_configured: true,
      message: 'temporary upstream failure',
    });

    const health = await aiService.checkHealth();

    expect(health.is_configured).toBe(true);
    expect(health.is_healthy).toBe(false);
    expect(await aiService.isAIAvailable()).toBe(false);
  });

  it('requests /health/ai with non-2xx-pass-through validateStatus', async () => {
    mockApiGet.mockResolvedValue({
      status: 'degraded',
      is_healthy: false,
      is_configured: true,
      message: 'upstream timeout',
    });

    await aiService.checkHealth();

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
