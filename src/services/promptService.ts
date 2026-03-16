/**
 * 提示词管理服务
 * 管理塔罗牌解读的各种提示词模板
 */

// 提示词模板类型
export interface PromptTemplate {
  id: string;
  name: string;
  description: string;
  template: string;
  variables: string[];
  category: 'tarot' | 'general' | 'system';
  language: 'zh' | 'en';
  version: string;
}

// 问题类型配置
export interface QuestionTypeConfig {
  type: 'love' | 'career' | 'finance' | 'health' | 'general';
  name: string;
  description: string;
  keywords: string[];
  focusAreas: string[];
  suggestedSpreads: string[];
}

// 牌阵解读配置
export interface SpreadConfig {
  name: string;
  positions: Array<{
    index: number;
    name: string;
    meaning: string;
    description: string;
  }>;
  interpretation_guide: string;
}

/**
 * 提示词服务类
 */
export class PromptService {
  private templates: Map<string, PromptTemplate> = new Map();
  private questionTypes: Map<string, QuestionTypeConfig> = new Map();
  private spreadConfigs: Map<string, SpreadConfig> = new Map();

  constructor() {
    this.initializeTemplates();
    this.initializeQuestionTypes();
    this.initializeSpreadConfigs();
  }

  /**
   * 初始化提示词模板
   */
  private initializeTemplates(): void {
    const templates: PromptTemplate[] = [
      {
        id: 'basic_tarot_reading',
        name: '基础塔罗解读',
        description: '标准的塔罗牌解读模板',
        template: `你是一位经验丰富的专业塔罗师，名叫「卡珊德拉」。

用户问题：{{question}}
问题类型：{{question_type}}
使用牌阵：{{spread_name}}
抽取牌面：{{cards_info}}

请按照以下格式提供专业的塔罗占卜解读：

## 🔮 整体运势
（对本次占卜的总体印象和能量概述，100-200字）

## 🃏 逐牌解析
{{card_details}}

## ⚡ 牌间关系
（分析牌与牌之间的相互影响和联系，150-250字）

## 💡 智慧指引
（针对问题的具体建议和指导，200-300字）

## ✨ 温馨提醒
（积极正面的结语和祝福，50-100字）

请保持神秘而温暖的语调，语言优美富有诗意但易懂。`,
        variables: ['question', 'question_type', 'spread_name', 'cards_info', 'card_details'],
        category: 'tarot',
        language: 'zh',
        version: '1.0',
      },
      {
        id: 'love_focused_reading',
        name: '爱情专题解读',
        description: '专门针对情感问题的解读模板',
        template: `作为专业的情感塔罗师，我将为你解读爱情运势。

💕 情感问题：{{question}}
🌹 使用牌阵：{{spread_name}}
💎 抽取牌面：{{cards_info}}

## 💖 感情能量分析
（分析当前感情状态的能量场）

## 👥 关系动态解读
{{card_details}}

## 💘 情感发展趋势
（预测感情的可能发展方向）

## 💌 爱情行动指南
（具体的感情建议和行动方案）

## 🌟 情感祝福
（温馨的爱情祝福）

愿爱情的力量为你带来幸福与温暖。`,
        variables: ['question', 'spread_name', 'cards_info', 'card_details'],
        category: 'tarot',
        language: 'zh',
        version: '1.0',
      },
      {
        id: 'career_focused_reading',
        name: '事业专题解读',
        description: '专门针对事业问题的解读模板',
        template: `作为资深的事业塔罗顾问，我将为你解读职场运势。

💼 事业问题：{{question}}
📈 使用牌阵：{{spread_name}}
🎯 抽取牌面：{{cards_info}}

## 🚀 事业现状分析
（当前职业状态和发展阶段）

## 📊 职场能量解读
{{card_details}}

## 📈 发展机遇预测
（未来的机会和挑战）

## 💡 成功策略建议
（具体的职业发展建议）

## ⭐ 事业祝福
（对事业成功的美好祝愿）

愿智慧之光照亮你的职业道路。`,
        variables: ['question', 'spread_name', 'cards_info', 'card_details'],
        category: 'tarot',
        language: 'zh',
        version: '1.0',
      },
    ];

    templates.forEach(template => {
      this.templates.set(template.id, template);
    });
  }

  /**
   * 初始化问题类型配置
   */
  private initializeQuestionTypes(): void {
    const questionTypes: QuestionTypeConfig[] = [
      {
        type: 'love',
        name: '爱情感情',
        description: '关于恋爱关系、婚姻、情感发展的问题',
        keywords: ['爱情', '恋人', '婚姻', '感情', '关系', '表白', '分手', '复合'],
        focusAreas: ['感情状态', '关系发展', '对方心意', '感情阻碍', '情感建议'],
        suggestedSpreads: ['爱情牌阵', '过去现在未来', '单牌抽取'],
      },
      {
        type: 'career',
        name: '事业工作',
        description: '关于职业发展、工作机会、事业规划的问题',
        keywords: ['工作', '事业', '职业', '升职', '跳槽', '创业', '项目', '合作'],
        focusAreas: ['职业现状', '发展机会', '工作挑战', '团队关系', '职业建议'],
        suggestedSpreads: ['事业发展', '凯尔特十字', '过去现在未来'],
      },
      {
        type: 'finance',
        name: '财运金钱',
        description: '关于财务状况、投资理财、金钱运势的问题',
        keywords: ['财运', '金钱', '投资', '理财', '收入', '支出', '债务', '财富'],
        focusAreas: ['财务现状', '收入来源', '投资机会', '理财建议', '财富增长'],
        suggestedSpreads: ['财运牌阵', '过去现在未来', '单牌抽取'],
      },
      {
        type: 'health',
        name: '健康身体',
        description: '关于身体健康、精神状态、生活方式的问题',
        keywords: ['健康', '身体', '疾病', '治疗', '养生', '精神', '压力', '休息'],
        focusAreas: ['身体状况', '健康趋势', '生活习惯', '精神状态', '康复建议'],
        suggestedSpreads: ['单牌抽取', '过去现在未来', '三牌牌阵'],
      },
      {
        type: 'general',
        name: '综合运势',
        description: '关于整体运势、人生方向、重要决策的问题',
        keywords: ['运势', '命运', '人生', '选择', '决策', '方向', '未来', '发展'],
        focusAreas: ['整体运势', '人生方向', '重要选择', '发展机会', '生活建议'],
        suggestedSpreads: ['凯尔特十字', '过去现在未来', '事业发展'],
      },
    ];

    questionTypes.forEach(config => {
      this.questionTypes.set(config.type, config);
    });
  }

  /**
   * 初始化牌阵配置
   */
  private initializeSpreadConfigs(): void {
    const spreads: SpreadConfig[] = [
      {
        name: '单牌抽取',
        positions: [
          {
            index: 0,
            name: '指导',
            meaning: '当前最需要的指导和建议',
            description: '这张牌将为你提供当下最重要的指引',
          },
        ],
        interpretation_guide: '重点解读这张牌在当前情况下的深层含义和实用建议。',
      },
      {
        name: '过去现在未来',
        positions: [
          {
            index: 0,
            name: '过去',
            meaning: '影响当前状况的过去因素',
            description: '过去的经历、教训或根源',
          },
          {
            index: 1,
            name: '现在',
            meaning: '当前的状态和挑战',
            description: '现在面临的主要情况和能量',
          },
          {
            index: 2,
            name: '未来',
            meaning: '可能的发展方向',
            description: '基于当前选择的未来趋势',
          },
        ],
        interpretation_guide: '分析三个时间段的连贯性，重点关注从过去到未来的发展脉络。',
      },
      {
        name: '爱情牌阵',
        positions: [
          {
            index: 0,
            name: '你的感受',
            meaning: '你对这段关系的真实感受',
            description: '内心深处对感情的态度和情绪',
          },
          {
            index: 1,
            name: '对方感受',
            meaning: '对方对这段关系的感受',
            description: '对方的内心想法和情感状态',
          },
          {
            index: 2,
            name: '关系现状',
            meaning: '你们关系的当前状态',
            description: '两人关系的整体现状和特点',
          },
          {
            index: 3,
            name: '阻碍因素',
            meaning: '影响关系发展的障碍',
            description: '需要克服的困难和挑战',
          },
          {
            index: 4,
            name: '发展方向',
            meaning: '关系可能的发展方向',
            description: '感情的未来走向和建议',
          },
        ],
        interpretation_guide: '重点分析双方的情感互动，关注感情的平衡与和谐。',
      },
    ];

    spreads.forEach(spread => {
      this.spreadConfigs.set(spread.name, spread);
    });
  }

  /**
   * 获取提示词模板
   */
  getTemplate(templateId: string): PromptTemplate | null {
    return this.templates.get(templateId) || null;
  }

  /**
   * 根据问题类型获取合适的模板
   */
  getTemplateByQuestionType(questionType: string): PromptTemplate | null {
    switch (questionType) {
      case 'love':
        return this.getTemplate('love_focused_reading');
      case 'career':
        return this.getTemplate('career_focused_reading');
      default:
        return this.getTemplate('basic_tarot_reading');
    }
  }

  /**
   * 构建完整的提示词
   */
  buildPrompt(
    templateId: string,
    variables: Record<string, string>
  ): string {
    const template = this.getTemplate(templateId);
    if (!template) {
      throw new Error(`模板 ${templateId} 不存在`);
    }

    let prompt = template.template;

    // 替换变量
    Object.entries(variables).forEach(([key, value]) => {
      const placeholder = `{{${key}}}`;
      prompt = prompt.replace(new RegExp(placeholder, 'g'), value);
    });

    return prompt;
  }

  /**
   * 构建塔罗解读提示词
   */
  buildTarotPrompt(
    question: string,
    questionType: string,
    spreadName: string,
    cards: Array<{
      name: string;
      position: string;
      isUpright: boolean;
    }>
  ): string {
    const template = this.getTemplateByQuestionType(questionType);
    if (!template) {
      throw new Error('找不到合适的解读模板');
    }

    // 构建卡牌信息
    const cardsInfo = cards
      .map(card => `${card.name}(${card.isUpright ? '正位' : '逆位'}, ${card.position})`)
      .join(', ');

    // 构建详细卡牌解读结构
    const cardDetails = cards
      .map((card, index) => {
        return `### ${index + 1}. ${card.position} - ${card.name} (${card.isUpright ? '正位' : '逆位'})
（分析这张牌在此位置的具体含义和建议）`;
      })
      .join('\n\n');

    const variables = {
      question,
      question_type: this.getQuestionTypeName(questionType),
      spread_name: spreadName,
      cards_info: cardsInfo,
      card_details: cardDetails,
    };

    return this.buildPrompt(template.id, variables);
  }

  /**
   * 获取问题类型配置
   */
  getQuestionTypeConfig(type: string): QuestionTypeConfig | null {
    return this.questionTypes.get(type) || null;
  }

  /**
   * 获取问题类型名称
   */
  getQuestionTypeName(type: string): string {
    const config = this.getQuestionTypeConfig(type);
    return config ? config.name : type;
  }

  /**
   * 获取牌阵配置
   */
  getSpreadConfig(spreadName: string): SpreadConfig | null {
    return this.spreadConfigs.get(spreadName) || null;
  }

  /**
   * 获取所有问题类型
   */
  getAllQuestionTypes(): QuestionTypeConfig[] {
    return Array.from(this.questionTypes.values());
  }

  /**
   * 获取所有提示词模板
   */
  getAllTemplates(): PromptTemplate[] {
    return Array.from(this.templates.values());
  }

  /**
   * 根据关键词推荐问题类型
   */
  suggestQuestionType(question: string): string {
    const lowerQuestion = question.toLowerCase();

    const typeEntries = Array.from(this.questionTypes.entries());
    for (const [type, config] of typeEntries) {
      const hasKeyword = config.keywords.some(keyword =>
        lowerQuestion.includes(keyword)
      );
      if (hasKeyword) {
        return type;
      }
    }

    return 'general';
  }

  /**
   * 验证提示词变量
   */
  validatePromptVariables(templateId: string, variables: Record<string, string>): {
    isValid: boolean;
    missingVariables: string[];
  } {
    const template = this.getTemplate(templateId);
    if (!template) {
      return { isValid: false, missingVariables: [] };
    }

    const missingVariables = template.variables.filter(
      variable => !variables[variable]
    );

    return {
      isValid: missingVariables.length === 0,
      missingVariables,
    };
  }

  /**
   * 添加自定义模板
   */
  addTemplate(template: PromptTemplate): void {
    this.templates.set(template.id, template);
  }

  /**
   * 更新模板
   */
  updateTemplate(templateId: string, updates: Partial<PromptTemplate>): boolean {
    const template = this.templates.get(templateId);
    if (!template) {
      return false;
    }

    const updatedTemplate = { ...template, ...updates };
    this.templates.set(templateId, updatedTemplate);
    return true;
  }
}

// 创建全局实例
export const promptService = new PromptService();

// 默认导出
export default promptService;