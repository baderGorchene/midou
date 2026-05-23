# CheckPoint Chatbot

AI assistant for the CheckPoint workplace management system.  
Uses **Google Gemini (`gemini-3.5-flash`)** for reasoning and **Entity Framework Core** with Function Calling for direct database access.

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0+ |
| Google AI Studio API Key | Set in `Program.cs` or as an environment variable (`GOOGLE_API_KEY` / `GEMINI_API_KEY`) |
| SQL Server | CheckPoint_DB must exist and be accessible |

## Setup & Usage

### 1. Configure Gemini API Key (Optional)
The application includes a pre-configured fallback API key for Google AI Studio inside `Program.cs`. 
If you prefer to use your own key, set the `GOOGLE_API_KEY` or `GEMINI_API_KEY` environment variable on your system:

#### Windows (PowerShell):
```powershell
$env:GOOGLE_API_KEY="YOUR_API_KEY"
```

#### Linux/macOS:
```bash
export GOOGLE_API_KEY="YOUR_API_KEY"
```

### 2. Start the Application
Navigate to the project directory and run the .NET application:
```bash
cd PFE.Chatbot
dotnet run
```

### 3. Seed mock data (Optional)
Upon startup, the console will ask if you want to seed the database with mock data. 
Type `y` if you want to populate the database with test employees, departments, leave requests, etc.

### Example queries
- "How many employees are in each department?"
- "Show me all pending leave requests"
- "What events are coming up?"
- "Tell me about Fatima Zahra"
- "Are there any active announcements?"
- "What are the overall company statistics?"

## Architecture

```text
You ──► Program.cs (CLI) ──► ChatbotAgent ──► Google Gemini (gemini-3.5-flash via Google.GenAI)
                                   │
                           Function Calling
                                   │
                             DatabaseTools
                                   │
                       SQL Server (CheckPoint_DB via EF Core)
```

## AI Tools Available

The chatbot is equipped with the following tools to query real-time data:

| Tool | Description |
|---|---|
| `GetDepartments` | List departments with employee counts |
| `GetEmployees` | List employees, optionally filter by department |
| `GetEmployeeDetails` | Get detailed information for a specific employee |
| `GetLeaveRequests` | Leave requests, optionally filter by status (Pending, Approved, Rejected) |
| `GetEvents` | Upcoming company events |
| `GetRooms` | All conference, meeting, and training rooms |
| `GetAnnouncements` | Active company announcements |
| `GetGeneralRequests` | Support requests, optionally filter by status |
| `GetStatistics` | Overall company dashboard stats |
