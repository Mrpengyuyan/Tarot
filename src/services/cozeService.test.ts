import { CozeService } from './cozeService';

describe('CozeService browser secret guard', () => {
  let warnSpy: jest.SpyInstance;
  let errorSpy: jest.SpyInstance;
  let fetchMock: jest.Mock;

  beforeEach(() => {
    warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});
    errorSpy = jest.spyOn(console, 'error').mockImplementation(() => {});
    fetchMock = jest.fn();
    (global as any).fetch = fetchMock;
  });

  afterEach(() => {
    warnSpy.mockRestore();
    errorSpy.mockRestore();
  });

  it('drops apiKey provided in constructor', () => {
    const service = new CozeService({
      apiKey: 'pat_secret_should_not_be_kept',
      botId: '123456',
    });

    expect(service.getConfig().apiKey).toBeUndefined();
    expect(service.isConfigured()).toBe(true);
    expect(warnSpy).toHaveBeenCalled();
  });

  it('drops apiKey provided by updateConfig', () => {
    const service = new CozeService({ botId: '123456' });

    service.updateConfig({ apiKey: 'pat_runtime_secret' });

    expect(service.getConfig().apiKey).toBeUndefined();
    expect(service.isConfigured()).toBe(true);
    expect(warnSpy).toHaveBeenCalled();
  });

  it('reports config status by bot id only', () => {
    const service = new CozeService();
    expect(service.isConfigured()).toBe(false);
    expect(service.getConfigStatus()).toContain('Bot ID');

    service.updateConfig({ botId: '654321' });

    expect(service.isConfigured()).toBe(true);
    expect(service.getConfigStatus()).not.toContain('Bot ID');
  });

  it('uses configured api base URL for health check path', async () => {
    fetchMock.mockResolvedValue({
      ok: true,
      status: 200,
      json: async () => ({ status: 'healthy', is_healthy: true }),
    } as any);

    const service = new CozeService();
    await service.healthCheck();

    expect(fetchMock).toHaveBeenCalledWith(
      'http://localhost:8000/api/v1/health/ai',
      expect.objectContaining({ method: 'GET' }),
    );
  });

  it('returns backend payload even when health endpoint is non-2xx', async () => {
    fetchMock.mockResolvedValue({
      ok: false,
      status: 503,
      json: async () => ({
        status: 'degraded',
        message: 'AI service unavailable',
        is_healthy: false,
      }),
    } as any);

    const service = new CozeService();
    const result = await service.healthCheck();

    expect(result.status).toBe('degraded');
    expect(result.message).toBe('AI service unavailable');
    expect(result.is_healthy).toBe(false);
  });
});
