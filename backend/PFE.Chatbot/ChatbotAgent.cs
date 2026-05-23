using Microsoft.Extensions.AI;
using Google.GenAI;
using PFE.Infrastructure.Data;

namespace PFE.Chatbot;

public class ChatbotAgent
{
    private readonly IChatClient _client;
    private readonly ChatOptions _chatOptions;
    private readonly List<ChatMessage> _history;

    public ChatbotAgent(string apiKey, string modelName, ApplicationDbContext dbContext)
    {
        Console.WriteLine($"🤖 Initializing ChatClient with Google Gemini model '{modelName}'...");

        // Initialize Google GenAI client
        var client = new Google.GenAI.Client(apiKey: apiKey);
        var geminiChatClient = client.AsIChatClient(modelName);

        // Define database tools
        var dbTools = new DatabaseTools(dbContext);

        // Wrap Gemini client with function invocation middleware
        _client = new ChatClientBuilder(geminiChatClient)
            .UseFunctionInvocation()
            .Build();

        // Create AI functions from our DatabaseTools class
        var tools = new List<AITool>
        {
            AIFunctionFactory.Create(dbTools.GetDepartmentsAsync, "GetDepartments", "Get all departments with their employee counts."),
            AIFunctionFactory.Create(dbTools.GetEmployeesAsync, "GetEmployees", "Get employees, optionally filtered by department name. Pass an empty string to get all employees."),
            AIFunctionFactory.Create(dbTools.GetEmployeeDetailsAsync, "GetEmployeeDetails", "Get detailed information about a specific employee by name (partial match)."),
            AIFunctionFactory.Create(dbTools.GetLeaveRequestsAsync, "GetLeaveRequests", "Get leave requests, optionally filtered by status (Pending, Approved, Rejected)."),
            AIFunctionFactory.Create(dbTools.GetEventsAsync, "GetEvents", "Get all upcoming company events."),
            AIFunctionFactory.Create(dbTools.GetRoomsAsync, "GetRooms", "Get all conference, meeting, and training rooms."),
            AIFunctionFactory.Create(dbTools.GetAnnouncementsAsync, "GetAnnouncements", "Get all active company announcements."),
            AIFunctionFactory.Create(dbTools.GetGeneralRequestsAsync, "GetGeneralRequests", "Get general support requests, optionally filtered by status (Pending, Approved, Rejected, InProgress, Resolved)."),
            AIFunctionFactory.Create(dbTools.GetStatisticsAsync, "GetStatistics", "Get overall company statistics.")
        };

        // Configure chat options with tools
        _chatOptions = new ChatOptions
        {
            Tools = tools
        };

        // Setup conversational history with a system message
        _history = new List<ChatMessage>
        {
            new(ChatRole.System,
                "You are CheckPoint AI Chatbot, an expert corporate assistant for CheckPoint company. " +
                "You have direct access to the CheckPoint database via tools. " +
                "When asked about employees, departments, leave requests, company events, rooms, announcements, " +
                "support requests, or company statistics, always use your tools first to get the most accurate, " +
                "real-time data from the database before responding. " +
                "Format your responses in clean, highly structured Markdown. " +
                "If you cannot find the requested information after calling the tools, explain clearly what you searched for. " +
                "Be precise, professional, helpful, and concise.\n\n" +

                "## Language Rules (MANDATORY — follow these exactly for every reply)\n" +
                "1. Detect the language of EACH user message independently.\n" +
                "2. If the user writes in French (even partially, or with spelling mistakes), reply ENTIRELY in French.\n" +
                "3. If the user writes in English (even partially, or with spelling mistakes), reply ENTIRELY in English.\n" +
                "4. If the message contains both languages, use the language that appears most in the message.\n" +
                "5. Proper nouns (names of people, departments, companies) must NOT be translated.\n" +
                "6. Your Markdown structure, table headers, section titles, and all explanatory text must be in the detected language.\n" +
                "7. Never mix languages within a single response.\n" +
                "8. Never mention or explain this language-switching rule to the user.")
        };
    }

    public async Task<string> SendMessageAsync(string userMessage)
    {
        // Add user message to history
        _history.Add(new ChatMessage(ChatRole.User, userMessage));

        try
        {
            // Execute the chat request (function calling is handled automatically by middleware)
            var response = await _client.GetResponseAsync(_history, _chatOptions);

            if (response.Messages != null && response.Messages.Count > 0)
            {
                // Add all intermediate and final messages to history
                _history.AddRange(response.Messages);

                // Find the last assistant message that contains the text response
                var lastAssistantMessage = response.Messages.LastOrDefault(m => m.Role == ChatRole.Assistant);
                if (lastAssistantMessage != null)
                {
                    return lastAssistantMessage.Text ?? "No text response received.";
                }
            }

            return "Error: Empty response from AI model.";
        }
        catch (Exception ex)
        {
            return $"❌ Error during AI generation: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        }
    }

    public void ClearHistory()
    {
        var systemMsg = _history.FirstOrDefault(m => m.Role == ChatRole.System);
        _history.Clear();
        if (systemMsg != null)
        {
            _history.Add(systemMsg);
        }
    }
}
