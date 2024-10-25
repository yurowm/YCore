namespace Yurowm.Utilities {
    public static class YRandomExtensionsForUnity {
       
        public static float Range(this YRandom random, FloatRange range, string key = null) {
            return random.Range(range.min, range.max, key);
        }

        public static int Range(this YRandom random, IntRange range, string key = null) {
            return random.Range(range.min, range.max, key);
        }
    }
}
