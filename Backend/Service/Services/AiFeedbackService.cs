using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Microsoft.Extensions.Configuration;

namespace Service.Services;

public class AiFeedbackService(IConfiguration config) : IAiFeedbackService
{
    private readonly ILog _logger = LogManager.GetLogger(typeof(AiFeedbackService));

    public async Task<string> GenerateFeedbackAsync(string predictedLevel, PythonPredictResponseDTO ml)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.Warn("No Anthropic API key configured -- using rule-based feedback.");
            return BuildRuleBasedFeedback(predictedLevel, ml);
        }

        try
        {
            var client = new AnthropicClient(apiKey);
            var prompt = BuildClaudePrompt(predictedLevel, ml);

            var response = await client.Messages.GetClaudeMessageAsync(new MessageParameters
            {
                Model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001",
                MaxTokens = 300,
                Messages =
                [
                    new Message(RoleType.User, prompt)
                ],
            });

            var text = response.Content
                .OfType<TextContent>()
                .FirstOrDefault()?.Text;

            return text ?? BuildRuleBasedFeedback(predictedLevel, ml);
        }
        catch (Exception ex)
        {
            _logger.Error("Claude API failed, using fallback.", ex);
            return BuildRuleBasedFeedback(predictedLevel, ml);
        }
    }

    private static string BuildClaudePrompt(string level, PythonPredictResponseDTO ml)
    {
        var f = ml.Features;
        var levelGuidance = level switch
        {
            "A1" => "The student is an absolute beginner. Use very simple English, short sentences, no jargon. Praise any correct German they produced. Suggest ONE tiny improvement (e.g. a verb ending, an article). Be extra warm and encouraging.",
            "A2" => "The student is elementary. Use simple English with basic grammar terms only (verb, noun, past tense). Mention one strength and one concrete tip. Keep it approachable — they are still building confidence.",
            "B1" => "The student is intermediate. You can reference grammar concepts briefly (word order, cases, tenses). Point out one strength and one area to work on. You may suggest a slightly more advanced word choice.",
            "B2" => "The student is upper-intermediate. Use normal English with grammar terminology. Comment on style and sophistication, not just correctness. Suggest richer vocabulary or more varied sentence structures.",
            "C1" => "The student is advanced, near-native. Be precise and specific. Comment on stylistic nuance, register, idiomatic accuracy, and subtle word choice. Challenge them to polish, not just correct.",
            _ => "Adapt your feedback depth to the quality of the text."
        };

        return $"""
            You are a friendly German language tutor. A learner submitted text assessed at level {level}.
            Write 2-3 sentences of specific, warm, encouraging feedback in English.
            Focus on one strength and one concrete improvement. Do not mention CEFR or numbers directly.

            {levelGuidance}

            Linguistic profile:
            - Vocabulary richness (MATTR): {f.GetValueOrDefault("mattr"):F2}
            - Word rarity (Zipf): {f.GetValueOrDefault("median_zipf"):F1}
            - Grammar error rate per 100 tokens: {f.GetValueOrDefault("grammar_error_rate"):F1}
            - Sentence complexity (dep. depth): {f.GetValueOrDefault("avg_dep_depth"):F1}
            - Text coherence: {f.GetValueOrDefault("coherence_mean"):F2}
            - Errors detected: {ml.Errors.Count}
            """;
    }

    private static string BuildRuleBasedFeedback(string level, PythonPredictResponseDTO ml)
    {
        var f = ml.Features;
        var mattr = f.GetValueOrDefault("mattr");
        var gerr = f.GetValueOrDefault("grammar_error_rate");
        var coh = f.GetValueOrDefault("coherence_mean");

        var strength = mattr > 0.7f ? "your vocabulary is varied and rich"
                     : coh > 0.5f ? "your sentences flow well together"
                     : "you are making consistent effort";

        var improvement = gerr > 3.0f ? "focus on reducing grammar errors — review verb conjugations and article usage"
                        : mattr < 0.5f ? "try to use a wider range of vocabulary to avoid repetition"
                        : "work on connecting ideas with transition words for better flow";

        return $"Good work — {strength}. To improve further, {improvement}.";
    }
}