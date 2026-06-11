using System;

namespace TarotUnity.Data
{
    [Serializable]
    public sealed class ReadingSessionSnapshot
    {
        public int spreadId;
        public string spreadName;
        public int cardCount;
        public string question;
        public string questionType;
        public CardDrawData[] cardDraws;
        public string summary;
        public string overallInterpretation;
        public string cardAnalysis;
        public string advice;
        public string warning;
    }

    public static class ReadingSessionStore
    {
        public static ReadingSessionSnapshot Current { get; private set; }

        public static bool HasCurrent => Current != null;

        public static void Save(ReadingSessionSnapshot session)
        {
            Current = session;
        }

        public static void Clear()
        {
            Current = null;
        }
    }
}

