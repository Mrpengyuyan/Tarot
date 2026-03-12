/**
 * Coze服务前端封装
 * 提供与Coze AI的直接通信能力（可选功能）
 */

// Coze配置类型
export interface CozeConfig {
  apiKey?: string;
  botId?: string;
  baseUrl?: string;
  timeout?: number;
}

// Coze消息类型
export interface CozeMessage {
  role: 'user' | 'assistant' | 'system';
  content: string;
  type?: 'text' | 'image' | 'file';
}

// Coze对话请求
export interface CozeConversationRequest {
  message: string;
  user_id?: string;
  conversation_id?: string;
  stream?: boolean;
}

// Coze对话响应
export interface CozeConversationResponse {
  conversation_id: string;
  chat_id: string;
  messages: CozeMessage[];
  status: 'success' | 'failed' | 'pending';
  error?: string;
}

/**
 * Coze服务类（前端版本）
 * 注意：由于浏览器CORS限制，通常不直接从前端调用Coze API
 * 这个服务主要用于配置管理和状态检查
 */
export class CozeService {
  private config: CozeConfig;

  constructor(config: CozeConfig = {}) {
    this.config = {
      baseUrl: 'https://api.coze.cn',
      timeout: 30000,
      ...config,
    };
  }

  /**
   * 更新配置
   */
  updateConfig(config: Partial<CozeConfig>): void {
    this.config = { ...this.config, ...config };
  }

  /**
   * 获取当前配置
   */
  getConfig(): CozeConfig {
    return { ...this.config };
  }

  /**
   * 检查配置是否完整
   */
  isConfigured(): boolean {
    return !!(this.config.apiKey && this.config.botId);
  }

  /**
   * 获取配置状态描述
   */
  getConfigStatus(): string {
    if (!this.config.apiKey) {
      return '❌ 缺少API密钥';
    }
    if (!this.config.botId) {
      return '❌ 缺少Bot ID';
    }
    return '✅ 配置完整';
  }

  /**
   * 验证API密钥格式
   */
  validateApiKey(apiKey: string): boolean {
    // Coze API密钥通常以pat_开头
    return apiKey.startsWith('pat_') && apiKey.length > 20;
  }

  /**
   * 验证Bot ID格式
   */
  validateBotId(botId: string): boolean {
    // Bot ID通常是数字字符串
    return /^\d+$/.test(botId) && botId.length > 5;
  }

  /**
   * 构建请求头
   */
  private buildHeaders(): HeadersInit {
    const headers: HeadersInit = {
      'Content-Type': 'application/json',
    };

    if (this.config.apiKey) {
      headers['Authorization'] = `Bearer ${this.config.apiKey}`;
    }

    return headers;
  }

  /**
   * 前端健康检查（通过后端代理）
   * 由于CORS限制，前端不能直接调用Coze API
   */
  async healthCheck(): Promise<{
    status: string;
    message: string;
    is_healthy: boolean;
    details?: any;
  }> {
    // 这里实际上是调用我们自己的后端接口
    try {
      const response = await fetch('/api/v1/health/ai', {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const result = await response.json();
      return result;
    } catch (error) {
      console.error('Coze健康检查失败:', error);
      return {
        status: 'error',
        message: '无法连接到Coze服务',
        is_healthy: false,
      };
    }
  }

  /**
   * 获取Coze服务信息
   */
  getServiceInfo(): {
    name: string;
    version: string;
    description: string;
    capabilities: string[];
  } {
    return {
      name: 'Coze AI Service',
      version: '1.0.0',
      description: '专业的塔罗占卜师AI，提供深度解读服务',
      capabilities: [
        '塔罗牌解读',
        '多语言支持',
        '上下文理解',
        '个性化建议',
      ],
    };
  }

  /**
   * 格式化Coze消息为显示文本
   */
  formatMessage(message: CozeMessage): string {
    const roleMap = {
      user: '🧑 用户',
      assistant: '🤖 AI',
      system: '⚙️ 系统',
    };

    const role = roleMap[message.role] || message.role;
    return `${role}: ${message.content}`;
  }

  /**
   * 解析Coze错误信息
   */
  parseError(error: any): string {
    if (typeof error === 'string') {
      return error;
    }

    if (error?.message) {
      return error.message;
    }

    if (error?.detail) {
      return error.detail;
    }

    if (error?.error) {
      return error.error;
    }

    return '未知错误';
  }

  /**
   * 生成Coze配置指南
   */
  getConfigurationGuide(): {
    title: string;
    steps: Array<{
      step: number;
      title: string;
      description: string;
      action?: string;
    }>;
  } {
    return {
      title: 'Coze AI 配置指南',
      steps: [
        {
          step: 1,
          title: '注册Coze账户',
          description: '访问 coze.cn 注册账户',
          action: 'https://coze.cn',
        },
        {
          step: 2,
          title: '创建AI应用',
          description: '在控制台创建新的AI应用/Bot',
        },
        {
          step: 3,
          title: '获取API密钥',
          description: '在应用设置中生成API密钥（以pat_开头）',
        },
        {
          step: 4,
          title: '获取Bot ID',
          description: '复制Bot的唯一标识符（数字）',
        },
        {
          step: 5,
          title: '配置环境变量',
          description: '将API密钥和Bot ID配置到后端环境变量中',
        },
      ],
    };
  }

  /**
   * 创建测试消息
   */
  createTestMessage(): CozeConversationRequest {
    return {
      message: '你好，请回复收到',
      user_id: 'test_user',
      stream: false,
    };
  }

  /**
   * 创建塔罗解读消息
   */
  createTarotMessage(
    question: string,
    spreadName: string,
    cards: Array<{ name: string; position: string; reversed: boolean }>
  ): CozeConversationRequest {
    const cardsText = cards
      .map(card => `${card.name}(${card.reversed ? '逆位' : '正位'}, ${card.position})`)
      .join(', ');

    const message = `占卜问题：${question}
使用牌阵：${spreadName}
抽取牌面：${cardsText}

请根据以上信息提供专业的塔罗占卜解读。`;

    return {
      message,
      user_id: 'tarot_user',
      stream: false,
    };
  }
}

// 创建默认实例
export const cozeService = new CozeService();

// 默认导出
export default cozeService;
