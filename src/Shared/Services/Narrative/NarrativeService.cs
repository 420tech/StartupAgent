using StartupAgent.Shared.Models.Scoring;

namespace StartupAgent.Shared.Services.Narrative;

/// <summary>
/// Service to generate personalized narrative guidance in Tim's voice
/// </summary>
public interface INarrativeService
{
    /// <summary>
    /// Generate narrative text based on results and mindset
    /// </summary>
    Task<string> GenerateNarrativeAsync(
        DiagnosticResults results,
        CancellationToken cancellationToken = default);
}

public class NarrativeService : INarrativeService
{
    public async Task<string> GenerateNarrativeAsync(
        DiagnosticResults results,
        CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask; // Placeholder for AI call

        try
        {
            // TODO: Call Azure OpenAI with mindset-specific prompt
            // For now, generate mindset-appropriate narrative based on scores
            
            var narrative = GenerateMindsetNarrative(results);
            
            return narrative;
        }
        catch
        {
            // Fallback on any error
            return GetFallbackNarrative(results.MindsetBucket, results.OverallStatus);
        }
    }

    private static string GenerateMindsetNarrative(DiagnosticResults results)
    {
        var mindset = results.MindsetBucket.ToLower();
        var status = results.OverallStatus;
        var score = results.OverallScore;
        var topPriorities = results.TopPriorities;

        return mindset switch
        {
            "overwhelmed" => GenerateOverwhelmedNarrative(status, score, topPriorities),
            "confident-but-unsure" => GenerateConfidentNarrative(status, score, topPriorities),
            "stuck" => GenerateStuckNarrative(status, score, topPriorities),
            "pre-fundraise" => GeneratePreFundraiseNarrative(status, score, topPriorities),
            _ => GenerateDefaultNarrative(status, score, topPriorities)
        };
    }

    private static string GenerateOverwhelmedNarrative(string status, int score, List<string> priorities)
    {
        var intro = status switch
        {
            "Green" => "First, take a breath. You're doing better than you think—your score of {0}/100 shows you've built solid fundamentals. The overwhelm you're feeling is real, but it's not a reflection of where you actually are.",
            "Yellow" => "I know it feels like everything's on fire right now, but here's what I see: a {0}/100 score means you've got some real strengths in place. The overwhelm isn't because you're failing—it's because you're trying to fix everything at once.",
            _ => "Listen—scoring {0}/100 doesn't mean you're failing. It means you've identified the gaps before they became disasters. Most founders don't get that clarity until it's too late. The overwhelm you feel? That's normal when you can suddenly see everything that needs fixing."
        };

        var priority = priorities.Any() 
            ? $"\n\nHere's what I'd focus on if I were you: **{priorities[0]}**. Not everything—just that. Get one win, feel the momentum shift, then tackle the next thing. You don't need to fix it all this week."
            : "\n\nYou don't need to fix everything this week. Pick one area, get a win, build momentum.";

        var close = status == "Red"
            ? "\n\nI've worked with founders who started with lower scores than this and turned it around. The difference wasn't talent—it was focus. You've got the clarity now. Let's use it."
            : "\n\nYou're not as far behind as you feel. Trust the process, focus on what matters, and give yourself permission to ignore the rest for now.";

        return string.Format(intro, score) + priority + close;
    }

    private static string GenerateConfidentNarrative(string status, int score, List<string> priorities)
    {
        var intro = status switch
        {
            "Green" => "Your instincts are solid—{0}/100 confirms you're on the right track. But I'm seeing a few places where you could tighten up before they become blockers down the road.",
            "Yellow" => "You're right to feel confident about your foundation—{0}/100 shows real progress. But there are a couple of areas where your intuition might be off, and that's exactly why you're here.",
            _ => "I appreciate that you came here for validation, even with a {0}/100 score. That self-awareness is rare. Most founders in your position wouldn't have done this diagnostic—they'd have charged ahead and hit a wall at the worst possible time."
        };

        var priority = priorities.Any()
            ? $"\n\nThe gap I'd address first: **{priorities[0]}**. You're confident in your vision, and that's good—but this area needs attention before it undermines everything else you've built."
            : "\n\nFocus on closing the gaps you've identified before they become bigger issues.";

        var close = status == "Green"
            ? "\n\nYou're closer to ready than most founders at your stage. A few tactical adjustments and you'll be in excellent shape for what's next."
            : "\n\nYour confidence isn't misplaced—you've got real strengths. Now let's make sure nothing trips you up when it matters most.";

        return string.Format(intro, score) + priority + close;
    }

    private static string GenerateStuckNarrative(string status, int score, List<string> priorities)
    {
        var intro = status switch
        {
            "Green" => "Here's the thing: you're scoring {0}/100, which is objectively solid. The 'stuck' feeling isn't about your fundamentals—it's about not seeing the path forward. That's a different problem, and it's fixable.",
            "Yellow" => "A {0}/100 score tells me you've built momentum in some areas but hit a wall in others. The stuck feeling makes sense—you can see what's working, but you can't figure out how to break through on what's not.",
            _ => "Scoring {0}/100 and feeling stuck is actually a good sign. You're not stuck because you don't know what to do—you're stuck because you can see too many gaps and don't know which one is the real blocker. Let me show you."
        };

        var priority = priorities.Any()
            ? $"\n\nThe unlock you're looking for: **{priorities[0]}**. I know it feels like everything's connected and you need to fix it all, but this is the leverage point. Fix this, and the rest starts to move."
            : "\n\nThe breakthrough is closer than you think. Focus on the highest-leverage area and watch the others start to shift.";

        var close = status == "Red"
            ? "\n\nBeing stuck doesn't mean you're failing—it means you've outgrown your current approach. This diagnostic is the map out. Follow it."
            : "\n\nYou're not as stuck as you feel. You've got traction in key areas. Now let's figure out the one thing that unlocks the rest.";

        return string.Format(intro, score) + priority + close;
    }

    private static string GeneratePreFundraiseNarrative(string status, int score, List<string> priorities)
    {
        var intro = status switch
        {
            "Green" => "You're at {0}/100, which puts you in solid shape for fundraising. Most investors won't dig this deep, but the ones worth taking money from will. Let's make sure you're ready for those conversations.",
            "Yellow" => "A {0}/100 score is workable for fundraising, but it's not optimal. You're going to get questions on the gaps, and you need crisp answers—not just 'we're working on it.' Here's what I'd tighten up before you start pitching.",
            _ => "Here's the truth: at {0}/100, you're not ready for serious fundraising. Investors won't always articulate why they pass, but these gaps will kill your momentum in diligence. The good news? You found out now, not after wasting 6 months pitching."
        };

        var priority = priorities.Any()
            ? $"\n\nBefore you take any meetings: **{priorities[0]}**. This will come up. If you can't speak to it with confidence and data, you're giving investors an easy out."
            : "\n\nBefore pitching, shore up the areas that will get scrutinized in diligence.";

        var close = status == "Green"
            ? "\n\nYou're in better shape than 80% of founders who start fundraising. A few tactical refinements and you'll stand out in the right ways."
            : status == "Yellow"
                ? "\n\nYou've got enough to start conversations, but I'd fix these gaps before you get into serious diligence. Nothing kills a round faster than surprises that should have been addressed."
                : "\n\nI know it's frustrating to hear 'not yet,' but better to know now than after you've burned your network on a failed round. Fix these areas, come back in 90 days, and you'll be in a completely different position.";

        return string.Format(intro, score) + priority + close;
    }

    private static string GenerateDefaultNarrative(string status, int score, List<string> priorities)
    {
        var intro = $"Your {score}/100 score gives us a clear picture of where you are and what needs attention next.";
        
        var priority = priorities.Any()
            ? $"\n\nI'd start with **{priorities[0]}**—addressing this will have the biggest impact on your overall readiness."
            : "\n\nFocus on your top priorities to move the needle.";

        var close = status == "Green"
            ? "\n\nYou've built a strong foundation. Keep refining and you'll be well-positioned for what's next."
            : "\n\nYou've got clear direction now. Execute on these priorities and you'll see real momentum.";

        return intro + priority + close;
    }

    private static string GetFallbackNarrative(string mindset, string status)
    {
        return status switch
        {
            "Green" => "You're in solid shape overall. Your assessment shows strong fundamentals across most areas. Focus on maintaining this momentum and addressing your top priorities to keep moving forward.",
            "Yellow" => "You've got a good foundation in place, with clear areas for improvement. The priorities identified in your assessment will help you focus your energy where it matters most. Addressing these areas will strengthen your overall position.",
            _ => "Your assessment reveals important areas that need attention. The good news: you've identified the gaps before they became critical issues. Focus on the top priorities, tackle them systematically, and you'll see meaningful progress."
        };
    }
}
