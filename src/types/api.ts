// 基础API响应类型
export interface ApiResponse<T> {
    data: T;
    message: string;
    status: number;
  }

  // 用户类型
  export interface User {
    id: number;
    username: string;
    email: string;
    nickname?: string;
    avatar_url?: string;
    created_at?: string;
  }

  // 认证相关类型
  export interface LoginData {
    username: string;
    password: string;
  }

  export interface RegisterData {
    username: string;
    email: string;
    password: string;
    nickname?: string;
  }

  export interface AuthResponse {
    success?: boolean;
    data?: {
      user: User;
      token: string;
    };
    message?: string;
    access_token?: string;
    token_type?: string;
  }

  // 塔罗牌类型
  export interface TarotCard {
    id: number;
    name_zh: string;
    name_en: string;
    card_number: number;
    card_type: 'major_arcana' | 'minor_arcana';
    suit?: string | null;
    image_url?: string;
    upright_meaning?: string;
    reversed_meaning?: string;
    description?: string;
    keywords_upright?: string;
    keywords_reversed?: string;
  }

  // 牌阵类型
  export interface SpreadType {
    id: number;
    name: string;
    name_en?: string;
    description: string;
    card_count: number;
    difficulty_level: number;
    positions?: Array<{
      position: number;
      name: string;
      meaning: string;
    }>;
    is_beginner_friendly: boolean;
    usage_count?: number;
    layout_image_url?: string;
    suitable_for_love?: boolean;
    suitable_for_career?: boolean;
    suitable_for_finance?: boolean;
    suitable_for_health?: boolean;
    suitable_for_general?: boolean;
  }

  // 占卜记录类型
  export interface Reading {
    id: number;
    user_id: number;
    spread_type_id: number;
    question: string;
    question_type: 'love' | 'career' | 'finance' | 'health' | 'general';
    status: 'pending' | 'processing' | 'completed' | 'failed';
    created_at: string;
    completed_at?: string;
  }
