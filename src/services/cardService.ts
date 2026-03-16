import { api } from './api';
import { TarotCard } from '../types/api';

/**
 * 塔罗牌相关API服务
 */
export class CardService {

  /**
   * 获取所有塔罗牌
   */
  async getAllCards(): Promise<TarotCard[]> {
    try {
      const response = await api.get('/cards/');
      return response as unknown as TarotCard[];
    } catch (error) {
      console.error('获取塔罗牌失败:', error);
      throw new Error('获取塔罗牌数据失败');
    }
  }

  /**
   * 获取塔罗牌详情
   */
  async getCardDetail(cardId: number): Promise<TarotCard> {
    try {
      const response = await api.get(`/cards/${cardId}`);
      return response as unknown as TarotCard;
    } catch (error) {
      console.error('获取塔罗牌详情失败:', error);
      throw new Error('获取塔罗牌详情失败');
    }
  }

  /**
   * 随机抽牌
   */
  async drawRandomCards(count: number, excludeIds: number[] = []): Promise<TarotCard[]> {
    try {
      const queryParams = new URLSearchParams({
        count: count.toString(),
      });

      if (excludeIds.length > 0) {
        excludeIds.forEach(id => {
          queryParams.append('exclude_ids', id.toString());
        });
      }

      const response = await api.get(`/cards/draw?${queryParams.toString()}`);
      return response as unknown as TarotCard[];
    } catch (error) {
      console.error('随机抽牌失败:', error);
      throw new Error('抽牌失败，请重试');
    }
  }

  /**
   * 获取大阿卡纳牌
   */
  async getMajorArcanaCards(): Promise<TarotCard[]> {
    try {
      const response = await api.get('/cards/major-arcana');
      return response as unknown as TarotCard[];
    } catch (error) {
      console.error('获取大阿卡纳牌失败:', error);
      throw new Error('获取大阿卡纳牌失败');
    }
  }

  /**
   * 获取小阿卡纳牌
   */
  async getMinorArcanaCards(): Promise<TarotCard[]> {
    try {
      const response = await api.get('/cards/minor-arcana');
      return response as unknown as TarotCard[];
    } catch (error) {
      console.error('获取小阿卡纳牌失败:', error);
      throw new Error('获取小阿卡纳牌失败');
    }
  }

  /**
   * 搜索塔罗牌
   */
  async searchCards(query: string, skip: number = 0, limit: number = 50): Promise<TarotCard[]> {
    try {
      const response = await api.get(
        `/cards/search?q=${encodeURIComponent(query)}&skip=${skip}&limit=${limit}`
      );
      return response as unknown as TarotCard[];
    } catch (error) {
      console.error('搜索塔罗牌失败:', error);
      throw new Error('搜索失败');
    }
  }

  /**
   * 按花色获取小阿卡纳牌
   */
  async getCardsBySuit(suit: 'wands' | 'cups' | 'swords' | 'pentacles'): Promise<TarotCard[]> {
    try {
      const cards = await this.getMinorArcanaCards();
      return cards.filter(card => card.suit === suit);
    } catch (error) {
      console.error('按花色获取塔罗牌失败:', error);
      throw new Error('获取花色牌失败');
    }
  }

  /**
   * 按卡牌类型获取牌
   */
  async getCardsByType(type: 'major_arcana' | 'minor_arcana'): Promise<TarotCard[]> {
    try {
      if (type === 'major_arcana') {
        return await this.getMajorArcanaCards();
      } else {
        return await this.getMinorArcanaCards();
      }
    } catch (error) {
      console.error('按类型获取塔罗牌失败:', error);
      throw new Error('获取塔罗牌失败');
    }
  }

  /**
   * 获取随机推荐牌
   */
  async getRandomCard(): Promise<TarotCard> {
    try {
      const cards = await this.drawRandomCards(1);
      if (cards.length === 0) {
        throw new Error('没有获取到卡牌');
      }
      return cards[0];
    } catch (error) {
      console.error('获取随机推荐牌失败:', error);
      throw new Error('获取推荐牌失败');
    }
  }

  /**
   * 验证卡牌ID是否存在
   */
  async validateCardId(cardId: number): Promise<boolean> {
    try {
      await this.getCardDetail(cardId);
      return true;
    } catch (error) {
      return false;
    }
  }

  /**
   * 获取塔罗牌统计信息
   */
  async getCardStats(): Promise<{
    total: number;
    majorArcana: number;
    minorArcana: number;
    suits: {
      wands: number;
      cups: number;
      swords: number;
      pentacles: number;
    };
  }> {
    try {
      const [majorCards, minorCards] = await Promise.all([
        this.getMajorArcanaCards(),
        this.getMinorArcanaCards(),
      ]);

      const suits = {
        wands: minorCards.filter(card => card.suit === 'wands').length,
        cups: minorCards.filter(card => card.suit === 'cups').length,
        swords: minorCards.filter(card => card.suit === 'swords').length,
        pentacles: minorCards.filter(card => card.suit === 'pentacles').length,
      };

      return {
        total: majorCards.length + minorCards.length,
        majorArcana: majorCards.length,
        minorArcana: minorCards.length,
        suits,
      };
    } catch (error) {
      console.error('获取塔罗牌统计失败:', error);
      throw new Error('获取统计信息失败');
    }
  }

  /**
   * 批量获取卡牌详情
   */
  async getCardsByIds(cardIds: number[]): Promise<TarotCard[]> {
    try {
      const promises = cardIds.map(id => this.getCardDetail(id));
      const results = await Promise.allSettled(promises);

      return results
        .filter(result => result.status === 'fulfilled')
        .map(result => (result as PromiseFulfilledResult<TarotCard>).value);
    } catch (error) {
      console.error('批量获取卡牌详情失败:', error);
      throw new Error('批量获取卡牌失败');
    }
  }

  /**
   * 获取卡牌的推荐组合
   */
  async getCardCombinations(cardId: number, count: number = 2): Promise<TarotCard[]> {
    try {
      // 获取当前卡牌详情
      const currentCard = await this.getCardDetail(cardId);

      // 根据卡牌类型推荐相关卡牌
      let excludeIds = [cardId];

      if (currentCard.card_type === 'major_arcana') {
        // 大阿卡纳牌推荐其他大阿卡纳
        const majorCards = await this.getMajorArcanaCards();
        const filtered = majorCards.filter(card => card.id !== cardId);
        return this.shuffleArray(filtered).slice(0, count);
      } else {
        // 小阿卡纳推荐同花色
        if (currentCard.suit) {
          const suitCards = await this.getCardsBySuit(currentCard.suit as any);
          const filtered = suitCards.filter(card => card.id !== cardId);
          return this.shuffleArray(filtered).slice(0, count);
        }
      }

      // 默认随机推荐
      return await this.drawRandomCards(count, excludeIds);
    } catch (error) {
      console.error('获取卡牌组合推荐失败:', error);
      throw new Error('获取推荐组合失败');
    }
  }

  /**
   * 洗牌工具函数
   */
  private shuffleArray<T>(array: T[]): T[] {
    const shuffled = [...array];
    for (let i = shuffled.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [shuffled[i], shuffled[j]] = [shuffled[j], shuffled[i]];
    }
    return shuffled;
  }

  /**
   * 根据关键词匹配卡牌
   */
  async findCardsByKeywords(keywords: string[]): Promise<TarotCard[]> {
    try {
      const allCards = await this.getAllCards();

      return allCards.filter(card => {
        const searchText = `${card.name_zh} ${card.name_en} ${card.description || ''}`.toLowerCase();
        return keywords.some(keyword => searchText.includes(keyword.toLowerCase()));
      });
    } catch (error) {
      console.error('根据关键词查找卡牌失败:', error);
      throw new Error('关键词搜索失败');
    }
  }
}

// 创建全局实例
export const cardService = new CardService();

// 默认导出
export default cardService;