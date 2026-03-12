import { api } from './api';
import { User } from '../types/api';

export interface UpdateProfileParams {
  username?: string;
  email?: string;
}

export interface ChangePasswordParams {
  current_password: string;
  new_password: string;
  confirm_password: string;
}

export interface UserPreferences {
  language: 'zh' | 'en';
  theme: 'light' | 'dark' | 'auto';
  card_style: 'classic' | 'modern' | 'mystical';
  reading_reminder: boolean;
  email_notifications: boolean;
  privacy_level: 'public' | 'friends' | 'private';
  default_question_type: string;
  favorite_spreads: number[];
}

export interface UserStats {
  total_readings: number;
  days_active: number;
  favorite_cards: string[];
  most_used_question_type: string;
  reading_streak: number;
  achievement_count: number;
  level: number;
  experience_points: number;
}

export interface Achievement {
  id: string;
  name: string;
  description: string;
  icon: string;
  category: 'readings' | 'cards' | 'streaks' | 'special';
  unlocked_at?: string;
  progress?: {
    current: number;
    required: number;
  };
}

const USER_PREFERENCES_KEY = 'tarot_user_preferences';
const USER_ACHIEVEMENTS_KEY = 'tarot_user_achievements';

export class UserService {
  async getCurrentUser(): Promise<User> {
    return api.get('/users/me');
  }

  async updateProfile(params: UpdateProfileParams): Promise<User> {
    return api.put('/users/me', params);
  }

  async changePassword(_params: ChangePasswordParams): Promise<{ success: boolean; message: string }> {
    throw new Error('当前后端未实现修改密码接口');
  }

  async uploadAvatar(_file: File): Promise<{ avatar_url: string }> {
    throw new Error('当前后端未实现头像上传接口');
  }

  async getUserPreferences(): Promise<UserPreferences> {
    try {
      const raw = localStorage.getItem(USER_PREFERENCES_KEY);
      if (!raw) {
        return this.getDefaultPreferences();
      }
      return { ...this.getDefaultPreferences(), ...JSON.parse(raw) };
    } catch {
      return this.getDefaultPreferences();
    }
  }

  async updateUserPreferences(preferences: Partial<UserPreferences>): Promise<UserPreferences> {
    const merged = {
      ...(await this.getUserPreferences()),
      ...preferences,
    };
    localStorage.setItem(USER_PREFERENCES_KEY, JSON.stringify(merged));
    return merged;
  }

  async getUserStats(): Promise<UserStats> {
    const recordsStats = (await api.get('/records/stats')) as any;
    const totalReadings = Number(recordsStats?.total_predictions ?? 0);
    const completed = Number(recordsStats?.completed_predictions ?? 0);

    return {
      total_readings: totalReadings,
      days_active: Math.max(1, Math.ceil(totalReadings / 2)),
      favorite_cards: [],
      most_used_question_type: recordsStats?.most_used_question_type || 'general',
      reading_streak: Math.max(0, Math.min(30, completed)),
      achievement_count: (await this.getUserAchievements()).length,
      level: Math.max(1, Math.floor(totalReadings / 10) + 1),
      experience_points: totalReadings * 20,
    };
  }

  async getUserAchievements(): Promise<Achievement[]> {
    try {
      const raw = localStorage.getItem(USER_ACHIEVEMENTS_KEY);
      if (!raw) {
        return [];
      }
      return JSON.parse(raw) as Achievement[];
    } catch {
      return [];
    }
  }

  async unlockAchievement(achievementId: string): Promise<Achievement> {
    const achievements = await this.getUserAchievements();
    const existing = achievements.find((item) => item.id === achievementId);
    if (existing) {
      return existing;
    }

    const unlocked: Achievement = {
      id: achievementId,
      name: achievementId,
      description: '已解锁成就',
      icon: 'achievement',
      category: 'special',
      unlocked_at: new Date().toISOString(),
    };

    achievements.push(unlocked);
    localStorage.setItem(USER_ACHIEVEMENTS_KEY, JSON.stringify(achievements));
    return unlocked;
  }

  async deleteAccount(_password: string): Promise<{ success: boolean; message: string }> {
    throw new Error('当前后端未实现注销账号接口');
  }

  async exportUserData(): Promise<Blob> {
    const [user, readings] = await Promise.all([
      this.getCurrentUser(),
      api.get('/records/?skip=0&limit=100'),
    ]);

    const payload = {
      exported_at: new Date().toISOString(),
      user,
      readings,
      preferences: await this.getUserPreferences(),
      achievements: await this.getUserAchievements(),
    };

    return new Blob([JSON.stringify(payload, null, 2)], {
      type: 'application/json',
    });
  }

  async getUserById(userId: number): Promise<User> {
    return api.get(`/users/${userId}`);
  }

  async checkEmailAvailability(_email: string): Promise<{ available: boolean }> {
    return { available: true };
  }

  getDefaultPreferences(): UserPreferences {
    return {
      language: 'zh',
      theme: 'auto',
      card_style: 'classic',
      reading_reminder: true,
      email_notifications: false,
      privacy_level: 'private',
      default_question_type: 'general',
      favorite_spreads: [],
    };
  }

  async getUserDashboardData() {
    const [user, stats, preferences, achievements] = await Promise.all([
      this.getCurrentUser(),
      this.getUserStats(),
      this.getUserPreferences(),
      this.getUserAchievements(),
    ]);

    return {
      user,
      stats,
      preferences,
      achievements,
    };
  }
}

export const userService = new UserService();
export default userService;
