# -*- coding: utf-8 -*-
"""
增强版塔罗牌解读服务
支持不同牌阵的智能解读，包括牌阵位置含义和牌间联系分析
"""
import json
import logging
from typing import Dict, List, Any, Optional
from app.services.coze_service import coze_service, CozeError
from app.models.tarot_card import TarotCard
from app.models.spread import SpreadType

logger = logging.getLogger(__name__)

class EnhancedTarotInterpretationService:
    """增强版塔罗牌解读服务"""

    def __init__(self):
        self.coze_service = coze_service

        # 不同牌阵的解读模板
        self.spread_templates = {
            "单张牌": self._get_single_card_template,
            "三张牌": self._get_three_card_template,
            "过去现在未来": self._get_three_card_template,
            "凯尔特十字": self._get_celtic_cross_template,
            "五张牌": self._get_five_card_template,
            "马蹄形": self._get_horseshoe_template,
            "生命之树": self._get_tree_of_life_template,
        }

    async def get_spread_aware_interpretation(
        self,
        question: str,
        question_type: str,
        spread: Dict[str, Any],  # 包含牌阵信息
        cards_drawn: List[Dict[str, Any]],  # 包含牌面信息和位置
        user_context: Optional[str] = None,
        user_id: str = "user"
    ) -> Dict[str, Any]:
        """
        获取牌阵感知的塔罗牌解读

        Args:
            question: 用户问题
            question_type: 问题类型 (love, career, finance, health, general)
            spread: 牌阵信息 {id, name, description, positions}
            cards_drawn: 抽取的牌面信息，包含位置信息
            user_context: 用户背景信息
            user_id: 用户ID

        Returns:
            完整的解读结果字典
        """
        if not self.coze_service.is_configured():
            raise CozeError("Coze服务未配置，请检查API密钥和Bot ID")

        try:
            # 构建牌阵感知的AI消息
            ai_message = self._build_spread_aware_message(
                question, question_type, spread, cards_drawn, user_context
            )

            logger.info("Sending spread-aware interpretation request to AI service")

            # 调用Coze AI获取解读
            ai_response = await self.coze_service.send_message_and_wait(
                message=ai_message,
                user_id=user_id,
                max_wait_time=60  # 复杂牌阵解读需要更长时间
            )

            # 解析AI回复
            interpretation = self._parse_spread_aware_response(
                ai_response, question, question_type, spread, cards_drawn
            )

            logger.info(f"成功获取{spread['name']}的塔罗牌解读")
            return interpretation

        except CozeError as e:
            logger.error(f"获取塔罗牌解读失败: {str(e)}")
            raise
        except Exception as e:
            logger.error(f"塔罗牌解读服务异常: {str(e)}")
            raise CozeError(f"解读服务异常: {str(e)}")

    def _build_spread_aware_message(
        self,
        question: str,
        question_type: str,
        spread: Dict[str, Any],
        cards_drawn: List[Dict[str, Any]],
        user_context: Optional[str] = None
    ) -> str:
        """
        构建牌阵感知的AI消息
        """
        spread_name = spread.get('name', '未知牌阵')
        spread_description = spread.get('description', '')

        # 构建牌面信息
        cards_info = []
        for card in cards_drawn:
            card_name = card.get('name', card.get('card_name', 'Unknown'))
            is_reversed = card.get('is_reversed', card.get('reversed', False))
            position = card.get('position', card.get('position_name', '未知位置'))
            position_meaning = card.get('position_meaning', '')

            orientation = "逆位" if is_reversed else "正位"
            card_info = f"{card_name}({orientation}) - {position}"
            if position_meaning:
                card_info += f" (含义: {position_meaning})"
            cards_info.append(card_info)

        # 获取牌阵特定的解读模板
        template = self.spread_templates.get(spread_name, self._get_generic_template)

        # 构建完整的提示词
        message = f"""你是一位专业的塔罗占卜师，请根据以下信息提供深度解读：

【占卜信息】
用户问题：{question}
问题类型：{question_type}
使用牌阵：{spread_name}
牌阵描述：{spread_description}

【抽取牌面】
{chr(10).join([f"{i+1}. {info}" for i, info in enumerate(cards_info)])}

【用户背景】
{user_context or '无额外背景信息'}

{template}

请严格按照JSON格式返回解读结果，包含整体分析、每张牌的详细解读、牌间联系分析、核心洞察和行动建议。"""

        return message

    def _get_single_card_template(self) -> str:
        """单张牌解读模板"""
        return """
【解读要求】
针对单张牌的解读，请提供：
1. 牌面在当前问题中的核心含义
2. 正位/逆位的具体影响
3. 对问题的直接回答和建议
4. 需要注意的事项"""

    def _get_three_card_template(self) -> str:
        """三张牌解读模板"""
        return """
【解读要求】
针对过去-现在-未来三张牌，请提供：
1. 过去牌：如何影响当前状况
2. 现在牌：当前状况的核心要素
3. 未来牌：可能的发展趋势
4. 三张牌的时间线联系和发展脉络
5. 基于时间线的建议"""

    def _get_celtic_cross_template(self) -> str:
        """凯尔特十字解读模板"""
        return """
【解读要求】
针对凯尔特十字牌阵（10张牌），请按位置解读：
1. 中心牌：当前状况的核心
2. 挑战牌：当前的障碍
3. 远景牌：远期目标
4. 基础牌：过去的根源
5. 过去牌：最近的影响
6. 未来牌：即将到来的影响
7. 自我牌：自身态度和位置
8. 外界牌：外部环境和他人影响
9. 希望恐惧牌：内心希望和恐惧
10. 结果牌：最终可能结果

请分析各位置间的相互关系，提供整体的命运指引。"""

    def _get_five_card_template(self) -> str:
        """五张牌解读模板"""
        return """
【解读要求】
针对五张牌阵，请提供：
1. 每张牌在其位置的具体含义
2. 五张牌构成的整体图景
3. 关键转折点和影响因素
4. 问题解决的方向建议"""

    def _get_horseshoe_template(self) -> str:
        """马蹄形牌阵解读模板"""
        return """
【解读要求】
针对马蹄形牌阵（7张牌），请按顺序解读：
1-3. 过去的影响因素
4-5. 现在的状况
6-7. 未来的可能发展
请呈现马蹄形的命运轨迹和转折点。"""

    def _get_tree_of_life_template(self) -> str:
        """生命之树牌阵解读模板"""
        return """
【解读要求】
针对生命之树牌阵（10张牌），请按卡巴拉路径解读：
1-3. 三个支柱（慈悲、严厉、温和）
4-10. 七个原质的体现
请分析精神成长和灵魂进化的路径。"""

    def _get_generic_template(self) -> str:
        """通用解读模板"""
        return """
【解读要求】
请根据牌阵的特点：
1. 分析每张牌在其位置的含义
2. 探讨牌与牌之间的联系
3. 提供整体的综合解读
4. 给出实用的建议和指导"""

    def _parse_spread_aware_response(
        self,
        ai_response: str,
        question: str,
        question_type: str,
        spread: Dict[str, Any],
        cards_drawn: List[Dict[str, Any]]
    ) -> Dict[str, Any]:
        """
        解析AI回复，提取结构化的解读结果
        """
        try:
            # 尝试提取JSON内容
            json_content = self._extract_json_from_response(ai_response)

            if json_content:
                # 验证和补充必要字段
                interpretation = self._validate_and_enhance_response(
                    json_content, question, question_type, spread, cards_drawn
                )
                return interpretation
            else:
                # 如果无法解析JSON，创建增强的备选格式
                return self._create_enhanced_fallback_response(
                    ai_response, question, question_type, spread, cards_drawn
                )

        except Exception as e:
            logger.warning(f"解析AI回复时出错: {str(e)}, 使用增强备选格式")
            return self._create_enhanced_fallback_response(
                ai_response, question, question_type, spread, cards_drawn
            )

    def _extract_json_from_response(self, response: str) -> Optional[Dict[str, Any]]:
        """从AI回复中提取JSON内��"""
        try:
            # 方法1: 查找JSON代码块
            if "```json" in response:
                start = response.find("```json") + 7
                end = response.find("```", start)
                if end != -1:
                    json_str = response[start:end].strip()
                    return json.loads(json_str)

            # 方法2: 查找花括号包围的内容
            if "{" in response and "}" in response:
                start = response.find("{")
                end = response.rfind("}") + 1
                json_str = response[start:end]
                return json.loads(json_str)

            # 方法3: 尝试直接解析整个回复
            return json.loads(response.strip())

        except json.JSONDecodeError:
            return None
        except Exception:
            return None

    def _validate_and_enhance_response(
        self,
        json_data: Dict[str, Any],
        question: str,
        question_type: str,
        spread: Dict[str, Any],
        cards_drawn: List[Dict[str, Any]]
    ) -> Dict[str, Any]:
        """验证和增强AI返回的JSON数据"""
        import time
        import uuid

        # 生成默认值
        default_reading_id = str(uuid.uuid4())[:8]
        default_timestamp = int(time.time())

        # 确保基础字段存在
        result = {
            "reading_id": json_data.get("reading_id", default_reading_id),
            "timestamp": json_data.get("timestamp", default_timestamp),
            "question": json_data.get("question", question),
            "question_type": json_data.get("question_type", question_type),
            "spread_type": json_data.get("spread_type", spread.get('name')),
            "spread_info": {
                "id": spread.get('id'),
                "name": spread.get('name'),
                "description": spread.get('description'),
                "card_count": len(cards_drawn)
            },
            "cards_drawn": json_data.get("cards_drawn", self._format_cards_data(cards_drawn)),
            "interpretation": json_data.get("interpretation", {}),
            "conclusion": json_data.get("conclusion", "感谢您的咨询，愿塔罗为您带来智慧与指引。")
        }

        # 增强interpretation字段的结构
        interpretation = result["interpretation"]
        if not isinstance(interpretation, dict):
            interpretation = {}

        # 确保所有必要的解读字段存在
        interpretation.setdefault("overview", "")
        interpretation.setdefault("card_details", [])
        interpretation.setdefault("card_connections", "")
        interpretation.setdefault("spread_analysis", "")
        interpretation.setdefault("core_insights", [])
        interpretation.setdefault("action_recommendations", {})
        interpretation.setdefault("timing_guidance", "")

        result["interpretation"] = interpretation

        return result

    def _format_cards_data(self, cards_drawn: List[Dict[str, Any]]) -> List[Dict[str, Any]]:
        """格式化牌面数据"""
        formatted_cards = []
        for card in cards_drawn:
            formatted_card = {
                "name": card.get('name', card.get('card_name', 'Unknown')),
                "position": card.get('position', card.get('position_name', 'Unknown')),
                "is_reversed": card.get('is_reversed', card.get('reversed', False)),
                "position_meaning": card.get('position_meaning', ''),
                "card_meaning": card.get('card_meaning', '')
            }
            formatted_cards.append(formatted_card)
        return formatted_cards

    def _create_enhanced_fallback_response(
        self,
        ai_response: str,
        question: str,
        question_type: str,
        spread: Dict[str, Any],
        cards_drawn: List[Dict[str, Any]]
    ) -> Dict[str, Any]:
        """创建增强的备选格式解读结果"""
        import time
        import uuid

        return {
            "reading_id": str(uuid.uuid4())[:8],
            "timestamp": int(time.time()),
            "question": question,
            "question_type": question_type,
            "spread_type": spread.get('name'),
            "spread_info": {
                "id": spread.get('id'),
                "name": spread.get('name'),
                "description": spread.get('description'),
                "card_count": len(cards_drawn)
            },
            "cards_drawn": self._format_cards_data(cards_drawn),
            "interpretation": {
                "overview": ai_response[:800] + "..." if len(ai_response) > 800 else ai_response,
                "card_details": [],
                "card_connections": "请参考上述AI解读内容分析牌面之间的联系。",
                "spread_analysis": f"基于{spread.get('name')}牌阵的整体分析。",
                "core_insights": ["AI提供了专业解读，请仔细阅读并思考其中的指导意义。"],
                "action_recommendations": {
                    "immediate": ["根据解读内容调整当前的心态和行为"],
                    "medium_term": ["持续关注解读中提到的发展趋势"],
                    "avoid": ["避免盲目行动，保持理性思考"],
                    "enhance": ["培养直觉能力，保持内心的平静"]
                },
                "timing_guidance": "解读建议在当前时期内保持耐心，等待合适时机行动。"
            },
            "conclusion": "AI已为您提供了基于牌阵特点的专业解读，请结合自身情况仔细思考。"
        }

    async def create_interpretation(
        self,
        db,
        prediction,
        cards_data: List[Dict[str, Any]],
        user_context: Optional[str] = None
    ) -> str:
        """
        为预测创建AI解读（兼容原有接口）

        Args:
            db: 数据库会话
            prediction: 预测记录
            cards_data: 牌面数据
            user_context: 用户背景信息

        Returns:
            AI解读文本
        """
        try:
            # 获取牌阵信息
            spread_info = {
                'id': prediction.spread_type.id,
                'name': prediction.spread_type.name,
                'description': prediction.spread_type.description
            }

            # 构建牌面数据
            cards_drawn = []
            for card_info in cards_data:
                card = card_info.get('card')
                if card:
                    cards_drawn.append({
                        'name': card.name_zh,
                        'position': card_info.get('position', 'Unknown'),
                        'is_reversed': card_info.get('is_reversed', False),
                        'position_meaning': card_info.get('position_meaning', ''),
                        'card_meaning': card.upright_meaning if not card_info.get('is_reversed') else card.reversed_meaning
                    })

            # 调用增强解读服务
            result = await self.get_spread_aware_interpretation(
                question=prediction.question,
                question_type=prediction.question_type.value if prediction.question_type else "general",
                spread=spread_info,
                cards_drawn=cards_drawn,
                user_context=user_context,
                user_id=str(prediction.user_id)
            )

            # 提取整体解读文本
            overview = result.get('interpretation', {}).get('overview', '')
            if not overview:
                # 如果没有overview，则组合其他字段
                interpretation_parts = []
                interpretation = result.get('interpretation', {})

                if interpretation.get('spread_analysis'):
                    interpretation_parts.append(interpretation['spread_analysis'])
                if interpretation.get('card_connections'):
                    interpretation_parts.append(interpretation['card_connections'])
                if interpretation.get('core_insights'):
                    interpretation_parts.extend(interpretation['core_insights'])

                overview = '。'.join(interpretation_parts)

            return overview or "AI解读服务暂时不可用，请稍后重试。"

        except Exception as e:
            logger.error(f"创建AI解读失败: {str(e)}")
            return f"AI解读服务出现异常: {str(e)}"

    async def health_check(self) -> Dict[str, Any]:
        """检查增强塔罗解读服务健康状态"""
        # 首先检查Coze服务状态
        coze_status = await self.coze_service.health_check()

        if not coze_status["is_healthy"]:
            return {
                "service_name": "增强塔罗解读服务",
                "status": coze_status["status"],
                "message": f"Coze服务不可用: {coze_status['message']}",
                "is_healthy": False,
                "details": coze_status["details"]
            }

        try:
            # 使用复杂的测试用例
            test_spread = {
                'id': 1,
                'name': '三张牌',
                'description': '过去-现在-未来时间线分析'
            }

            test_cards = [
                {
                    'name': '愚者',
                    'position': '过去',
                    'is_reversed': False,
                    'position_meaning': '过去的起点和开始'
                },
                {
                    'name': '魔术师',
                    'position': '现在',
                    'is_reversed': False,
                    'position_meaning': '当前的能力和资源'
                },
                {
                    'name': '世界',
                    'position': '未来',
                    'is_reversed': False,
                    'position_meaning': '未来的完成和成就'
                }
            ]

            logger.info("开始增强塔罗解读服务健康检查...")
            result = await self.get_spread_aware_interpretation(
                question="这是一个健康检查测试：我的事业发展如何？",
                question_type="career",
                spread=test_spread,
                cards_drawn=test_cards,
                user_context="健康检查测试",
                user_id="health_check"
            )

            if result and result.get("interpretation"):
                logger.info("增强塔罗解读服务健康检查通过")
                return {
                    "service_name": "增强塔罗解读服务",
                    "status": "healthy",
                    "message": "增强塔罗解读服务运行正常",
                    "is_healthy": True,
                    "details": {
                        "supported_spreads": list(self.spread_templates.keys()),
                        "test_result_keys": list(result.keys()),
                        "interpretation_exists": bool(result.get("interpretation")),
                        "spread_aware": True
                    }
                }
            else:
                logger.warning("增强塔罗解读服务返回空结果")
                return {
                    "service_name": "增强塔罗解读服务",
                    "status": "unhealthy",
                    "message": "解读服务返回空结果",
                    "is_healthy": False,
                    "details": {"result": result}
                }

        except Exception as e:
            logger.error(f"增强塔罗解读服务健康检查失败: {type(e).__name__}: {str(e)}")
            return {
                "service_name": "增强塔罗解读服务",
                "status": "error",
                "message": f"健康检查异常: {str(e)}",
                "is_healthy": False,
                "details": {"error_type": type(e).__name__}
            }

# 全局增强塔罗解读服务实例
enhanced_tarot_interpretation_service = EnhancedTarotInterpretationService()
