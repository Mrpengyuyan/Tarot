/**
 * 增强版塔罗服务
 * 集成AI解读功能，支持不同牌阵的智能占卜
 */

import { api } from './api';
import { TarotCard, SpreadType } from '../types/api';

// 抽牌相关类型定义
export interface DrawnCard {
  card: TarotCard;
  isReversed: boolean;
  position: number;
  positionName?: string;
  positionMeaning?: string;
}

export interface CreateReadingParams {
  spreadId: number;
  question: string;
  questionType: 'love' | 'career' | 'finance' | 'health' | 'general';
  userContext?: string;
}

export interface Reading {
  id: number;
  user_id: number;
  spread_id: number;
  question: string;
  question_type: string;
  status: string;
  created_at: string;
  updated_at: string;
}

export interface EnhancedReadingResult {
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
  interpretation: {
    ai_interpretation?: any;
    summary?: string;
  };
  status: string;
  created_at: string;
}

export interface AIInterpretationResult {
  reading_id: string;
  timestamp: number;
  question: string;
  question_type: string;
  spread_type: string;
  spread_info: {
    id: number;
    name: string;
    description: string;
    card_count: number;
  };
  cards_drawn: Array<{
    name: string;
    position: string;
    orientation: string;
    meaning: string;
    advice: string;
  }>;
  interpretation: {
    overview: string;
    card_details: Array<{
      card_name: string;
      position: string;
      orientation: string;
      meaning: string;
      advice: string;
    }>;
    card_connections: string;
    spread_analysis: string;
    core_insights: string[];
    action_recommendations: {
      immediate: string[];
      medium_term: string[];
      avoid: string[];
      enhance: string[];
    };
    timing_guidance: string;
  };
  conclusion: string;
}

export const enhancedTarotService = {
  // 开始新的塔罗占卜（完整流程：抽牌 + AI解读）
  startNewReading: async (params: CreateReadingParams): Promise<EnhancedReadingResult> => {
    throw new Error('该接口已弃用，请使用记录式占卜流程');
  },

  // 单独获取AI解读
  getAIInterpretation: async (interpretationRequest: {
    question: string;
    spread_name: string;
    cards_drawn: Array<{
      card_name: string;
      position: string;
      is_upright: boolean;
    }>;
    question_type?: string;
    user_context?: string;
  }): Promise<AIInterpretationResult> => {
    throw new Error('该接口已弃用，请使用记录式占卜流程');
  },

  // 检查AI服务健康状态
  checkAIServiceHealth: async (): Promise<{
    status: string;
    message: string;
    is_healthy: boolean;
    details?: any;
  }> => {
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
        details: response?.details || response,
      };
    } catch (error) {
      console.error('检查AI服务健康状态失败:', error);
      throw error;
    }
  },

  // 获取占卜历史
  getReadingHistory: async (skip = 0, limit = 20): Promise<Reading[]> => {
    try {
      return await api.get(`/records/?skip=${skip}&limit=${limit}`);
    } catch (error) {
      console.error('获取占卜历史失败:', error);
      throw error;
    }
  },

  // 获取占卜详情
  getReadingDetail: async (predictionId: number): Promise<EnhancedReadingResult> => {
    try {
      return await api.get(`/records/${predictionId}`);
    } catch (error) {
      console.error('获取占卜详情失败:', error);
      throw error;
    }
  },

  // 获取所有塔罗牌
  getAllCards: async (): Promise<TarotCard[]> => {
    try {
      return await api.get('/cards/');
    } catch (error) {
      console.error('获取塔罗牌失败:', error);
      throw error;
    }
  },

  // 获取塔罗牌详情
  getCardDetail: async (cardId: number): Promise<TarotCard> => {
    try {
      return await api.get(`/cards/${cardId}`);
    } catch (error) {
      console.error('获取塔罗牌详情失败:', error);
      throw error;
    }
  },

  // 随机抽牌
  drawRandomCards: async (params: {
    count: number;
    excludeIds?: number[];
  }): Promise<TarotCard[]> => {
    try {
      const queryParams = new URLSearchParams({
        count: params.count.toString(),
      });

      if (params.excludeIds && params.excludeIds.length > 0) {
        params.excludeIds.forEach(id => {
          queryParams.append('exclude_ids', id.toString());
        });
      }

      return await api.get(`/cards/draw?${queryParams.toString()}`);
    } catch (error) {
      console.error('随机抽牌失败:', error);
      throw error;
    }
  },

  // 获取所有牌阵
  getAllSpreads: async (): Promise<SpreadType[]> => {
    try {
      return await api.get('/spreads/');
    } catch (error) {
      console.error('获取牌阵失败:', error);
      throw error;
    }
  },

  // 获取牌阵详情
  getSpreadDetail: async (spreadId: number): Promise<SpreadType> => {
    try {
      return await api.get(`/spreads/${spreadId}`);
    } catch (error) {
      console.error('获取牌阵详情失败:', error);
      throw error;
    }
  },

  // 获取热门牌阵
  getPopularSpreads: async (limit = 10): Promise<SpreadType[]> => {
    try {
      return await api.get(`/spreads/popular?limit=${limit}`);
    } catch (error) {
      console.error('获取热门牌阵失败:', error);
      throw error;
    }
  },

  // 根据问题类型获取适合的牌阵
  getSpreadsByQuestionType: async (
    questionType: string,
    skip = 0,
    limit = 50
  ): Promise<SpreadType[]> => {
    try {
      return await api.get(`/spreads/by-question-type/${questionType}?skip=${skip}&limit=${limit}`);
    } catch (error) {
      console.error('获取适合牌阵失败:', error);
      throw error;
    }
  },

  // 格式化AI解读结果为显示文本
  formatInterpretationForDisplay: (interpretation: AIInterpretationResult): {
    overview: string;
    cardAnalysis: string[];
    insights: string[];
    recommendations: {
      immediate: string[];
      medium: string[];
      avoid: string[];
      enhance: string[];
    };
    conclusion: string;
  } => {
    const { interpretation: aiInterpretation, conclusion } = interpretation;

    return {
      overview: aiInterpretation.overview || '暂无整体解读',
      cardAnalysis: aiInterpretation.card_details?.map(
        card => `${card.card_name}(${card.position}): ${card.meaning}`
      ) || [],
      insights: aiInterpretation.core_insights || [],
      recommendations: {
        immediate: aiInterpretation.action_recommendations?.immediate || [],
        medium: aiInterpretation.action_recommendations?.medium_term || [],
        avoid: aiInterpretation.action_recommendations?.avoid || [],
        enhance: aiInterpretation.action_recommendations?.enhance || []
      },
      conclusion: conclusion || '感谢您的咨询，愿塔罗为您带来智慧与指引。'
    };
  },

  // 创建占卜请求（用于自定义牌阵）
  createCustomReading: async (
    question: string,
    questionType: string,
    spreadName: string,
    cards: DrawnCard[],
    userContext?: string
  ): Promise<AIInterpretationResult> => {
    try {
      const cardsForAI = cards.map(card => ({
        card_name: card.card.name_zh || card.card.name_en,
        position: card.positionName || `位置${card.position}`,
        is_upright: !card.isReversed
      }));

      return await enhancedTarotService.getAIInterpretation({
        question,
        spread_name: spreadName,
        cards_drawn: cardsForAI,
        question_type: questionType,
        user_context: userContext
      });
    } catch (error) {
      console.error('创建自定义占卜失败:', error);
      throw error;
    }
  },

  // 验证AI服务配置
  validateAIConfiguration: async (): Promise<{
    isConfigured: boolean;
    status: string;
    message: string;
  }> => {
    try {
      const healthCheck = await enhancedTarotService.checkAIServiceHealth();
      return {
        isConfigured: healthCheck.is_healthy,
        status: healthCheck.status,
        message: healthCheck.message
      };
    } catch (error) {
      return {
        isConfigured: false,
        status: 'error',
        message: '无法连接到AI服务'
      };
    }
  }
};

export default enhancedTarotService;
