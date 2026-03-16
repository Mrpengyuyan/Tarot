import { api } from './api';

export interface Reading {
  id: number;
  user_id: number;
  spread_type_id: number;
  question: string;
  question_type: string;
  status: string;
  created_at: string;
  completed_at?: string;
  is_favorite?: boolean;
  user_notes?: string | null;
  user_rating?: number | null;
}

export interface DrawnCard {
  id: number;
  prediction_id: number;
  tarot_card_id: number;
  position: number;
  is_reversed: boolean;
  drawn_at: string;
  tarot_card?: any;
  card_meaning?: any;
  position_name?: string;
  position_meaning?: string;
}

export interface ReadingWithCards {
  reading: Reading;
  cards: DrawnCard[];
}

export interface CreateReadingParams {
  spreadId: number;
  question: string;
  questionType: 'love' | 'career' | 'finance' | 'health' | 'general';
}

export interface ReadingStats {
  total_predictions: number;
  completed_predictions: number;
  favorite_predictions: number;
  most_used_question_type?: string;
  average_rating?: number | null;
}

export class ReadingService {
  async createReading(params: CreateReadingParams): Promise<Reading> {
    return api.post('/records/', {
      spread_type_id: params.spreadId,
      question: params.question,
      question_type: params.questionType,
    });
  }

  async drawCardsForReading(readingId: number): Promise<{ prediction_id: number; card_draws: DrawnCard[]; status: string }> {
    return api.post(`/records/${readingId}/draw`);
  }

  async getUserReadings(skip = 0, limit = 20): Promise<Reading[]> {
    return api.get(`/records/?skip=${skip}&limit=${limit}`);
  }

  async getReadingDetail(readingId: number): Promise<ReadingWithCards> {
    const [reading, cards] = await Promise.all([
      api.get(`/records/${readingId}`),
      api.get(`/records/${readingId}/cards`),
    ]);

    return {
      reading: reading as unknown as Reading,
      cards: cards as unknown as DrawnCard[],
    };
  }

  async getUserStats(): Promise<ReadingStats> {
    return api.get('/records/stats');
  }

  async getRecentReadings(days = 7, limit = 10): Promise<Reading[]> {
    return api.get(`/records/recent?days=${days}&limit=${limit}`);
  }

  async updateReading(
    readingId: number,
    updates: {
      question?: string;
      user_notes?: string;
      is_favorite?: boolean;
      user_rating?: number;
    }
  ): Promise<Reading> {
    return api.put(`/records/${readingId}`, updates);
  }

  async deleteReading(readingId: number): Promise<void> {
    await api.delete(`/records/${readingId}`);
  }

  async toggleFavorite(readingId: number, isFavorite: boolean): Promise<Reading> {
    return this.updateReading(readingId, { is_favorite: isFavorite });
  }

  async getFavoriteReadings(skip = 0, limit = 20): Promise<Reading[]> {
    return api.get(`/records/?favorites_only=true&skip=${skip}&limit=${limit}`);
  }

  async getReadingsByQuestionType(questionType: string, skip = 0, limit = 20): Promise<Reading[]> {
    return api.get(`/records/?question_type=${encodeURIComponent(questionType)}&skip=${skip}&limit=${limit}`);
  }

  async searchReadings(query: string, skip = 0, limit = 20): Promise<Reading[]> {
    return api.get(`/records/?search=${encodeURIComponent(query)}&skip=${skip}&limit=${limit}`);
  }

  async exportReadings(format: 'json' | 'csv' = 'json'): Promise<Blob> {
    const readings = await this.getUserReadings(0, 500);

    if (format === 'csv') {
      const header = 'id,question,question_type,status,created_at,is_favorite';
      const rows = readings.map((item) => {
        const escapedQuestion = (item.question || '').replace(/"/g, '""');
        return `${item.id},"${escapedQuestion}",${item.question_type},${item.status},${item.created_at},${item.is_favorite ? '1' : '0'}`;
      });
      return new Blob([[header, ...rows].join('\n')], { type: 'text/csv' });
    }

    return new Blob([JSON.stringify(readings, null, 2)], {
      type: 'application/json',
    });
  }

  async getReadingTrends(period: 'week' | 'month' | 'year' = 'month') {
    const days = period === 'week' ? 7 : period === 'month' ? 30 : 365;
    const readings = await this.getRecentReadings(days, 500);

    return {
      period,
      total: readings.length,
      by_status: readings.reduce((acc: Record<string, number>, item) => {
        acc[item.status] = (acc[item.status] || 0) + 1;
        return acc;
      }, {}),
    };
  }

  async batchDeleteReadings(readingIds: number[]): Promise<{ success_count: number; failed_count: number }> {
    let successCount = 0;
    let failedCount = 0;

    for (const id of readingIds) {
      try {
        await this.deleteReading(id);
        successCount += 1;
      } catch {
        failedCount += 1;
      }
    }

    return { success_count: successCount, failed_count: failedCount };
  }

  async getSimilarReadings(readingId: number, limit = 5): Promise<Reading[]> {
    const detail = await this.getReadingDetail(readingId);
    const sameType = await this.getReadingsByQuestionType(detail.reading.question_type, 0, 50);
    return sameType.filter((item) => item.id !== readingId).slice(0, limit);
  }

  async shareReading(readingId: number, _options?: { is_public?: boolean; expires_in_days?: number }) {
    return {
      reading_id: readingId,
      share_url: `${window.location.origin}/reading/${readingId}`,
      created_at: new Date().toISOString(),
    };
  }
}

export const readingService = new ReadingService();
export default readingService;
