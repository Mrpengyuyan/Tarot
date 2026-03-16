import { format, formatDistanceToNow, parseISO, isValid, differenceInDays, addDays, startOfDay, endOfDay, startOfWeek, endOfWeek, startOfMonth, endOfMonth, isBefore, isAfter, isSameDay } from 'date-fns';
import { zhCN } from 'date-fns/locale';

// 日期格式化函数
export const formatDate = (
  date: Date | string | number,
  formatString: string = 'yyyy-MM-dd'
): string => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    if (!isValid(dateObj)) {
      throw new Error('Invalid date');
    }
    return format(dateObj, formatString, { locale: zhCN });
  } catch (error) {
    console.error('Date formatting error:', error);
    return '无效日期';
  }
};

// 格式化日期时间
export const formatDateTime = (
  date: Date | string | number,
  formatString: string = 'yyyy-MM-dd HH:mm:ss'
): string => {
  return formatDate(date, formatString);
};

// 格式化时间
export const formatTime = (
  date: Date | string | number,
  formatString: string = 'HH:mm:ss'
): string => {
  return formatDate(date, formatString);
};

// 相对时间格式化（如：2小时前）
export const formatRelativeTime = (
  date: Date | string | number,
  addSuffix: boolean = true
): string => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    if (!isValid(dateObj)) {
      throw new Error('Invalid date');
    }
    return formatDistanceToNow(dateObj, {
      locale: zhCN,
      addSuffix
    });
  } catch (error) {
    console.error('Relative time formatting error:', error);
    return '时间未知';
  }
};

// 智能日期格式化（根据时间差选择格式）
export const formatSmartDate = (date: Date | string | number): string => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    if (!isValid(dateObj)) {
      return '无效日期';
    }

    const now = new Date();
    const daysDiff = differenceInDays(now, dateObj);

    if (daysDiff === 0) {
      return formatTime(dateObj, 'HH:mm');
    } else if (daysDiff === 1) {
      return '昨天 ' + formatTime(dateObj, 'HH:mm');
    } else if (daysDiff < 7) {
      return formatDate(dateObj, 'EEEE HH:mm');
    } else if (daysDiff < 365) {
      return formatDate(dateObj, 'MM-dd HH:mm');
    } else {
      return formatDate(dateObj, 'yyyy-MM-dd');
    }
  } catch (error) {
    console.error('Smart date formatting error:', error);
    return '时间未知';
  }
};

// 获取今天的开始和结束时间
export const getToday = (): { start: Date; end: Date } => {
  const now = new Date();
  return {
    start: startOfDay(now),
    end: endOfDay(now),
  };
};

// 获取本周的开始和结束时间
export const getThisWeek = (): { start: Date; end: Date } => {
  const now = new Date();
  return {
    start: startOfWeek(now, { weekStartsOn: 1 }), // 周一开始
    end: endOfWeek(now, { weekStartsOn: 1 }),
  };
};

// 获取本月的开始和结束时间
export const getThisMonth = (): { start: Date; end: Date } => {
  const now = new Date();
  return {
    start: startOfMonth(now),
    end: endOfMonth(now),
  };
};

// 获取指定天数前/后的日期
export const getDaysFromNow = (days: number): Date => {
  return addDays(new Date(), days);
};

// 计算两个日期之间的天数差
export const getDaysBetween = (
  startDate: Date | string | number,
  endDate: Date | string | number
): number => {
  try {
    const start = typeof startDate === 'string' ? parseISO(startDate) : new Date(startDate);
    const end = typeof endDate === 'string' ? parseISO(endDate) : new Date(endDate);

    if (!isValid(start) || !isValid(end)) {
      throw new Error('Invalid date');
    }

    return differenceInDays(end, start);
  } catch (error) {
    console.error('Days calculation error:', error);
    return 0;
  }
};

// 检查日期是否在指定范围内
export const isDateInRange = (
  date: Date | string | number,
  startDate: Date | string | number,
  endDate: Date | string | number
): boolean => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    const startObj = typeof startDate === 'string' ? parseISO(startDate) : new Date(startDate);
    const endObj = typeof endDate === 'string' ? parseISO(endDate) : new Date(endDate);

    if (!isValid(dateObj) || !isValid(startObj) || !isValid(endObj)) {
      return false;
    }

    return !isBefore(dateObj, startObj) && !isAfter(dateObj, endObj);
  } catch (error) {
    console.error('Date range check error:', error);
    return false;
  }
};

// 检查是否是今天
export const isToday = (date: Date | string | number): boolean => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    if (!isValid(dateObj)) {
      return false;
    }
    return isSameDay(dateObj, new Date());
  } catch (error) {
    console.error('Today check error:', error);
    return false;
  }
};

// 获取星座
export const getZodiacSign = (date: Date | string | number): string => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    if (!isValid(dateObj)) {
      return '未知';
    }

    const month = dateObj.getMonth() + 1;
    const day = dateObj.getDate();

    if ((month === 3 && day >= 21) || (month === 4 && day <= 19)) return '白羊座';
    if ((month === 4 && day >= 20) || (month === 5 && day <= 20)) return '金牛座';
    if ((month === 5 && day >= 21) || (month === 6 && day <= 21)) return '双子座';
    if ((month === 6 && day >= 22) || (month === 7 && day <= 22)) return '巨蟹座';
    if ((month === 7 && day >= 23) || (month === 8 && day <= 22)) return '狮子座';
    if ((month === 8 && day >= 23) || (month === 9 && day <= 22)) return '处女座';
    if ((month === 9 && day >= 23) || (month === 10 && day <= 23)) return '天秤座';
    if ((month === 10 && day >= 24) || (month === 11 && day <= 21)) return '天蝎座';
    if ((month === 11 && day >= 22) || (month === 12 && day <= 21)) return '射手座';
    if ((month === 12 && day >= 22) || (month === 1 && day <= 19)) return '摩羯座';
    if ((month === 1 && day >= 20) || (month === 2 && day <= 18)) return '水瓶座';
    if ((month === 2 && day >= 19) || (month === 3 && day <= 20)) return '双鱼座';

    return '未知';
  } catch (error) {
    console.error('Zodiac sign error:', error);
    return '未知';
  }
};

// 获取农历信息（简化版，仅提供接口）
export const getLunarDate = (date: Date | string | number): string => {
  // 这里可以集成农历库，如 lunar-javascript
  // 暂时返回占位符
  return '农历日期';
};

// 获取时间戳
export const getTimestamp = (date?: Date | string | number): number => {
  try {
    const dateObj = date ? (typeof date === 'string' ? parseISO(date) : new Date(date)) : new Date();
    if (!isValid(dateObj)) {
      throw new Error('Invalid date');
    }
    return dateObj.getTime();
  } catch (error) {
    console.error('Timestamp error:', error);
    return Date.now();
  }
};

// 从时间戳创建日期
export const fromTimestamp = (timestamp: number): Date => {
  return new Date(timestamp);
};

// 验证日期字符串格式
export const validateDateString = (
  dateString: string,
  formatString: string = 'yyyy-MM-dd'
): boolean => {
  try {
    const date = parseISO(dateString);
    return isValid(date) && formatDate(date, formatString) === dateString;
  } catch (error) {
    return false;
  }
};

// 获取本地时区偏移
export const getTimezoneOffset = (): number => {
  return new Date().getTimezoneOffset();
};

// 转换为本地时区
export const toLocalTimezone = (date: Date | string | number): Date => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    if (!isValid(dateObj)) {
      throw new Error('Invalid date');
    }
    return new Date(dateObj.getTime() - dateObj.getTimezoneOffset() * 60000);
  } catch (error) {
    console.error('Timezone conversion error:', error);
    return new Date();
  }
};

// 日期范围生成器
export const generateDateRange = (
  startDate: Date | string,
  endDate: Date | string,
  interval: 'day' | 'week' | 'month' = 'day'
): Date[] => {
  try {
    const start = typeof startDate === 'string' ? parseISO(startDate) : startDate;
    const end = typeof endDate === 'string' ? parseISO(endDate) : endDate;

    if (!isValid(start) || !isValid(end)) {
      throw new Error('Invalid date range');
    }

    const dates: Date[] = [];
    let current = new Date(start);

    while (!isAfter(current, end)) {
      dates.push(new Date(current));

      switch (interval) {
        case 'day':
          current = addDays(current, 1);
          break;
        case 'week':
          current = addDays(current, 7);
          break;
        case 'month':
          current = new Date(current.getFullYear(), current.getMonth() + 1, current.getDate());
          break;
      }
    }

    return dates;
  } catch (error) {
    console.error('Date range generation error:', error);
    return [];
  }
};

// 工作日/周末检查
export const isWeekend = (date: Date | string | number): boolean => {
  try {
    const dateObj = typeof date === 'string' ? parseISO(date) : new Date(date);
    if (!isValid(dateObj)) {
      return false;
    }
    const day = dateObj.getDay();
    return day === 0 || day === 6; // 周日或周六
  } catch (error) {
    return false;
  }
};

export const isWeekday = (date: Date | string | number): boolean => {
  return !isWeekend(date);
};

// 年龄计算
export const calculateAge = (birthDate: Date | string | number): number => {
  try {
    const birth = typeof birthDate === 'string' ? parseISO(birthDate) : new Date(birthDate);
    if (!isValid(birth)) {
      return 0;
    }

    const today = new Date();
    let age = today.getFullYear() - birth.getFullYear();
    const monthDiff = today.getMonth() - birth.getMonth();

    if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())) {
      age--;
    }

    return age;
  } catch (error) {
    console.error('Age calculation error:', error);
    return 0;
  }
};

// 预设的常用日期格式
export const DATE_FORMATS = {
  DATE_ONLY: 'yyyy-MM-dd',
  TIME_ONLY: 'HH:mm:ss',
  DATETIME: 'yyyy-MM-dd HH:mm:ss',
  DATETIME_SHORT: 'yyyy-MM-dd HH:mm',
  CHINESE_DATE: 'yyyy年MM月dd日',
  CHINESE_DATETIME: 'yyyy年MM月dd日 HH时mm分',
  ISO_STRING: "yyyy-MM-dd'T'HH:mm:ss.SSSxxx",
  RELATIVE: 'relative',
} as const;

const dateUtils = {
  formatDate,
  formatDateTime,
  formatTime,
  formatRelativeTime,
  formatSmartDate,
  getToday,
  getThisWeek,
  getThisMonth,
  getDaysFromNow,
  getDaysBetween,
  isDateInRange,
  isToday,
  getZodiacSign,
  getLunarDate,
  getTimestamp,
  fromTimestamp,
  validateDateString,
  getTimezoneOffset,
  toLocalTimezone,
  generateDateRange,
  isWeekend,
  isWeekday,
  calculateAge,
  DATE_FORMATS,
};

export default dateUtils;
