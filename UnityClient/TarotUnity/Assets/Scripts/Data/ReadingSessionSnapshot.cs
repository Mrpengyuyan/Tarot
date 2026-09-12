using System;

namespace TarotUnity.Data
{
    // Phase 66: where a reading's interpretation text came from.
    public enum ReadingSource
    {
        Offline,
        Online,
    }

    // Phase 66: Ready is the first member so default(InterpretationState) and a
    // fresh snapshot both describe an offline reading that already has its text.
    public enum InterpretationState
    {
        Ready,
        Pending,
        Failed,
    }

    // Phase 66: why an online interpretation stopped (spec 7.3, interpretation rows).
    public enum InterpretationFailure
    {
        BackendFailed,
        TimedOut,
        ConnectionLost,
        AttemptsExhausted,
        SessionExpired,
        Unavailable,
    }

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

        // Phase 66: online interpretation state. An offline reading keeps the
        // defaults: no record, offline source, text already Ready.
        public int predictionId;
        public ReadingSource source = ReadingSource.Offline;
        public InterpretationState interpretationState = InterpretationState.Ready;
        public string modelUsed = string.Empty;
        public string failureMessage = string.Empty;
        public bool canRetry;
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

