namespace Saleling.Util
{
    public static class PluralUtil
    {
        public static string Count(long count, string singular, string plural)
        {
            return $"{count:N0} {(count == 1 ? singular : plural)}";
        }
    }
}
