# Part 3 — CyberSecurity Awareness Chatbot

A Windows Forms chatbot with a task assistant, a quiz game, NLP-style keyword
detection, and an activity log. Built on top of the Part 1 and Part 2 chatbot.
All code lives in a single file: `Program.cs`.

**No downloads or NuGet packages required.** Tasks are stored in a local
`tasks.json` file using `System.Text.Json`, which ships built into .NET 8.

---

## Requirements

- Windows 10 or 11
- Visual Studio 2022
- .NET 8.0 SDK

That's it — no database server, no NuGet packages, no internet connection needed.

---

## How to Set Up

1. Open Visual Studio and create a new **Windows Forms App** project
2. Name it **Part2** and select **.NET 8.0** as the framework
3. Delete the default `Form1.cs`, `Form1.Designer.cs` and `Program.cs`
4. Add the provided `Program.cs` and `Part2.csproj` into the project
5. Press **F5** to run

A file called `tasks.json` will be created automatically the first time you
add a task — this is your task storage file, no setup required.

---

## Adding the Audio File

Place your `Chat bot.wav` file in:
```
bin\Debug\net8.0-windows\
```

Or add it through Visual Studio so it copies automatically every build:
1. Right-click the project → **Add → Existing Item** → select `chat bot.wav`
2. Click the file in Solution Explorer, then in Properties set:
   - **Build Action** → `Content`
   - **Copy to Output Directory** → `Copy always`

---

## What's New in Part 3

### 📝 Task Assistant
- Add cybersecurity tasks with a title, description, and optional reminder
- Tasks are stored permanently in a local file (`tasks.json`)
- View, complete, or delete tasks through chat commands or the **My Tasks** panel

### 🎮 Cybersecurity Quiz
- 13 questions mixing multiple-choice and true/false formats
- One question shown at a time with instant feedback and an explanation
- Final score with encouragement based on performance

### 🧠 NLP-Style Understanding
- Recognises differently-worded requests using keyword and pattern matching
- "Add task - enable 2FA", "Remind me to update my password tomorrow", and
  "Can you remind me to check my privacy settings" are all understood

### 📜 Activity Log
- Every task, reminder, and quiz attempt is automatically recorded
- Ask "What have you done for me?" or click **Activity Log** to view it
- Shows the most recent actions, with an option to view full history

---

## How to Use It

| What you type | What happens |
|---|---|
| `Add task - Review privacy settings` | Adds a task, then asks if you'd like a reminder |
| `Yes, remind me in 3 days` | Sets the reminder on the task you just added |
| `Remind me to update my password tomorrow` | Adds a task with a reminder in one step |
| `Show tasks` | Lists all your tasks and their status |
| `Complete task - review privacy settings` | Marks a task as done |
| `Delete task - review privacy settings` | Removes a task |
| `Start quiz` | Opens the cybersecurity quiz |
| `Show activity log` / `What have you done for me?` | Shows recent bot actions |
| `Tell me about phishing` | Carries over from Part 2 — keyword-based tips |
| `I am worried about scams` | Carries over from Part 2 — sentiment detection |
| `quit` or `bye` | Closes the chat |

You can also use the **My Tasks**, **Start Quiz**, and **Activity Log** buttons
in the side panel instead of typing commands.

---

## A Note on the "Database"

The brief asked for MySQL integration. Since no database server was set up,
this version stores tasks in a `tasks.json` file instead, using .NET's
built-in `System.Text.Json` — no installation, no server, no NuGet packages.
It still gives you persistent storage with full add/view/complete/delete
functionality, the same as a real database would.

---

*Part 3 of the PROG POE — CyberSecurity Awareness Chatbot*
