// 数学计算工具函数

// 基础数学函数

// 生成指定范围内的随机整数
export const randomInt = (min: number, max: number): number => {
  return Math.floor(Math.random() * (max - min + 1)) + min;
};

// 生成指定范围内的随机浮点数
export const randomFloat = (min: number, max: number): number => {
  return Math.random() * (max - min) + min;
};

// 生成随机布尔值
export const randomBoolean = (probability: number = 0.5): boolean => {
  return Math.random() < probability;
};

// 从数组中随机选择一个元素
export const randomChoice = <T>(array: T[]): T => {
  if (array.length === 0) {
    throw new Error('Cannot choose from empty array');
  }
  return array[Math.floor(Math.random() * array.length)];
};

// 从数组中随机选择多个元素（不重复）
export const randomSample = <T>(array: T[], count: number): T[] => {
  if (count > array.length) {
    throw new Error('Sample size cannot be larger than array length');
  }

  const shuffled = [...array].sort(() => Math.random() - 0.5);
  return shuffled.slice(0, count);
};

// 洗牌算法（Fisher-Yates）
export const shuffle = <T>(array: T[]): T[] => {
  const result = [...array];

  for (let i = result.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [result[i], result[j]] = [result[j], result[i]];
  }

  return result;
};

// 数值处理

// 限制数值在指定范围内
export const clamp = (value: number, min: number, max: number): number => {
  return Math.min(Math.max(value, min), max);
};

// 线性插值
export const lerp = (start: number, end: number, t: number): number => {
  return start + (end - start) * t;
};

// 将值从一个范围映射到另一个范围
export const map = (
  value: number,
  fromMin: number,
  fromMax: number,
  toMin: number,
  toMax: number
): number => {
  const normalized = (value - fromMin) / (fromMax - fromMin);
  return toMin + normalized * (toMax - toMin);
};

// 四舍五入到指定小数位
export const round = (value: number, decimals: number = 0): number => {
  const multiplier = Math.pow(10, decimals);
  return Math.round(value * multiplier) / multiplier;
};

// 向上舍入到指定小数位
export const roundUp = (value: number, decimals: number = 0): number => {
  const multiplier = Math.pow(10, decimals);
  return Math.ceil(value * multiplier) / multiplier;
};

// 向下舍入到指定小数位
export const roundDown = (value: number, decimals: number = 0): number => {
  const multiplier = Math.pow(10, decimals);
  return Math.floor(value * multiplier) / multiplier;
};

// 角度转换

// 角度转弧度
export const degToRad = (degrees: number): number => {
  return (degrees * Math.PI) / 180;
};

// 弧度转角度
export const radToDeg = (radians: number): number => {
  return (radians * 180) / Math.PI;
};

// 标准化角度到 0-360 范围
export const normalizeAngle = (angle: number): number => {
  angle = angle % 360;
  return angle < 0 ? angle + 360 : angle;
};

// 距离计算

// 计算两点之间的直线距离
export const distance = (
  x1: number,
  y1: number,
  x2: number,
  y2: number
): number => {
  return Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2);
};

// 计算两点之间的曼哈顿距离
export const manhattanDistance = (
  x1: number,
  y1: number,
  x2: number,
  y2: number
): number => {
  return Math.abs(x2 - x1) + Math.abs(y2 - y1);
};

// 3D空间中两点之间的距离
export const distance3D = (
  x1: number,
  y1: number,
  z1: number,
  x2: number,
  y2: number,
  z2: number
): number => {
  return Math.sqrt((x2 - x1) ** 2 + (y2 - y1) ** 2 + (z2 - z1) ** 2);
};

// 统计函数

// 计算平均值
export const average = (numbers: number[]): number => {
  if (numbers.length === 0) return 0;
  return sum(numbers) / numbers.length;
};

// 计算中位数
export const median = (numbers: number[]): number => {
  if (numbers.length === 0) return 0;

  const sorted = [...numbers].sort((a, b) => a - b);
  const middle = Math.floor(sorted.length / 2);

  if (sorted.length % 2 === 0) {
    return (sorted[middle - 1] + sorted[middle]) / 2;
  }

  return sorted[middle];
};

// 计算模式（出现次数最多的数）
export const mode = (numbers: number[]): number[] => {
  if (numbers.length === 0) return [];

  const frequency: { [key: number]: number } = {};

  numbers.forEach(num => {
    frequency[num] = (frequency[num] || 0) + 1;
  });

  const maxFreq = Math.max(...Object.values(frequency));
  return Object.keys(frequency)
    .filter(key => frequency[Number(key)] === maxFreq)
    .map(Number);
};

// 计算总和
export const sum = (numbers: number[]): number => {
  return numbers.reduce((acc, num) => acc + num, 0);
};

// 计算最小值
export const min = (numbers: number[]): number => {
  if (numbers.length === 0) return 0;
  return Math.min(...numbers);
};

// 计算最大值
export const max = (numbers: number[]): number => {
  if (numbers.length === 0) return 0;
  return Math.max(...numbers);
};

// 计算方差
export const variance = (numbers: number[]): number => {
  if (numbers.length === 0) return 0;

  const avg = average(numbers);
  const squaredDiffs = numbers.map(num => (num - avg) ** 2);
  return average(squaredDiffs);
};

// 计算标准差
export const standardDeviation = (numbers: number[]): number => {
  return Math.sqrt(variance(numbers));
};

// 几何函数

// 计算圆的周长
export const circumference = (radius: number): number => {
  return 2 * Math.PI * radius;
};

// 计算圆的面积
export const circleArea = (radius: number): number => {
  return Math.PI * radius ** 2;
};

// 计算矩形面积
export const rectangleArea = (width: number, height: number): number => {
  return width * height;
};

// 计算三角形面积（海伦公式）
export const triangleArea = (a: number, b: number, c: number): number => {
  const s = (a + b + c) / 2;
  return Math.sqrt(s * (s - a) * (s - b) * (s - c));
};

// 检查点是否在圆内
export const isPointInCircle = (
  pointX: number,
  pointY: number,
  centerX: number,
  centerY: number,
  radius: number
): boolean => {
  return distance(pointX, pointY, centerX, centerY) <= radius;
};

// 检查点是否在矩形内
export const isPointInRectangle = (
  pointX: number,
  pointY: number,
  rectX: number,
  rectY: number,
  rectWidth: number,
  rectHeight: number
): boolean => {
  return pointX >= rectX &&
         pointX <= rectX + rectWidth &&
         pointY >= rectY &&
         pointY <= rectY + rectHeight;
};

// 塔罗相关的数学函数

// 生成塔罗牌抽取序列（避免重复）
export const generateTarotDrawSequence = (
  totalCards: number,
  drawCount: number
): number[] => {
  if (drawCount > totalCards) {
    throw new Error('Cannot draw more cards than available');
  }

  const cardIndices = Array.from({ length: totalCards }, (_, i) => i);
  return randomSample(cardIndices, drawCount);
};

// 计算塔罗牌的正逆位置（基于随机概率）
export const generateCardOrientation = (
  uprightProbability: number = 0.7
): 'upright' | 'reversed' => {
  return randomBoolean(uprightProbability) ? 'upright' : 'reversed';
};

// 生成神秘学数字（基于生日或其他输入）
export const calculateLifePathNumber = (birthDate: Date): number => {
  const dateString = birthDate.toISOString().slice(0, 10).replace(/-/g, '');
  let sum = dateString.split('').reduce((acc, digit) => acc + parseInt(digit), 0);

  // 减少到单个数字（除了主数字11, 22, 33）
  while (sum > 9 && ![11, 22, 33].includes(sum)) {
    sum = sum.toString().split('').reduce((acc, digit) => acc + parseInt(digit), 0);
  }

  return sum;
};

// 计算幸运数字
export const calculateLuckyNumbers = (seed: string | number, count: number = 3): number[] => {
  const seedString = seed.toString();
  let hash = 0;

  for (let i = 0; i < seedString.length; i++) {
    const char = seedString.charCodeAt(i);
    hash = ((hash << 5) - hash) + char;
    hash = hash & hash; // Convert to 32-bit integer
  }

  const numbers: number[] = [];
  let current = Math.abs(hash);

  for (let i = 0; i < count; i++) {
    current = (current * 1103515245 + 12345) & 0x7fffffff;
    numbers.push((current % 99) + 1); // 1-99 range
  }

  return numbers;
};

// 概率和权重函数

// 基于权重的随机选择
export const weightedChoice = <T>(
  items: T[],
  weights: number[]
): T => {
  if (items.length !== weights.length) {
    throw new Error('Items and weights arrays must have the same length');
  }

  if (items.length === 0) {
    throw new Error('Cannot choose from empty array');
  }

  const totalWeight = weights.reduce((sum, weight) => sum + weight, 0);
  let random = Math.random() * totalWeight;

  for (let i = 0; i < items.length; i++) {
    random -= weights[i];
    if (random <= 0) {
      return items[i];
    }
  }

  return items[items.length - 1];
};

// 正态分布随机数生成（Box-Muller变换）
export const normalRandom = (mean: number = 0, stdDev: number = 1): number => {
  const u1 = Math.random();
  const u2 = Math.random();

  const z0 = Math.sqrt(-2 * Math.log(u1)) * Math.cos(2 * Math.PI * u2);
  return z0 * stdDev + mean;
};

// 泊松分布随机数生成
export const poissonRandom = (lambda: number): number => {
  const L = Math.exp(-lambda);
  let k = 0;
  let p = 1;

  do {
    k++;
    p *= Math.random();
  } while (p > L);

  return k - 1;
};

// 工具函数

// 检查数字是否为质数
export const isPrime = (num: number): boolean => {
  if (num < 2) return false;
  if (num === 2) return true;
  if (num % 2 === 0) return false;

  for (let i = 3; i * i <= num; i += 2) {
    if (num % i === 0) return false;
  }

  return true;
};

// 计算最大公约数
export const gcd = (a: number, b: number): number => {
  return b === 0 ? a : gcd(b, a % b);
};

// 计算最小公倍数
export const lcm = (a: number, b: number): number => {
  return (a * b) / gcd(a, b);
};

// 阶乘
export const factorial = (n: number): number => {
  if (n < 0) return 0;
  if (n === 0 || n === 1) return 1;
  return n * factorial(n - 1);
};

// 组合数 C(n, k)
export const combination = (n: number, k: number): number => {
  if (k > n || k < 0) return 0;
  return factorial(n) / (factorial(k) * factorial(n - k));
};

// 排列数 P(n, k)
export const permutation = (n: number, k: number): number => {
  if (k > n || k < 0) return 0;
  return factorial(n) / factorial(n - k);
};

export default {
  randomInt,
  randomFloat,
  randomBoolean,
  randomChoice,
  randomSample,
  shuffle,
  clamp,
  lerp,
  map,
  round,
  roundUp,
  roundDown,
  degToRad,
  radToDeg,
  normalizeAngle,
  distance,
  manhattanDistance,
  distance3D,
  average,
  median,
  mode,
  sum,
  min,
  max,
  variance,
  standardDeviation,
  circumference,
  circleArea,
  rectangleArea,
  triangleArea,
  isPointInCircle,
  isPointInRectangle,
  generateTarotDrawSequence,
  generateCardOrientation,
  calculateLifePathNumber,
  calculateLuckyNumbers,
  weightedChoice,
  normalRandom,
  poissonRandom,
  isPrime,
  gcd,
  lcm,
  factorial,
  combination,
  permutation,
};