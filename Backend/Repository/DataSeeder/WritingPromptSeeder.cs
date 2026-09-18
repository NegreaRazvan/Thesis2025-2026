using log4net;
using Microsoft.EntityFrameworkCore;
using Repository.Context;
using Repository.EFEntities;

namespace Repository.DataSeeder;

public class WritingPromptSeeder(GermanAIContext context)
{
    private readonly GermanAIContext _context = context;
    private readonly ILog _logger = LogManager.GetLogger(typeof(WritingPromptSeeder));

    public async Task SeedAsync()
    {
        if (await _context.WritingPrompts.AnyAsync())
        {
            _logger.Info("Writing prompts already seeded — skipping.");
            return;
        }

        _logger.Info("Seeding writing prompts…");

        var prompts = new List<WritingPrompt>
        {
            new() { CefrLevel = "A1", PromptDe = "Beschreibe dich selbst. Wie heißt du? Woher kommst du?",                                PromptEn = "Describe yourself. What is your name? Where are you from?",                       TopicTag = "personal"     },
            new() { CefrLevel = "A1", PromptDe = "Beschreibe dein Zimmer. Was siehst du?",                                                 PromptEn = "Describe your room. What do you see?",                                           TopicTag = "daily_life"   },
            new() { CefrLevel = "A2", PromptDe = "Erzähle von deinem typischen Tag.",                                                      PromptEn = "Tell us about your typical day.",                                                TopicTag = "daily_life"   },
            new() { CefrLevel = "A2", PromptDe = "Schreibe eine kurze E-Mail an einen Freund über dein Wochenende.",                        PromptEn = "Write a short email to a friend about your weekend.",                            TopicTag = "communication"},
            new() { CefrLevel = "B1", PromptDe = "Beschreibe eine Person, die dich inspiriert, und erkläre warum.",                        PromptEn = "Describe a person who inspires you and explain why.",                            TopicTag = "personal"     },
            new() { CefrLevel = "B1", PromptDe = "Was sind die Vor- und Nachteile von sozialen Medien?",                                   PromptEn = "What are the advantages and disadvantages of social media?",                     TopicTag = "society"      },
            new() { CefrLevel = "B2", PromptDe = "Diskutiere, wie der Klimawandel unser tägliches Leben beeinflusst.",                     PromptEn = "Discuss how climate change affects our daily lives.",                            TopicTag = "environment"  },
            new() { CefrLevel = "B2", PromptDe = "Sollte das Studium an Universitäten kostenlos sein? Begründe deine Meinung.",            PromptEn = "Should university education be free? Justify your opinion.",                    TopicTag = "education"    },
            new() { CefrLevel = "C1", PromptDe = "Analysiere die gesellschaftlichen Auswirkungen der Digitalisierung auf den Arbeitsmarkt.", PromptEn = "Analyse the social effects of digitalisation on the labour market.",           TopicTag = "society"      },
            new() { CefrLevel = "C1", PromptDe = "Inwiefern spiegelt Sprache die Weltanschauung einer Gesellschaft wider?",                 PromptEn = "To what extent does language reflect a society's worldview?",                   TopicTag = "culture"      },
        };

        await _context.WritingPrompts.AddRangeAsync(prompts);
        await _context.SaveChangesAsync();
        _logger.InfoFormat("Seeded {0} writing prompts.", prompts.Count);
    }
}
