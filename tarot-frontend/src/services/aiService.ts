import { api } from './api';

// AI解读相关类型定义
export interface AIInterpretationRequest {
  question: string;
  spread_name: string;
  cards_drawn: Array<{
    card_name: string;
    position: string;
    is_upright: boolean;
  }>;
}

export interface AIInterpretationResponse {
  reading_id: string;
  question: string;
  spread_type: string;
  interpretation: {
    overview: string;
    card_details: Array<{
      position: string;
      card_name: string;
      meaning: string;
      advice: string;
    }>;
    card_connections: string;
    core_insights: string[];
    action_recommendations: {
      immediate: string[];
      medium_term: string[];
      avoid: string[];
      enhance: string[];
    };
  };
  conclusion: string;
  timestamp?: number;
}

export interface PredictionStartRequest {
  spread_id: number;
  question: string;
  question_type: 'love' | 'career' | 'finance' | 'health' | 'general';
}

export interface PredictionResult {
  prediction_id: number;
  question: string;
  spread: {
    id: number;
    name: string;
    description: string;
  };
  cards_drawn: Array<{
    card: {
      id: number;
      name: string;
      description: string;
      upright_meaning: string;
      reversed_meaning: string;
      image_url: string;
    };
    position: string;
    is_upright: boolean;
    meaning: string;
  }>;
  interpretation: AIInterpretationResponse;
  created_at: string;
}

export interface AIHealthCheck {
  status: string;
  message: string;
  is_healthy: boolean;
  is_configured: boolean;
  details?: any;
}

/**
 * AI解读服务
 * 处理塔罗牌的AI解读功能
 */
export class AIService {

  /**
   * 开始完整的塔罗占卜流程
   * 包括抽牌和AI解读
   */
  async startReading(request: PredictionStartRequest): Promise<PredictionResult> {
    throw new Error('该接口已弃用，请使用记录式占卜流程');
  }

  /**
   * 获取AI解读
   * 用于对已有牌面进行解读
   */
  async getInterpretation(request: AIInterpretationRequest): Promise<AIInterpretationResponse> {
    throw new Error('该接口已弃用，请使用记录式占卜流程');
  }

  /**
   * 检查AI服务健康状态
   */
  async checkHealth(): Promise<AIHealthCheck> {
    try {
      const response: any = await api.get('/health/ai');
      const isConfigured = Boolean(
        response?.coze_configured ?? response?.is_configured ?? response?.configured
      );
      const isHealthy = Boolean(
        response?.coze_healthy ?? response?.is_healthy ?? response?.status === 'healthy'
      );

      return {
        status: response?.status || 'unknown',
        message: response?.message || '',
        is_healthy: isHealthy,
        is_configured: isConfigured,
        details: response?.details || response
      };
    } catch (error) {
      console.error('AI服务健康检查失败:', error);
      return {
        status: 'error',
        message: '无法连接到AI服务',
        is_healthy: false,
        is_configured: false,
      };
    }
  }

  /**
   * 重新解读已有占卜
   */
  async reinterpret(predictionId: number): Promise<AIInterpretationResponse> {
    throw new Error('该接口已弃用，请使用记录式占卜流程');
  }

  /**
   * 批量获取历史占卜
   */
  async getHistory(skip: number = 0, limit: number = 20): Promise<any[]> {
    throw new Error('该接口已弃用，请改用 /records');
  }

  /**
   * 获取占卜详情
   */
  async getPredictionDetail(predictionId: number): Promise<PredictionResult> {
    throw new Error('该接口已弃用，请改用 /records');
  }

  /**
   * 格式化AI解读文本
   */
  formatInterpretation(interpretation: AIInterpretationResponse): string {
    let formatted = `## 🔮 ${interpretation.question}\n\n`;

    if (interpretation.interpretation.overview) {
      formatted += `### 💫 整体运势\n${interpretation.interpretation.overview}\n\n`;
    }

    if (interpretation.interpretation.card_details?.length > 0) {
      formatted += `### 🃏 逐牌解析\n`;
      interpretation.interpretation.card_details.forEach((card, index) => {
        formatted += `**${index + 1}. ${card.position} - ${card.card_name}**\n`;
        formatted += `${card.meaning}\n`;
        if (card.advice) {
          formatted += `💡 *${card.advice}*\n`;
        }
        formatted += `\n`;
      });
    }

    if (interpretation.interpretation.card_connections) {
      formatted += `### ⚡ 牌间关系\n${interpretation.interpretation.card_connections}\n\n`;
    }

    if (interpretation.interpretation.core_insights?.length > 0) {
      formatted += `### 💡 核心洞察\n`;
      interpretation.interpretation.core_insights.forEach(insight => {
        formatted += `• ${insight}\n`;
      });
      formatted += `\n`;
    }

    if (interpretation.interpretation.action_recommendations) {
      const rec = interpretation.interpretation.action_recommendations;
      formatted += `### 📋 行动建议\n`;

      if (rec.immediate?.length > 0) {
        formatted += `**立即行动:**\n`;
        rec.immediate.forEach(action => formatted += `• ${action}\n`);
        formatted += `\n`;
      }

      if (rec.medium_term?.length > 0) {
        formatted += `**中期规划:**\n`;
        rec.medium_term.forEach(action => formatted += `• ${action}\n`);
        formatted += `\n`;
      }

      if (rec.enhance?.length > 0) {
        formatted += `**增强方面:**\n`;
        rec.enhance.forEach(action => formatted += `• ${action}\n`);
        formatted += `\n`;
      }

      if (rec.avoid?.length > 0) {
        formatted += `**需要避免:**\n`;
        rec.avoid.forEach(action => formatted += `• ${action}\n`);
        formatted += `\n`;
      }
    }

    if (interpretation.conclusion) {
      formatted += `### ✨ 温馨提醒\n${interpretation.conclusion}\n`;
    }

    return formatted;
  }

  /**
   * 检查是否支持AI功能
   */
  async isAIAvailable(): Promise<boolean> {
    try {
      const health = await this.checkHealth();
      return health.is_healthy && health.is_configured;
    } catch (error) {
      return false;
    }
  }

  /**
   * 获取AI服务状态描述
   */
  async getAIStatusDescription(): Promise<string> {
    try {
      const health = await this.checkHealth();

      if (health.is_healthy && health.is_configured) {
        return '🟢 AI解读服务正常';
      } else if (health.is_configured) {
        return '🟡 AI服务已配置但不可用';
      } else {
        return '🔴 AI服务未配置';
      }
    } catch (error) {
      return '❌ AI服务连接失败';
    }
  }
}

// 创建全局实例
export const aiService = new AIService();

// 默认导出
export default aiService;
