# CheckPoint Chatbot

AI assistant for the CheckPoint workplace management system.  
Uses **Ollama** (e.g., `gemma4:e4b`) for reasoning and **Entity Framework Core** with Function Calling for direct database access.

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 8.0+ |
| Ollama | Running locally with `gemma4:e4b` model (or edit `Program.cs` to use your preferred model) |
| SQL Server | CheckPoint_DB must exist and be accessible |

## Setup & Usage

### 1. Start Ollama
Ensure Ollama is running and you have the model pulled:
```bash
ollama run gemma4:e4b
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
You ──► Program.cs (CLI) ──► ChatbotAgent ──► Ollama (gemma4:e4b via OllamaSharp)
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
