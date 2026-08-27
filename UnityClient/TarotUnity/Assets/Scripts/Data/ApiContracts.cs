using System;

namespace TarotUnity.Data
{
    [Serializable]
    public sealed class HealthCheckResponse
    {
        public string status;
        public string timestamp;
        public string service;
        public string version;
    }

    [Serializable]
    public sealed class TokenResponse
    {
        public string access_token;
        public string token_type;
    }

    [Serializable]
    public sealed class UserProfile
    {
        public int id;
        public string username;
        public string email;
        public bool is_active;
        public bool is_superuser;
    }

    [Serializable]
    public sealed class SpreadPositionData
    {
        public int position;
        public string name;
        public string meaning;
    }

    [Serializable]
    public class SpreadSummary
    {
        public int id;
        public string name;
        public string name_en;
        public string description;
        public int card_count;
        public int difficulty_level;
        public SpreadPositionData[] positions;
        public bool is_beginner_friendly;
        public int usage_count;
        public bool suitable_for_love;
        public bool suitable_for_career;
        public bool suitable_for_finance;
        public bool suitable_for_health;
        public bool suitable_for_general;
    }

    [Serializable]
    public sealed class SpreadDetail : SpreadSummary
    {
        public string layout_image_url;
        public bool is_active;
    }

    [Serializable]
    public sealed class PredictionCreateRequest
    {
        public string question;
        public string question_type;
        public int spread_type_id;
    }

    [Serializable]
    public class PredictionResponse
    {
        public int id;
        public int user_id;
        public int spread_type_id;
        public string question;
        public string question_type;
        public string status;
        public string created_at;
        public string completed_at;
        public bool is_favorite;
        public int user_rating;
        public string user_notes;
    }

    [Serializable]
    public sealed class TarotCardSimple
    {
        public int id;
        public string name_zh;
        public string name_en;
        public string arcana;
        public string suit;
        public int number;
        public string image_url;
    }

    [Serializable]
    public sealed class TarotCardMeaningData
    {
        public int id;
        public string name_zh;
        public string name_en;
        public bool is_reversed;
        public string meaning;
        public string[] keywords;
        public int position;
        public string position_name;
        public string position_meaning;
    }

    [Serializable]
    public sealed class CardDrawData
    {
        public int id;
        public int prediction_id;
        public int tarot_card_id;
        public int position;
        public bool is_reversed;
        public string drawn_at;
        public TarotCardSimple tarot_card;
        public TarotCardMeaningData card_meaning;
        public string position_name;
        public string position_meaning;
    }

    [Serializable]
    public sealed class DrawCardsResponse
    {
        public int prediction_id;
        public CardDrawData[] card_draws;
        public string status;
    }

    [Serializable]
    public sealed class InterpretationResponse
    {
        public int id;
        public int prediction_id;
        public string overall_interpretation;
        public string card_analysis;
        public string relationship_analysis;
        public string advice;
        public string warning;
        public string summary;
        public string key_themes;
        public string model_used;
        public string model_version;
        public float confidence_score;
        public string generated_at;
    }

    [Serializable]
    public sealed class PredictionDetailResponse : PredictionResponse
    {
        public SpreadSummary spread_type;
        public CardDrawData[] card_draws;
        public InterpretationResponse interpretation;
    }
}
