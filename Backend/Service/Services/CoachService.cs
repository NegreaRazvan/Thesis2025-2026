using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using Controller.Interfaces;
using Domain.DTOs;
using log4net;
using Microsoft.Extensions.Configuration;
using Service.Interfaces;

namespace Service.Services;

public class CoachService(IConfiguration config, ICoachSessionRepository sessionRepository) : ICoachService
{
    private readonly ICoachSessionRepository _sessionRepository = sessionRepository;
    private readonly ILog _logger = LogManager.GetLogger(typeof(CoachService));

    private static string BuildSystemInstructions(string cefr) => $"""
        [Coaching persona — follow these instructions throughout the conversation]
        You are "Lena", an expert and encouraging German language tutor.
        The student has submitted a German text that has been automatically analysed.

        Act as a Socratic writing coach:
        - Ask targeted questions to help the student discover and fix their own errors.
        - Give concrete rewrites only when the student is genuinely stuck.
        - Celebrate improvements, no matter how small.
        - Keep each reply to 2-4 sentences maximum so the student stays engaged.
        - Respond in English by default; switch to German (with English explanations)
          only if the student writes in German.
        - Never repeat feedback already given in this session.
        - Never mention CEFR labels, scores or numbers — focus on the language itself.

        === ADAPT YOUR TEACHING TO THE STUDENT'S LEVEL ({cefr}) ===

        {GetLevelGuidance(cefr)}

        [End instructions]
        """;

    private static string GetLevelGuidance(string cefr) => cefr switch
    {
        "A1" => """
            BEGINNER — The student knows very basic German.
            - Use simple, short English sentences. No linguistic jargon at all.
            - Focus ONLY on: basic word order (SVO), present tense verb forms, common noun genders (der/die/das), basic articles.
            - Ignore advanced issues like subordinate clauses or passive voice — they are beyond this level.
            - Give one small correction at a time. Provide the correct form directly after one guiding question.
            - Use encouraging, warm language — this student is easily overwhelmed.
            - When giving German examples, always include the English translation in parentheses.
            """,
        "A2" => """
            ELEMENTARY — The student can handle everyday phrases.
            - Use clear, simple English. Avoid grammar terminology beyond "noun", "verb", "adjective", "past tense".
            - Focus on: verb conjugation accuracy, Perfekt vs Präteritum basics, accusative/dative cases, separable verbs, basic connectors (und, aber, weil).
            - Don't expect correct subordinate clause word order yet — gently point it out but don't insist.
            - Limit to 1-2 corrections per reply. Give a direct example after the guiding question.
            - When giving German examples, provide the English translation.
            """,
        "B1" => """
            INTERMEDIATE — The student can discuss everyday topics.
            - Use moderate English. You can use terms like "subordinate clause", "case", "tense", but briefly explain them.
            - Focus on: subordinate clause word order (verb-final), Konjunktiv II basics, adjective declension, Wechselpräpositionen, richer connectors (obwohl, nachdem, damit).
            - You can address 2-3 issues per reply. Ask guiding questions first, give the rule if they struggle.
            - Suggest vocabulary upgrades — replacing basic words with more precise alternatives.
            - Start encouraging the student to self-correct by asking "What rule applies here?".
            """,
        "B2" => """
            UPPER-INTERMEDIATE — The student can handle complex topics.
            - Use natural English with standard grammatical terminology (no need to define "passive voice" or "relative clause").
            - Focus on: Konjunktiv I/II nuance, passive alternatives, extended attributes, Nominalstil, stylistic variety, register awareness (formal vs informal).
            - Point out subtle errors: wrong preposition choice, unnatural collocations, redundancies.
            - Encourage the student to restructure sentences for better flow and sophistication.
            - Suggest idiomatic expressions or collocations that a native speaker would prefer.
            - Challenge: "Can you rephrase this using a relative clause / passive / nominalization?"
            """,
        "C1" => """
            ADVANCED — The student is near-native.
            - Treat them as a fellow language enthusiast. Use precise linguistic terminology freely.
            - Focus on: stylistic polish, register consistency, Konjunktiv subtleties, idiomatic precision, academic/literary style, Partizipialattribute, comma rules.
            - Address nuance: "This is grammatically correct, but a native speaker would say..."
            - Discuss connotation differences between near-synonyms.
            - Challenge them with reformulation tasks: "Rewrite this paragraph in formal academic register" or "Express this more concisely".
            - Point out even minor comma or punctuation issues — at this level, precision matters.
            """,
        _ => """
            Adapt your explanation depth to what seems appropriate based on the quality of the student's text.
            """
    };

    public async Task<CoachChatResponseDTO> ChatAsync(CoachChatRequestDTO req)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.Warn("No Anthropic API key configured -- using rule-based coach reply.");
            return new CoachChatResponseDTO(BuildFallback(req));
        }

        try
        {
            var client = new AnthropicClient(apiKey);

            var messages = new List<Message>();
            var instructions = BuildSystemInstructions(req.PredictedCefr);

            var contextPrimer = $"""
                {instructions}

                The student submitted the following German text (detected level: {req.PredictedCefr}):

                ---
                {req.GermanText}
                ---

                Please begin the coaching session by identifying the single most impactful
                issue in the text and asking one guiding question about it.
                """;

            if (req.History.Count == 0)
            {
                messages.Add(new Message(RoleType.User, contextPrimer));
            }
            else
            {
                messages.Add(new Message(RoleType.User, contextPrimer));

                foreach (var m in req.History)
                {
                    var role = m.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase)
                        ? RoleType.Assistant
                        : RoleType.User;
                    messages.Add(new Message(role, m.Content));
                }

                messages.Add(new Message(RoleType.User, req.UserMessage));
            }

            var response = await client.Messages.GetClaudeMessageAsync(new MessageParameters
            {
                Model = config["Anthropic:Model"] ?? "claude-haiku-4-5-20251001",
                MaxTokens = 250,
                Messages = messages,
            });

            var text = response.Content.OfType<TextContent>().FirstOrDefault()?.Text
                       ?? BuildFallback(req);

            return new CoachChatResponseDTO(text);
        }
        catch (Exception ex)
        {
            _logger.Error("Coach API call failed.", ex);
            return new CoachChatResponseDTO(BuildFallback(req));
        }
    }

    public Task<CoachSessionSummaryDTO> SaveSessionAsync(string userId, SaveCoachSessionDTO dto) =>
        _sessionRepository.SaveAsync(userId, dto);

    public Task<List<CoachSessionSummaryDTO>> GetSessionsAsync(string userId) =>
        _sessionRepository.GetHistoryAsync(userId);

    public Task<CoachSessionDetailDTO?> GetSessionDetailAsync(string userId, Guid id) =>
        _sessionRepository.GetDetailAsync(userId, id);

    private static string BuildFallback(CoachChatRequestDTO req) =>
        req.History.Count == 0
            ? "Let's start! Look at your text - can you spot any verbs that might not be conjugated correctly for their subject?"
            : "Great effort! Think about the gender of the nouns you used - do the articles match?";
}