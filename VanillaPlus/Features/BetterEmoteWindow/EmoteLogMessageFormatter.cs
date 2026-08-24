using System;
using System.Text;
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
        var parameters = new SeStringParameter[67]; // Highest used parameter is gnum67.

        parameters[0] = string.Empty; // Viewer name; empty forces third-person grammar.
        parameters[1] = playerName; // Source player name.
        parameters[2] = targetName; // Target name.
        parameters[4] = (uint)playerSex; // Source player sex.
        parameters[5] = (uint)targetSex; // Target sex.
        parameters[65] = StartsWithVowel(playerName); // Source name starts with a vowel.
        parameters[66] = StartsWithVowel(targetName); // Target name starts with a vowel.

        // lnum7/8 remain zero so noun macros use the provided names instead of ObjStr rows.

        return parameters;
    }

    private static uint StartsWithVowel(string value) {
        if (string.IsNullOrEmpty(value)) return 0;

        var firstCharacter = value[0];
        if (firstCharacter is 'Æ' or 'æ' or 'Œ' or 'œ') return 1;

        var normalized = firstCharacter.ToString().Normalize(NormalizationForm.FormD);
        var baseCharacter = char.ToUpperInvariant(normalized[0]);
        if (baseCharacter is 'A' or 'E' or 'I' or 'O' or 'U' or 'Y') return 1;

        return 0;
    }

    private static ReadOnlySeString RewriteGlobalParameters(ReadOnlySeString source) {
        // Dalamud's evaluator reads gstr/gnum values from shared game state and cannot override them per call.
        // Rewriting them to the same-numbered local parameters lets the preview supply isolated values instead.
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
        // Only global parameter references change. Every containing expression is recursively rebuilt so nested
        // conditionals, noun macros, strings, and comparisons retain the original language-specific grammar.
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
