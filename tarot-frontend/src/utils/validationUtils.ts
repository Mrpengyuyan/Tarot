import { REGEX } from './constants';

// 基础验证接口
export interface ValidationResult {
  isValid: boolean;
  error?: string;
}

// 验证规则类型
export type ValidationRule<T = any> = (value: T) => ValidationResult | Promise<ValidationResult>;

// 创建验证规则
export const createValidator = <T>(
  predicate: (value: T) => boolean,
  errorMessage: string
): ValidationRule<T> => {
  return (value: T): ValidationResult => ({
    isValid: predicate(value),
    error: predicate(value) ? undefined : errorMessage,
  });
};

// 组合多个验证规则
export const combineValidators = <T>(...validators: ValidationRule<T>[]): ValidationRule<T> => {
  return async (value: T): Promise<ValidationResult> => {
    for (const validator of validators) {
      const result = await validator(value);
      if (!result.isValid) {
        return result;
      }
    }
    return { isValid: true };
  };
};

// 常用验证规则

// 必填验证
export const required = createValidator(
  (value: any) => {
    if (value === null || value === undefined) return false;
    if (typeof value === 'string') return value.trim().length > 0;
    if (Array.isArray(value)) return value.length > 0;
    return true;
  },
  '此字段为必填项'
);

// 最小长度验证
export const minLength = (min: number) => createValidator(
  (value: string) => !value || value.length >= min,
  `最少需要${min}个字符`
);

// 最大长度验证
export const maxLength = (max: number) => createValidator(
  (value: string) => !value || value.length <= max,
  `最多只能输入${max}个字符`
);

// 长度范围验证
export const lengthRange = (min: number, max: number) => createValidator(
  (value: string) => !value || (value.length >= min && value.length <= max),
  `长度必须在${min}-${max}个字符之间`
);

// 最小值验证
export const minValue = (min: number) => createValidator(
  (value: number) => value == null || value >= min,
  `值不能小于${min}`
);

// 最大值验证
export const maxValue = (max: number) => createValidator(
  (value: number) => value == null || value <= max,
  `值不能大于${max}`
);

// 数值范围验证
export const valueRange = (min: number, max: number) => createValidator(
  (value: number) => value == null || (value >= min && value <= max),
  `值必须在${min}-${max}之间`
);

// 正整数验证
export const positiveInteger = createValidator(
  (value: number | string) => {
    if (value == null || value === '') return true;
    const num = typeof value === 'string' ? parseInt(value, 10) : value;
    return Number.isInteger(num) && num > 0;
  },
  '请输入正整数'
);

// 非负数验证
export const nonNegative = createValidator(
  (value: number | string) => {
    if (value == null || value === '') return true;
    const num = typeof value === 'string' ? parseFloat(value) : value;
    return !isNaN(num) && num >= 0;
  },
  '请输入非负数'
);

// 正则表达式验证
export const pattern = (regex: RegExp, message: string) => createValidator(
  (value: string) => !value || regex.test(value),
  message
);

// 邮箱验证
export const email = pattern(REGEX.EMAIL, '请输入有效的邮箱地址');

// 手机号验证
export const phone = pattern(REGEX.PHONE, '请输入有效的手机号码');

// 密码强度验证
export const password = createValidator(
  (value: string) => {
    if (!value) return true;

    // 至少8位，包含大小写字母和数字
    return value.length >= 8 &&
           /[a-z]/.test(value) &&
           /[A-Z]/.test(value) &&
           /\d/.test(value);
  },
  '密码至少8位，必须包含大小写字母和数字'
);

// 简单密码验证（仅长度）
export const simplePassword = minLength(6);

// 用户名验证
export const username = pattern(REGEX.USERNAME, '用户名只能包含字母、数字和下划线，3-16位');

// 中文姓名验证
export const chineseName = pattern(REGEX.CHINESE_NAME, '请输入有效的中文姓名（2-8个字符）');

// URL验证
export const url = pattern(REGEX.URL, '请输入有效的网址');

// 十六进制颜色验证
export const hexColor = pattern(REGEX.HEX_COLOR, '请输入有效的十六进制颜色值');

// 确认密码验证
export const confirmPassword = (password: string) => createValidator(
  (confirmValue: string) => confirmValue === password,
  '两次输入的密码不一致'
);

// 文件类型验证
export const fileType = (allowedTypes: string[]) => createValidator(
  (file: File) => {
    if (!file) return true;
    const fileExtension = file.name.split('.').pop()?.toLowerCase();
    return fileExtension ? allowedTypes.includes(fileExtension) : false;
  },
  `只能上传以下类型的文件：${allowedTypes.join(', ')}`
);

// 文件大小验证
export const fileSize = (maxSizeInMB: number) => createValidator(
  (file: File) => {
    if (!file) return true;
    const maxSizeInBytes = maxSizeInMB * 1024 * 1024;
    return file.size <= maxSizeInBytes;
  },
  `文件大小不能超过${maxSizeInMB}MB`
);

// 身份证验证
export const idCard = createValidator(
  (value: string) => {
    if (!value) return true;

    const cleanValue = value.replace(/\s/g, '');

    // 18位身份证验证
    if (cleanValue.length === 18) {
      const weights = [7, 9, 10, 5, 8, 4, 2, 1, 6, 3, 7, 9, 10, 5, 8, 4, 2];
      const checkCodes = ['1', '0', 'X', '9', '8', '7', '6', '5', '4', '3', '2'];

      let sum = 0;
      for (let i = 0; i < 17; i++) {
        sum += parseInt(cleanValue[i]) * weights[i];
      }

      const checkCode = checkCodes[sum % 11];
      return cleanValue[17].toUpperCase() === checkCode;
    }

    // 15位身份证（老版本）
    if (cleanValue.length === 15) {
      return /^\d{15}$/.test(cleanValue);
    }

    return false;
  },
  '请输入有效的身份证号码'
);

// 银行卡号验证（Luhn算法）
export const bankCard = createValidator(
  (value: string) => {
    if (!value) return true;

    const cleanValue = value.replace(/\s/g, '');

    if (!/^\d+$/.test(cleanValue) || cleanValue.length < 13 || cleanValue.length > 19) {
      return false;
    }

    // Luhn算法验证
    let sum = 0;
    let alternate = false;

    for (let i = cleanValue.length - 1; i >= 0; i--) {
      let n = parseInt(cleanValue[i]);

      if (alternate) {
        n *= 2;
        if (n > 9) {
          n = (n % 10) + 1;
        }
      }

      sum += n;
      alternate = !alternate;
    }

    return sum % 10 === 0;
  },
  '请输入有效的银行卡号'
);

// IP地址验证
export const ipAddress = createValidator(
  (value: string) => {
    if (!value) return true;

    // IPv4验证
    const ipv4Regex = /^(\d{1,3}\.){3}\d{1,3}$/;
    if (ipv4Regex.test(value)) {
      const parts = value.split('.');
      return parts.every(part => {
        const num = parseInt(part, 10);
        return num >= 0 && num <= 255;
      });
    }

    // IPv6验证（简化）
    const ipv6Regex = /^([0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}$/;
    return ipv6Regex.test(value);
  },
  '请输入有效的IP地址'
);

// 日期格式验证
export const dateFormat = (format: string = 'YYYY-MM-DD') => createValidator(
  (value: string) => {
    if (!value) return true;

    // 简化的日期格式验证
    if (format === 'YYYY-MM-DD') {
      const dateRegex = /^\d{4}-\d{2}-\d{2}$/;
      if (!dateRegex.test(value)) return false;

      const date = new Date(value);
      return date instanceof Date && !isNaN(date.getTime()) &&
             value === date.toISOString().split('T')[0];
    }

    return true;
  },
  `请输入正确的日期格式 (${format})`
);

// 年龄验证
export const age = (min: number = 0, max: number = 150) => createValidator(
  (birthDate: string) => {
    if (!birthDate) return true;

    const birth = new Date(birthDate);
    const today = new Date();

    if (birth > today) return false;

    const ageValue = today.getFullYear() - birth.getFullYear();
    const monthDiff = today.getMonth() - birth.getMonth();

    const actualAge = monthDiff < 0 || (monthDiff === 0 && today.getDate() < birth.getDate())
      ? ageValue - 1
      : ageValue;

    return actualAge >= min && actualAge <= max;
  },
  `年龄必须在${min}-${max}岁之间`
);

// 数组验证
export const arrayMinLength = (min: number) => createValidator(
  (value: any[]) => !value || value.length >= min,
  `至少需要选择${min}项`
);

export const arrayMaxLength = (max: number) => createValidator(
  (value: any[]) => !value || value.length <= max,
  `最多只能选择${max}项`
);

// JSON格式验证
export const jsonFormat = createValidator(
  (value: string) => {
    if (!value) return true;
    try {
      JSON.parse(value);
      return true;
    } catch {
      return false;
    }
  },
  '请输入有效的JSON格式'
);

// 自定义验证器工厂
export const custom = (
  validator: (value: any) => boolean | Promise<boolean>,
  message: string
): ValidationRule => {
  return async (value: any): Promise<ValidationResult> => {
    try {
      const isValid = await validator(value);
      return { isValid, error: isValid ? undefined : message };
    } catch (error) {
      return { isValid: false, error: message };
    }
  };
};

// 条件验证
export const when = <T>(
  condition: (value: T) => boolean,
  validator: ValidationRule<T>
): ValidationRule<T> => {
  return async (value: T): Promise<ValidationResult> => {
    if (condition(value)) {
      return await validator(value);
    }
    return { isValid: true };
  };
};

// 验证对象的某个字段
export const validateField = async (
  object: Record<string, any>,
  field: string,
  validators: ValidationRule[]
): Promise<ValidationResult> => {
  const value = object[field];

  for (const validator of validators) {
    const result = await validator(value);
    if (!result.isValid) {
      return { isValid: false, error: result.error };
    }
  }

  return { isValid: true };
};

// 验证整个对象
export const validateObject = async (
  object: Record<string, any>,
  rules: Record<string, ValidationRule[]>
): Promise<Record<string, ValidationResult>> => {
  const results: Record<string, ValidationResult> = {};

  for (const [field, validators] of Object.entries(rules)) {
    results[field] = await validateField(object, field, validators);
  }

  return results;
};

// 检查对象是否所有字段都验证通过
export const isObjectValid = (
  validationResults: Record<string, ValidationResult>
): boolean => {
  return Object.values(validationResults).every(result => result.isValid);
};

export default {
  createValidator,
  combineValidators,
  required,
  minLength,
  maxLength,
  lengthRange,
  minValue,
  maxValue,
  valueRange,
  positiveInteger,
  nonNegative,
  pattern,
  email,
  phone,
  password,
  simplePassword,
  username,
  chineseName,
  url,
  hexColor,
  confirmPassword,
  fileType,
  fileSize,
  idCard,
  bankCard,
  ipAddress,
  dateFormat,
  age,
  arrayMinLength,
  arrayMaxLength,
  jsonFormat,
  custom,
  when,
  validateField,
  validateObject,
  isObjectValid,
};