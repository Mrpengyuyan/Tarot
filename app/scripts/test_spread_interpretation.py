# -*- coding: utf-8 -*-
"""
测试不同牌阵的AI解读效果
验证Coze大模型对不同牌阵的处理能力
"""
import asyncio
import logging
import sys
import os

# 添加项目根目录到Python路径
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(__file__))))

from app.services.enhanced_tarot_interpretation import enhanced_tarot_interpretation_service

# 配置日志
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

class SpreadTester:
    """牌阵解读测试器"""

    def __init__(self):
        self.test_cases = [
            {
                "name": "单张牌 - 爱情问题",
                "question": "我的爱情状况如何？",
                "question_type": "love",
                "spread": {
                    "id": 1,
                    "name": "单张牌",
                    "description": "单张牌快速解读"
                },
                "cards": [
                    {
                        "name": "恋人",
                        "position": "当前状况",
                        "is_reversed": False,
                        "position_meaning": "爱情现状的核心反映",
                        "card_meaning": "和谐的关系，深度的情感连接"
                    }
                ]
            },
            {
                "name": "三张牌 - 事业发展",
                "question": "我的职业发展前景如何？",
                "question_type": "career",
                "spread": {
                    "id": 2,
                    "name": "三张牌",
                    "description": "过去-现在-未来时间线分析"
                },
                "cards": [
                    {
                        "name": "愚者",
                        "position": "过去",
                        "is_reversed": False,
                        "position_meaning": "过去的起点和新的开始",
                        "card_meaning": "新的开始，冒险精神，天真无邪"
                    },
                    {
                        "name": "魔术师",
                        "position": "现在",
                        "is_reversed": False,
                        "position_meaning": "当前的能力和资源",
                        "card_meaning": "意志力，创造力，技能，集中注意力"
                    },
                    {
                        "name": "世界",
                        "position": "未来",
                        "is_reversed": False,
                        "position_meaning": "未来的完成和成就",
                        "card_meaning": "完成，成就，圆满，统一，成功"
                    }
                ]
            },
            {
                "name": "凯尔特十字 - 综合问题",
                "question": "我的人生整体运势如何？",
                "question_type": "general",
                "spread": {
                    "id": 3,
                    "name": "凯尔特十字",
                    "description": "全面的深度分析，包含10个位置"
                },
                "cards": [
                    {
                        "name": "命运之轮",
                        "position": "中心牌：当前状况的核心",
                        "is_reversed": False,
                        "position_meaning": "当前的核心能量和状态",
                        "card_meaning": "命运，机遇，变化，循环，好运"
                    },
                    {
                        "name": "塔",
                        "position": "挑战牌：直接的障碍和挑战",
                        "is_reversed": True,
                        "position_meaning": "当前的挑战和障碍",
                        "card_meaning": "抗拒改变，内在动荡，逃避现实，缓慢变化"
                    },
                    {
                        "name": "星星",
                        "position": "远景牌：远期目标和理想状态",
                        "is_reversed": False,
                        "position_meaning": "远期的希望和目标",
                        "card_meaning": "希望，灵感，治愈，指引，精神启发"
                    },
                    {
                        "name": "太阳",
                        "position": "基础牌：过去的根源和基础",
                        "is_reversed": False,
                        "position_meaning": "过去的基础和根源",
                        "card_meaning": "成功，喜悦，活力，乐观，成就"
                    },
                    {
                        "name": "月亮",
                        "position": "过去牌：最近的影响因素",
                        "is_reversed": False,
                        "position_meaning": "最近的影响因素",
                        "card_meaning": "幻觉，恐惧，潜意识，直觉，隐藏的真相"
                    },
                    {
                        "name": "倒吊人",
                        "position": "未来牌：即将到来的发展",
                        "is_reversed": False,
                        "position_meaning": "即将到来的发展和变化",
                        "card_meaning": "牺牲，等待，换位思考，暂停，新视角"
                    },
                    {
                        "name": "审判",
                        "position": "自我牌：自身态度和内在状态",
                        "is_reversed": False,
                        "position_meaning": "自身的态度和内在状态",
                        "card_meaning": "重生，觉醒，宽恕，内在召唤，最终判断"
                    },
                    {
                        "name": "恶魔",
                        "position": "外界牌：外部环境和他人影响",
                        "is_reversed": True,
                        "position_meaning": "外部环境和他人影响",
                        "card_meaning": "解脱，觉醒，摆脱束缚，战胜诱惑"
                    },
                    {
                        "name": "力量",
                        "position": "希望恐惧牌：内心的希望和恐惧",
                        "is_reversed": False,
                        "position_meaning": "内心的希望和恐惧",
                        "card_meaning": "内在力量，勇气，坚持，自制力，温柔的力量"
                    },
                    {
                        "name": "节制",
                        "position": "结果牌：最终可能的结果",
                        "is_reversed": False,
                        "position_meaning": "最终可能的结果",
                        "card_meaning": "平衡，节制，和谐，耐心，中庸之道"
                    }
                ]
            },
            {
                "name": "财务专用牌阵",
                "question": "我的财务状况会如何发展？",
                "question_type": "finance",
                "spread": {
                    "id": 4,
                    "name": "五张牌",
                    "description": "财务状况专用分析"
                },
                "cards": [
                    {
                        "name": "星币国王",
                        "position": "当前财务状况",
                        "is_reversed": False,
                        "position_meaning": "当前的财务基础",
                        "card_meaning": "财务成功，企业领导，慷慨，可靠"
                    },
                    {
                        "name": "权杖八",
                        "position": "收入机会",
                        "is_reversed": True,
                        "position_meaning": "潜在的收入来源",
                        "card_meaning": "延迟，缺乏进展，内急，等待"
                    },
                    {
                        "name": "圣杯四",
                        "position": "支出模式",
                        "is_reversed": True,
                        "position_meaning": "当前的支出习惯",
                        "card_meaning": "重新参与，抓住机会，觉醒，动机恢复"
                    },
                    {
                        "name": "宝剑二",
                        "position": "财务挑战",
                        "is_reversed": False,
                        "position_meaning": "面临的财务挑战",
                        "card_meaning": "艰难决定，僵局，平衡，避免冲突"
                    },
                    {
                        "name": "星星",
                        "position": "财务前景",
                        "is_reversed": False,
                        "position_meaning": "财务的未来前景",
                        "card_meaning": "希望，灵感，治愈，指引，精神启发"
                    }
                ]
            }
        ]

    async def run_test(self, test_case: dict) -> dict:
        """运行单个测试案例"""
        print(f"\n🔮 开始测试: {test_case['name']}")
        print(f"问题: {test_case['question']}")
        print(f"牌阵: {test_case['spread']['name']} ({len(test_case['cards'])}张牌)")

        try:
            result = await enhanced_tarot_interpretation_service.get_spread_aware_interpretation(
                question=test_case['question'],
                question_type=test_case['question_type'],
                spread=test_case['spread'],
                cards_drawn=test_case['cards'],
                user_context="这是AI解读功能的测试案例",
                user_id="test_user"
            )

            # 验证结果结构
            validation = self.validate_result(result, test_case)

            print(f"✅ 测试完成: {test_case['name']}")
            print(f"📊 解读ID: {result.get('reading_id', 'N/A')}")
            print(f"🎯 牌阵类型: {result.get('spread_type', 'N/A')}")
            print(f"📝 整体解读长度: {len(result.get('interpretation', {}).get('overview', ''))} 字符")
            print(f"🔍 核心洞察数量: {len(result.get('interpretation', {}).get('core_insights', []))}")
            print(f"💡 行动建议类别: {list(result.get('interpretation', {}).get('action_recommendations', {}).keys())}")
            print(f"✅ 验证结果: {validation['is_valid']}")
            if not validation['is_valid']:
                print(f"❌ 验证问题: {validation['issues']}")

            return {
                'test_name': test_case['name'],
                'success': True,
                'result': result,
                'validation': validation
            }

        except Exception as e:
            print(f"❌ 测试失败: {test_case['name']}")
            print(f"错误: {str(e)}")
            return {
                'test_name': test_case['name'],
                'success': False,
                'error': str(e),
                'validation': {'is_valid': False, 'issues': [str(e)]}
            }

    def validate_result(self, result: dict, test_case: dict) -> dict:
        """验证解读结果的结构和内容"""
        issues = []

        # 检查必需字段
        required_fields = ['reading_id', 'question', 'spread_type', 'interpretation']
        for field in required_fields:
            if field not in result:
                issues.append(f"缺少必需字段: {field}")

        # 检查解读内容结构
        interpretation = result.get('interpretation', {})
        required_interpretation_fields = ['overview', 'core_insights', 'action_recommendations']
        for field in required_interpretation_fields:
            if field not in interpretation:
                issues.append(f"缺少解读字段: {field}")

        # 检查内容质量
        if interpretation.get('overview') and len(interpretation['overview']) < 50:
            issues.append("整体解读内容过短")

        if interpretation.get('core_insights') and len(interpretation['core_insights']) < 2:
            issues.append("核心洞察数量不足")

        # 检查行动建议
        action_recommendations = interpretation.get('action_recommendations', {})
        if not action_recommendations or len(action_recommendations) < 2:
            issues.append("行动建议不完整")

        # 检查牌阵感知性
        if test_case['spread']['name'] == "凯尔特十字":
            # 凯尔特十字应该有深度分析
            if not interpretation.get('spread_analysis'):
                issues.append("凯尔特十字缺少牌阵分析")

        return {
            'is_valid': len(issues) == 0,
            'issues': issues,
            'field_completeness': len(required_fields) / len(required_fields),
            'interpretation_completeness': len(required_interpretation_fields) / len(required_interpretation_fields)
        }

    async def run_all_tests(self):
        """运行所有测试案例"""
        print("🚀 开始牌阵解读效果测试")
        print("=" * 60)

        results = []
        total_tests = len(self.test_cases)
        successful_tests = 0

        for test_case in self.test_cases:
            result = await self.run_test(test_case)
            results.append(result)
            if result['success']:
                successful_tests += 1

        # 生成测试报告
        self.generate_test_report(results, successful_tests, total_tests)

    def generate_test_report(self, results: list, successful_tests: int, total_tests: int):
        """生成测试报告"""
        print("\n" + "=" * 60)
        print("📊 测试报告")
        print("=" * 60)

        success_rate = (successful_tests / total_tests) * 100
        print(f"📈 总体成功率: {success_rate:.1f}% ({successful_tests}/{total_tests})")

        print("\n📋 详细结果:")
        for result in results:
            status = "✅ 成功" if result['success'] else "❌ 失败"
            print(f"  {result['test_name']}: {status}")

        if successful_tests > 0:
            print("\n🎯 成功测试案例分析:")
            successful_results = [r for r in results if r['success']]

            avg_completeness = sum(
                r['validation']['field_completeness'] + r['validation']['interpretation_completeness']
                for r in successful_results
            ) / (2 * len(successful_results))

            print(f"  📊 平均结构完整度: {avg_completeness * 100:.1f}%")

            # 分析常见的验证问题
            all_issues = []
            for r in successful_results:
                all_issues.extend(r['validation']['issues'])

            if all_issues:
                print("  ⚠️ 常见问题:")
                issue_counts = {}
                for issue in all_issues:
                    issue_counts[issue] = issue_counts.get(issue, 0) + 1

                for issue, count in sorted(issue_counts.items(), key=lambda x: x[1], reverse=True):
                    print(f"    {issue}: {count}次")

        print(f"\n🏁 测试完成: {successful_tests}/{total_tests} 个测试通过")

        if success_rate >= 80:
            print("🎉 测试结果优秀！AI解读系统工作良好。")
        elif success_rate >= 60:
            print("👍 测试结果良好，但还有一些改进空间。")
        else:
            print("⚠️ 测试结果需要改进，请检查AI服务配置和提示词。")

async def main():
    """主函数"""
    print("🔮 塔罗牌阵AI解读效果测试工具")
    print("测试Coze大模型对不同牌阵的解读能力...")

    tester = SpreadTester()
    await tester.run_all_tests()

if __name__ == "__main__":
    asyncio.run(main())