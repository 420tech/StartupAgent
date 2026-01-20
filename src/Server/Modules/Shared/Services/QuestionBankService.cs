using StartupAgent.Shared.Contracts;
using StartupAgent.Shared.Models;

namespace StartupAgent.Modules.Shared.Services;

/// <summary>
/// Service providing curated question bank for diagnostic sessions.
/// Questions are pre-written in Tim's voice and organized by dimension.
/// </summary>
public interface IQuestionBankService
{
    /// <summary>
    /// Get the opening mindset detection question.
    /// </summary>
    QuestionDto GetMindsetQuestion();

    /// <summary>
    /// Get next question based on previous answer and mindset.
    /// Implements adaptive questioning logic.
    /// </summary>
    QuestionDto? GetNextQuestion(string currentQuestionId, string answer, MindsetType? detectedMindset, int totalQuestionsAsked);

    /// <summary>
    /// Get all available questions for a dimension.
    /// </summary>
    IEnumerable<QuestionDto> GetQuestionsByDimension(string dimension);
}

/// <summary>
/// Implementation of question bank service with curated questions.
/// </summary>
public class QuestionBankService : IQuestionBankService
{
    private readonly Dictionary<string, QuestionDto> _questionBank;
    private readonly List<string> _questionOrder; // Order for adaptive selection
    private int _currentQuestionIndex = 0;

    public QuestionBankService()
    {
        _questionBank = InitializeQuestionBank();
        _questionOrder = _questionBank.Keys.ToList();
    }

    public QuestionDto GetMindsetQuestion()
    {
        return new QuestionDto
        {
            Id = "mindset_opener",
            Text = "How are you feeling about your startup right now? I want to understand where you are mentally.",
            Dimension = "mindset",
            QuestionType = "multiple-choice",
            Options = new List<string>
            {
                "Overwhelmed - I have too many priorities and don't know where to start",
                "Stuck - I know something's wrong but can't figure out what to fix",
                "Confident-but-unsure - Feeling good about progress, want to validate I'm thinking about the right things",
                "Pre-fundraise - Getting ready to raise capital, want to check readiness"
            },
            QuestionNumber = 1,
            TotalQuestions = 15 // MVP target: 15-20 questions
        };
    }

    public QuestionDto? GetNextQuestion(string currentQuestionId, string answer, MindsetType? detectedMindset, int totalQuestionsAsked)
    {
        // MVP implementation: Simple sequential progression
        // Future: Implement adaptive branching based on answer + mindset
        if (totalQuestionsAsked >= 15) // Stop at 15 questions for MVP
        {
            return null;
        }

        _currentQuestionIndex++;
        if (_currentQuestionIndex >= _questionBank.Count)
        {
            return null;
        }

        var nextQuestion = _questionBank[_questionOrder[_currentQuestionIndex]];
        nextQuestion.QuestionNumber = totalQuestionsAsked + 2;
        nextQuestion.TotalQuestions = 15;

        return nextQuestion;
    }

    public IEnumerable<QuestionDto> GetQuestionsByDimension(string dimension)
    {
        return _questionBank.Values.Where(q => q.Dimension == dimension);
    }

    private Dictionary<string, QuestionDto> InitializeQuestionBank()
    {
        return new Dictionary<string, QuestionDto>
        {
            // Problem Validation Dimension
            {
                "pv_1", new QuestionDto
                {
                    Id = "pv_1",
                    Text = "Tell me about the problem you're solving. What pain point are founders facing that you're addressing?",
                    Dimension = "problem_validation",
                    QuestionType = "text"
                }
            },
            {
                "pv_2", new QuestionDto
                {
                    Id = "pv_2",
                    Text = "How do you know this problem is real and painful? Have you talked to users about it?",
                    Dimension = "problem_validation",
                    QuestionType = "text"
                }
            },

            // User Research Dimension
            {
                "ur_1", new QuestionDto
                {
                    Id = "ur_1",
                    Text = "How many conversations have you had with potential users to validate the problem?",
                    Dimension = "user_research",
                    QuestionType = "multiple-choice",
                    Options = new List<string> { "None yet", "1-5", "6-15", "16-30", "30+" }
                }
            },
            {
                "ur_2", new QuestionDto
                {
                    Id = "ur_2",
                    Text = "Can you describe your ideal customer/user in detail? What's their role, company size, budget?",
                    Dimension = "user_research",
                    QuestionType = "text"
                }
            },

            // MVP Quality Dimension
            {
                "mvp_1", new QuestionDto
                {
                    Id = "mvp_1",
                    Text = "What stage is your product in? Do you have a working prototype, MVP, or early production?",
                    Dimension = "mvp_quality",
                    QuestionType = "multiple-choice",
                    Options = new List<string>
                    {
                        "Idea/mockup only",
                        "Early prototype",
                        "MVP with basic features",
                        "Production with core features",
                        "Scaled product with full feature set"
                    }
                }
            },
            {
                "mvp_2", new QuestionDto
                {
                    Id = "mvp_2",
                    Text = "If you have users, what's their initial reaction? Are they excited, lukewarm, or skeptical?",
                    Dimension = "mvp_quality",
                    QuestionType = "text"
                }
            },

            // Traction Dimension
            {
                "tr_1", new QuestionDto
                {
                    Id = "tr_1",
                    Text = "How many active users do you have, and what's your month-over-month growth rate?",
                    Dimension = "traction",
                    QuestionType = "text"
                }
            },
            {
                "tr_2", new QuestionDto
                {
                    Id = "tr_2",
                    Text = "What's your user retention like? What percentage of users come back after 1 month?",
                    Dimension = "traction",
                    QuestionType = "text"
                }
            },

            // Go-to-Market Dimension
            {
                "gtm_1", new QuestionDto
                {
                    Id = "gtm_1",
                    Text = "How are you acquiring users right now? What channels are you using?",
                    Dimension = "go_to_market",
                    QuestionType = "text"
                }
            },
            {
                "gtm_2", new QuestionDto
                {
                    Id = "gtm_2",
                    Text = "What does it cost you to acquire a customer, and how much lifetime value do they generate?",
                    Dimension = "go_to_market",
                    QuestionType = "text"
                }
            },

            // Revenue Model Dimension
            {
                "rev_1", new QuestionDto
                {
                    Id = "rev_1",
                    Text = "What's your pricing strategy? How do you plan to make money?",
                    Dimension = "revenue_model",
                    QuestionType = "text"
                }
            },
            {
                "rev_2", new QuestionDto
                {
                    Id = "rev_2",
                    Text = "Have you validated that users will pay for this? Any paying customers yet?",
                    Dimension = "revenue_model",
                    QuestionType = "yes-no"
                }
            },

            // Operations & Legal Dimension
            {
                "ops_1", new QuestionDto
                {
                    Id = "ops_1",
                    Text = "Do you have a cap table, operating agreement, or formal governance structure?",
                    Dimension = "operations_legal",
                    QuestionType = "yes-no"
                }
            },
            {
                "ops_2", new QuestionDto
                {
                    Id = "ops_2",
                    Text = "Are there any legal, compliance, or regulatory requirements for your business?",
                    Dimension = "operations_legal",
                    QuestionType = "text"
                }
            },

            // Team Dimension
            {
                "team_1", new QuestionDto
                {
                    Id = "team_1",
                    Text = "Tell me about your founding team. Who are the key people and what experience do they bring?",
                    Dimension = "team",
                    QuestionType = "text"
                }
            },
            {
                "team_2", new QuestionDto
                {
                    Id = "team_2",
                    Text = "What key skills or expertise are you missing on the team right now?",
                    Dimension = "team",
                    QuestionType = "text"
                }
            },

            // Runway Dimension
            {
                "runway_1", new QuestionDto
                {
                    Id = "runway_1",
                    Text = "How many months of runway do you have (cash to sustain operations)?",
                    Dimension = "runway",
                    QuestionType = "text"
                }
            }
        };
    }
}
