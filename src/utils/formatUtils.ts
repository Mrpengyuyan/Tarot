// 数字格式化工具

// 格式化数字，添加千分位分隔符
export const formatNumber = (
  num: number | string,
  options: {
    locale?: string;
    minimumFractionDigits?: number;
    maximumFractionDigits?: number;
    useGrouping?: boolean;
  } = {}
): string => {
  try {
    const {
      locale = 'zh-CN',
      minimumFractionDigits = 0,
      maximumFractionDigits = 2,
      useGrouping = true,
    } = options;

    const number = typeof num === 'string' ? parseFloat(num) : num;

    if (isNaN(number)) {
      return '0';
    }

    return new Intl.NumberFormat(locale, {
      minimumFractionDigits,
      maximumFractionDigits,
      useGrouping,
    }).format(number);
  } catch (error) {
    console.error('Number formatting error:', error);
    return String(num);
  }
};

// 格式化货币
export const formatCurrency = (
  amount: number | string,
  options: {
    currency?: string;
    locale?: string;
    minimumFractionDigits?: number;
    maximumFractionDigits?: number;
  } = {}
): string => {
  try {
    const {
      currency = 'CNY',
      locale = 'zh-CN',
      minimumFractionDigits = 2,
      maximumFractionDigits = 2,
    } = options;

    const number = typeof amount === 'string' ? parseFloat(amount) : amount;

    if (isNaN(number)) {
      return '¥0.00';
    }

    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency,
      minimumFractionDigits,
      maximumFractionDigits,
    }).format(number);
  } catch (error) {
    console.error('Currency formatting error:', error);
    return `¥${amount}`;
  }
};

// 格式化百分比
export const formatPercentage = (
  value: number | string,
  options: {
    minimumFractionDigits?: number;
    maximumFractionDigits?: number;
    locale?: string;
  } = {}
): string => {
  try {
    const {
      minimumFractionDigits = 0,
      maximumFractionDigits = 2,
      locale = 'zh-CN',
    } = options;

    const number = typeof value === 'string' ? parseFloat(value) : value;

    if (isNaN(number)) {
      return '0%';
    }

    return new Intl.NumberFormat(locale, {
      style: 'percent',
      minimumFractionDigits,
      maximumFractionDigits,
    }).format(number);
  } catch (error) {
    console.error('Percentage formatting error:', error);
    return `${value}%`;
  }
};

// 格式化文件大小
export const formatFileSize = (bytes: number, decimals: number = 2): string => {
  if (bytes === 0) return '0 Bytes';
  if (bytes < 0) return 'Invalid size';

  const k = 1024;
  const dm = decimals < 0 ? 0 : decimals;
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB'];

  const i = Math.floor(Math.log(bytes) / Math.log(k));
  const size = parseFloat((bytes / Math.pow(k, i)).toFixed(dm));

  return `${size} ${sizes[i]}`;
};

// 格式化时长（秒转为可读格式）
export const formatDuration = (
  seconds: number,
  options: {
    format?: 'short' | 'long' | 'digital';
    showZero?: boolean;
  } = {}
): string => {
  try {
    const { format = 'short', showZero = false } = options;

    if (seconds < 0) return '0';

    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = Math.floor(seconds % 60);

    if (format === 'digital') {
      if (hours > 0) {
        return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
      }
      return `${minutes.toString().padStart(2, '0')}:${secs.toString().padStart(2, '0')}`;
    }

    const parts: string[] = [];

    if (hours > 0 || showZero) {
      parts.push(format === 'long' ? `${hours}小时` : `${hours}h`);
    }
    if (minutes > 0 || (showZero && hours > 0)) {
      parts.push(format === 'long' ? `${minutes}分钟` : `${minutes}m`);
    }
    if (secs > 0 || (showZero && (hours > 0 || minutes > 0))) {
      parts.push(format === 'long' ? `${secs}秒` : `${secs}s`);
    }

    return parts.join(' ') || (format === 'long' ? '0秒' : '0s');
  } catch (error) {
    console.error('Duration formatting error:', error);
    return '0s';
  }
};

// 字符串截取并添加省略号
export const truncateText = (
  text: string,
  maxLength: number,
  suffix: string = '...'
): string => {
  if (!text || text.length <= maxLength) {
    return text || '';
  }

  return text.substring(0, maxLength - suffix.length) + suffix;
};

// 格式化电话号码
export const formatPhoneNumber = (phone: string): string => {
  try {
    // 移除所有非数字字符
    const cleaned = phone.replace(/\D/g, '');

    // 中国手机号码格式化
    if (cleaned.length === 11 && cleaned.startsWith('1')) {
      return cleaned.replace(/(\d{3})(\d{4})(\d{4})/, '$1 $2 $3');
    }

    // 固定电话格式化（区号-号码）
    if (cleaned.length >= 10) {
      return cleaned.replace(/(\d{3,4})(\d{7,8})/, '$1-$2');
    }

    return cleaned;
  } catch (error) {
    console.error('Phone formatting error:', error);
    return phone;
  }
};

// 格式化银行卡号
export const formatCardNumber = (cardNumber: string): string => {
  try {
    const cleaned = cardNumber.replace(/\D/g, '');
    return cleaned.replace(/(\d{4})(?=\d)/g, '$1 ').trim();
  } catch (error) {
    console.error('Card number formatting error:', error);
    return cardNumber;
  }
};

// 格式化身份证号
export const formatIdCard = (idCard: string): string => {
  try {
    const cleaned = idCard.replace(/\s/g, '');
    if (cleaned.length === 18) {
      return cleaned.replace(/(\d{6})(\d{8})(\d{4})/, '$1 $2 $3');
    }
    if (cleaned.length === 15) {
      return cleaned.replace(/(\d{6})(\d{6})(\d{3})/, '$1 $2 $3');
    }
    return cleaned;
  } catch (error) {
    console.error('ID card formatting error:', error);
    return idCard;
  }
};

// 首字母大写
export const capitalize = (str: string): string => {
  if (!str) return '';
  return str.charAt(0).toUpperCase() + str.slice(1).toLowerCase();
};

// 每个单词首字母大写
export const capitalizeWords = (str: string): string => {
  if (!str) return '';
  return str.replace(/\b\w/g, char => char.toUpperCase());
};

// 驼峰命名转换
export const toCamelCase = (str: string): string => {
  return str.replace(/[-_\s]+(.)?/g, (_, char) => char ? char.toUpperCase() : '');
};

// 蛇形命名转换
export const toSnakeCase = (str: string): string => {
  return str.replace(/[A-Z]/g, (char, index) =>
    index > 0 ? '_' + char.toLowerCase() : char.toLowerCase()
  ).replace(/[-\s]+/g, '_');
};

// 短横线命名转换
export const toKebabCase = (str: string): string => {
  return str.replace(/[A-Z]/g, (char, index) =>
    index > 0 ? '-' + char.toLowerCase() : char.toLowerCase()
  ).replace(/[_\s]+/g, '-');
};

// 格式化JSON
export const formatJSON = (obj: any, indent: number = 2): string => {
  try {
    return JSON.stringify(obj, null, indent);
  } catch (error) {
    console.error('JSON formatting error:', error);
    return String(obj);
  }
};

// 压缩JSON
export const compressJSON = (obj: any): string => {
  try {
    return JSON.stringify(obj);
  } catch (error) {
    console.error('JSON compression error:', error);
    return String(obj);
  }
};

// 移除HTML标签
export const stripHtml = (html: string): string => {
  try {
    const doc = new DOMParser().parseFromString(html, 'text/html');
    return doc.body.textContent || '';
  } catch (error) {
    // fallback: 使用正则表达式
    return html.replace(/<[^>]*>/g, '');
  }
};

// 转义HTML特殊字符
export const escapeHtml = (text: string): string => {
  const map: { [key: string]: string } = {
    '&': '&amp;',
    '<': '&lt;',
    '>': '&gt;',
    '"': '&quot;',
    "'": '&#39;',
  };

  return text.replace(/[&<>"']/g, char => map[char]);
};

// 反转义HTML特殊字符
export const unescapeHtml = (text: string): string => {
  const map: { [key: string]: string } = {
    '&amp;': '&',
    '&lt;': '<',
    '&gt;': '>',
    '&quot;': '"',
    '&#39;': "'",
  };

  return text.replace(/&(amp|lt|gt|quot|#39);/g, entity => map[entity]);
};

// 格式化URL
export const formatUrl = (url: string): string => {
  try {
    if (!url) return '';

    // 如果URL不包含协议，添加https://
    if (!/^https?:\/\//i.test(url)) {
      return `https://${url}`;
    }

    return url;
  } catch (error) {
    console.error('URL formatting error:', error);
    return url;
  }
};

// 提取URL域名
export const extractDomain = (url: string): string => {
  try {
    const urlObj = new URL(formatUrl(url));
    return urlObj.hostname;
  } catch (error) {
    console.error('Domain extraction error:', error);
    return url;
  }
};

// 格式化颜色值
export const formatColor = (color: string, format: 'hex' | 'rgb' | 'hsl' = 'hex'): string => {
  try {
    // 这里可以使用颜色处理库如 color 或 chroma-js
    // 简化实现，仅处理基本情况
    if (format === 'hex' && !color.startsWith('#')) {
      return `#${color}`;
    }
    return color;
  } catch (error) {
    console.error('Color formatting error:', error);
    return color;
  }
};

// 格式化IP地址
export const formatIpAddress = (ip: string): string => {
  try {
    // IPv4 格式化
    if (/^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(ip)) {
      return ip.split('.').map(part => parseInt(part, 10)).join('.');
    }

    // IPv6 格式化（简化）
    if (ip.includes(':')) {
      return ip.toLowerCase();
    }

    return ip;
  } catch (error) {
    console.error('IP formatting error:', error);
    return ip;
  }
};

// 生成随机ID
export const generateId = (prefix: string = '', length: number = 8): string => {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789';
  let result = '';

  for (let i = 0; i < length; i++) {
    result += chars.charAt(Math.floor(Math.random() * chars.length));
  }

  return prefix ? `${prefix}_${result}` : result;
};

// 生成UUID (简化版)
export const generateUUID = (): string => {
  return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
    const r = Math.random() * 16 | 0;
    const v = c === 'x' ? r : (r & 0x3 | 0x8);
    return v.toString(16);
  });
};

export default {
  formatNumber,
  formatCurrency,
  formatPercentage,
  formatFileSize,
  formatDuration,
  truncateText,
  formatPhoneNumber,
  formatCardNumber,
  formatIdCard,
  capitalize,
  capitalizeWords,
  toCamelCase,
  toSnakeCase,
  toKebabCase,
  formatJSON,
  compressJSON,
  stripHtml,
  escapeHtml,
  unescapeHtml,
  formatUrl,
  extractDomain,
  formatColor,
  formatIpAddress,
  generateId,
  generateUUID,
};