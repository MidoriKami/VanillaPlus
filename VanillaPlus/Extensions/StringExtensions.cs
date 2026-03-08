namespace VanillaPlus.Extensions;

public static class StringExtensions {
    extension(string text) {
        public string IfEmpty(string ifEmpty)
            => string.IsNullOrEmpty(text) ? ifEmpty : text;
    }
}
