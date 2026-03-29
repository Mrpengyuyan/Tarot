import { CozeService } from './cozeService';

describe('CozeService browser secret guard', () => {
  let warnSpy: jest.SpyInstance;

  beforeEach(() => {
    warnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});
  });

  afterEach(() => {
    warnSpy.mockRestore();
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
});
