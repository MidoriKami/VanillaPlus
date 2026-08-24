using System;
using Dalamud.Game.Text.Evaluator;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text;
using Lumina.Text.Expressions;
using Lumina.Text.ReadOnly;

namespace VanillaPlus.Features.BetterEmoteWindow;

public static class EmoteLogMessageFormatter {
    public static EmoteLogMessagePreview Format(
        Emote emote,
        string playerName,
        byte playerSex,
        string targetName,
        byte targetSex
    ) {
        var parameters = CreateParameters(playerName, playerSex, targetName, targetSex);

        return new EmoteLogMessagePreview(
            Format(emote.LogMessageUntargeted, parameters),
            Format(emote.LogMessageTargeted, parameters));
    }

    private static ReadOnlySeString Format(RowRef<LogMessage> logMessage, SeStringParameter[] parameters) {
        if (logMessage.RowId is 0 || !logMessage.IsValid) return default;

        var template = RewriteGlobalParameters(logMessage.Value.Text);
        return ISeStringEvaluator.Get().Evaluate(template, parameters);
    }

    private static SeStringParameter[] CreateParameters(
        string playerName,
        byte playerSex,
        string targetName,
        byte targetSex
    ) {
        const int LocalParameterCount = 67;
        const int ViewerNameIndex = 0;
        const int SourceNameIndex = 1;
        const int TargetNameIndex = 2;
        const int SourceSexIndex = 4;
        const int TargetSexIndex = 5;
        const int SourceStartsWithVowelIndex = 65;
        const int TargetStartsWithVowelIndex = 66;

        var parameters = new SeStringParameter[LocalParameterCount];

        // Keep gstr1 distinct from gstr2/gstr3 so previews use character names instead of first-person pronouns.
        parameters[ViewerNameIndex] = string.Empty;
        parameters[SourceNameIndex] = playerName;
        parameters[TargetNameIndex] = targetName;
        parameters[SourceSexIndex] = (uint)playerSex;
        parameters[TargetSexIndex] = (uint)targetSex;
        parameters[SourceStartsWithVowelIndex] = StartsWithVowel(playerName);
        parameters[TargetStartsWithVowelIndex] = StartsWithVowel(targetName);

        return parameters;
    }

    private static uint StartsWithVowel(string value) {
        if (string.IsNullOrEmpty(value)) return 0;

        const string Vowels = "AEIOUYÀÂÄÉÈÊËÎÏÔÖÙÛÜŸŒÆ";
        if (Vowels.Contains(char.ToUpperInvariant(value[0]))) return 1;

        return 0;
    }

    private static ReadOnlySeString RewriteGlobalParameters(ReadOnlySeString source) {
        using var rentedStringBuilder = new RentedSeStringBuilder();
        RewriteSeString(rentedStringBuilder.Builder, source.AsSpan());
        return rentedStringBuilder.Builder.ToReadOnlySeString();
    }

    private static void RewriteSeString(SeStringBuilder builder, ReadOnlySeStringSpan source) {
        foreach (var payload in source) {
            if (payload.Type is ReadOnlySePayloadType.Text) {
                builder.Append(payload.Body);
                continue;
            }

            builder.BeginMacro(payload.MacroCode);
            foreach (var expression in payload) RewriteExpression(builder, expression);
            builder.EndMacro();
        }
    }

    private static void RewriteExpression(SeStringBuilder builder, ReadOnlySeExpressionSpan source) {
        if (source.TryGetParameterExpression(out var type, out var operand)) {
            var expressionType = (ExpressionType)type;
            if (expressionType is ExpressionType.GlobalNumber) expressionType = ExpressionType.LocalNumber;
            if (expressionType is ExpressionType.GlobalString) expressionType = ExpressionType.LocalString;

            builder.BeginUnaryExpression(expressionType);
            RewriteExpression(builder, operand);
            builder.EndExpression();
            return;
        }

        if (source.TryGetBinaryExpression(out type, out var left, out var right)) {
            builder.BeginBinaryExpression((ExpressionType)type);
            RewriteExpression(builder, left);
            RewriteExpression(builder, right);
            builder.EndExpression();
            return;
        }

        if (source.TryGetString(out var value)) {
            builder.BeginStringExpression();
            RewriteSeString(builder, value);
            builder.EndExpression();
            return;
        }

        if (source.TryGetPlaceholderExpression(out type)) {
            builder.AppendNullaryExpression((ExpressionType)type);
            return;
        }

        if (source.TryGetUInt(out var unsignedValue)) {
            builder.AppendUIntExpression(unsignedValue);
            return;
        }

        if (source.TryGetInt(out var signedValue)) {
            builder.AppendUIntExpression(unchecked((uint)signedValue));
            return;
        }

        throw new InvalidOperationException("Unsupported SeString expression: " + source.ToString());
    }
}
