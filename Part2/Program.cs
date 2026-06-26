using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Text.Json;

namespace Part2
{
    internal class Program
    {
        // ══════════════════════════════════════════════════════════════════════
        //  ENTRY POINT
        // ══════════════════════════════════════════════════════════════════════
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WelcomeForm());
        }

        // ══════════════════════════════════════════════════════════════════════
        //  USER MEMORY  —  remembers details about the user
        // ══════════════════════════════════════════════════════════════════════
        class UserMemory
        {
            public string Name { get; set; } = "";
            public string FavouriteTopic { get; set; } = "";
            public string LastTopic { get; set; } = "";
            public string LastSentiment { get; set; } = "";
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TASK ITEM  —  one cybersecurity task
        // ══════════════════════════════════════════════════════════════════════
        class TaskItem
        {
            public int Id { get; set; }
            public string Title { get; set; } = "";
            public string Description { get; set; } = "";
            public DateTime? ReminderDate { get; set; }
            public bool IsCompleted { get; set; }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TASK DATABASE  —  JSON-file-backed storage for tasks (no external DB needed)
        // ══════════════════════════════════════════════════════════════════════
        class TaskDatabase
        {
            private readonly string _filePath;
            private List<TaskItem> _tasks;
            private int _nextId;

            public TaskDatabase()
            {
                _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tasks.json");
                LoadFromDisk();
            }

            // ── Load tasks.json from disk, or start fresh if it doesn't exist ──
            private void LoadFromDisk()
            {
                if (File.Exists(_filePath))
                {
                    try
                    {
                        string json = File.ReadAllText(_filePath);
                        _tasks = JsonSerializer.Deserialize<List<TaskItem>>(json) ?? new List<TaskItem>();
                    }
                    catch
                    {
                        _tasks = new List<TaskItem>();
                    }
                }
                else
                {
                    _tasks = new List<TaskItem>();
                }

                _nextId = _tasks.Count > 0 ? _tasks.Max(t => t.Id) + 1 : 1;
            }

            // ── Save the current task list to tasks.json ────────────────────────
            private void SaveToDisk()
            {
                string json = JsonSerializer.Serialize(_tasks, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }

            // ── Insert a new task, returns the new task's Id ───────────────────
            public int AddTask(string title, string description, DateTime? reminder)
            {
                TaskItem task = new TaskItem
                {
                    Id = _nextId++,
                    Title = title,
                    Description = description ?? "",
                    ReminderDate = reminder,
                    IsCompleted = false
                };
                _tasks.Add(task);
                SaveToDisk();
                return task.Id;
            }

            // ── Get every task, most recently added first ──────────────────────
            public List<TaskItem> GetAllTasks()
            {
                return _tasks.OrderByDescending(t => t.Id).ToList();
            }

            // ── Set / update a reminder date for an existing task ──────────────
            public void SetReminder(int id, DateTime reminder)
            {
                TaskItem task = _tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    task.ReminderDate = reminder;
                    SaveToDisk();
                }
            }

            // ── Mark a task as completed ────────────────────────────────────────
            public void CompleteTask(int id)
            {
                TaskItem task = _tasks.FirstOrDefault(t => t.Id == id);
                if (task != null)
                {
                    task.IsCompleted = true;
                    SaveToDisk();
                }
            }

            // ── Delete a task permanently ───────────────────────────────────────
            public void DeleteTask(int id)
            {
                _tasks.RemoveAll(t => t.Id == id);
                SaveToDisk();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVITY LOG  —  records actions the bot has taken
        // ══════════════════════════════════════════════════════════════════════
        class LogEntry
        {
            public DateTime Timestamp { get; set; }
            public string Description { get; set; } = "";
        }

        class ActivityLog
        {
            private List<LogEntry> _entries = new List<LogEntry>();

            // ── Record a new action ────────────────────────────────────────────
            public void Add(string description)
            {
                _entries.Add(new LogEntry { Timestamp = DateTime.Now, Description = description });
            }

            // ── Get the most recent N actions, newest first ────────────────────
            public List<LogEntry> GetRecent(int count)
            {
                return _entries
                    .Skip(Math.Max(0, _entries.Count - count))
                    .OrderByDescending(e => e.Timestamp)
                    .ToList();
            }

            // ── Get the full history, newest first ─────────────────────────────
            public List<LogEntry> GetAll()
            {
                return _entries.OrderByDescending(e => e.Timestamp).ToList();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  QUIZ QUESTION  —  one quiz question (multiple-choice or true/false)
        // ══════════════════════════════════════════════════════════════════════
        enum QuestionType { MultipleChoice, TrueFalse }

        class QuizQuestion
        {
            public string Question { get; set; } = "";
            public QuestionType Type { get; set; }
            public string[] Options { get; set; } = Array.Empty<string>();
            public int CorrectIndex { get; set; }
            public string Explanation { get; set; } = "";
        }

        // ══════════════════════════════════════════════════════════════════════
        //  QUIZ ENGINE  —  holds the question bank and tracks progress
        // ══════════════════════════════════════════════════════════════════════
        class QuizEngine
        {
            private List<QuizQuestion> _questions = new List<QuizQuestion>();

            public int CurrentIndex { get; private set; }
            public int Score { get; private set; }
            public int TotalQuestions => _questions.Count;
            public bool HasNext => CurrentIndex < _questions.Count;

            public QuizEngine()
            {
                BuildQuestions();
            }

            public void Reset()
            {
                CurrentIndex = 0;
                Score = 0;
            }

            public void AddScore() => Score++;
            public void Advance() => CurrentIndex++;

            public QuizQuestion GetCurrentQuestion() => _questions[CurrentIndex];

            // ── The question bank — 13 questions, mixed types ──────────────────
            private void BuildQuestions()
            {
                _questions.Add(new QuizQuestion
                {
                    Question = "What should you do if you receive an email asking for your password?",
                    Type = QuestionType.MultipleChoice,
                    Options = new[] { "Reply with your password", "Delete the email", "Report the email as phishing", "Ignore it" },
                    CorrectIndex = 2,
                    Explanation = "Reporting phishing emails helps prevent scams and warns others."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "It is safe to use the same password for multiple accounts.",
                    Type = QuestionType.TrueFalse,
                    Options = new[] { "True", "False" },
                    CorrectIndex = 1,
                    Explanation = "Reusing passwords means one breach can compromise all your accounts."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "Which of these is the strongest password?",
                    Type = QuestionType.MultipleChoice,
                    Options = new[] { "password123", "Sarah2020", "Tr@vel$unset99!", "qwerty" },
                    CorrectIndex = 2,
                    Explanation = "Mixing upper/lowercase, symbols and numbers in a long, unrelated string makes passwords far harder to crack."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "Public Wi-Fi networks are always safe for online banking.",
                    Type = QuestionType.TrueFalse,
                    Options = new[] { "True", "False" },
                    CorrectIndex = 1,
                    Explanation = "Public Wi-Fi is often unsecured, which can expose your data to attackers."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "What is phishing?",
                    Type = QuestionType.MultipleChoice,
                    Options = new[] { "A type of computer virus", "A fraudulent attempt to obtain sensitive information", "A firewall setting", "A type of antivirus software" },
                    CorrectIndex = 1,
                    Explanation = "Phishing tricks users into revealing sensitive information, often through fake emails or websites."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "Two-factor authentication adds an extra layer of security to your accounts.",
                    Type = QuestionType.TrueFalse,
                    Options = new[] { "True", "False" },
                    CorrectIndex = 0,
                    Explanation = "2FA requires a second verification step, making accounts much harder to breach."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "What should you check before clicking a link in an email?",
                    Type = QuestionType.MultipleChoice,
                    Options = new[] { "The colour of the email", "The sender's address and the link destination", "The time it was sent", "Nothing, just click it" },
                    CorrectIndex = 1,
                    Explanation = "Verifying the sender and hovering over links helps you spot phishing attempts before it's too late."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "Updating your software regularly helps protect against security vulnerabilities.",
                    Type = QuestionType.TrueFalse,
                    Options = new[] { "True", "False" },
                    CorrectIndex = 0,
                    Explanation = "Updates often patch security holes that attackers actively try to exploit."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "Which of the following is an example of social engineering?",
                    Type = QuestionType.MultipleChoice,
                    Options = new[] { "Installing antivirus software", "Someone pretending to be IT support to get your password", "Using a VPN", "Backing up your files" },
                    CorrectIndex = 1,
                    Explanation = "Social engineering manipulates people into giving up confidential information."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "A VPN hides and encrypts your internet traffic.",
                    Type = QuestionType.TrueFalse,
                    Options = new[] { "True", "False" },
                    CorrectIndex = 0,
                    Explanation = "VPNs encrypt your connection, making it much harder for attackers to intercept your data."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "What is the safest way to access your bank account online?",
                    Type = QuestionType.MultipleChoice,
                    Options = new[] { "Through a link in an email", "Typing the website address directly into your browser", "Through a pop-up ad", "Through a public Wi-Fi hotspot" },
                    CorrectIndex = 1,
                    Explanation = "Typing the address directly avoids phishing links that mimic real websites."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "Why should you avoid downloading attachments from unknown senders?",
                    Type = QuestionType.MultipleChoice,
                    Options = new[] { "They take up storage space", "They might contain malware", "They are usually too large", "They slow down your email client" },
                    CorrectIndex = 1,
                    Explanation = "Unknown attachments can contain malware that infects your device."
                });

                _questions.Add(new QuizQuestion
                {
                    Question = "It's a good idea to use a password manager to store your passwords.",
                    Type = QuestionType.TrueFalse,
                    Options = new[] { "True", "False" },
                    CorrectIndex = 0,
                    Explanation = "Password managers let you use strong, unique passwords without having to memorise them all."
                });
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CHATBOT ENGINE  —  all response logic, NLP intent detection,
        //                      task management, and activity logging
        // ══════════════════════════════════════════════════════════════════════
        class ChatbotEngine
        {
            private Random _rng = new Random();

            public UserMemory Memory { get; private set; } = new UserMemory();
            public TaskDatabase Tasks { get; private set; } = new TaskDatabase();
            public ActivityLog Log { get; private set; } = new ActivityLog();
            public QuizEngine Quiz { get; private set; } = new QuizEngine();

            // Tells the GUI to open a dialog after GetResponse() returns
            public enum BotAction { None, OpenQuiz, OpenTaskManager, OpenActivityLog }
            public BotAction PendingAction { get; set; } = BotAction.None;

            // State used while waiting for "yes/no, remind me in X days"
            private int? _awaitingReminderForTaskId = null;
            private string _awaitingReminderTaskTitle = "";

            // ── Keyword → single response (Part 2 carry-over) ─────────────────
            private Dictionary<string, string> _keywordResponses =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "scam",
                  "Scammers often pretend to be banks or government agencies. Never click suspicious links and never share personal details unless YOU made contact first." },
                { "privacy",
                  "Protecting your privacy starts with reviewing app permissions. Only allow apps access to what they truly need and check your social media privacy settings regularly." },
                { "malware",
                  "Malware is malicious software designed to damage or spy on your device. Keep your antivirus up to date and avoid downloading files from unknown sources." },
                { "ransomware",
                  "Ransomware locks your files and demands payment to unlock them. Back up your data regularly and never open email attachments from unknown senders." },
                { "firewall",
                  "A firewall is a barrier between your device and threats on the internet. Always make sure your device firewall is switched on." },
                { "vpn",
                  "A VPN encrypts your internet traffic, making it very difficult for attackers to intercept your data — especially important on public Wi-Fi." },
                { "two-factor",
                  "Two-factor authentication (2FA) means even if someone steals your password they still cannot get into your account without a second verification step." },
                { "2fa",
                  "Enabling 2FA is one of the most effective things you can do for online security. A stolen password alone will not be enough to hijack your account." },
                { "social engineering",
                  "Social engineering tricks people into handing over confidential information. Always verify the identity of anyone asking for sensitive data, even if they seem trustworthy." },
                { "public wi-fi",
                  "Public Wi-Fi is often unsecured. Avoid accessing banking or sensitive accounts on it. Use a VPN if you really have to connect." },
                { "email",
                  "Always check the sender address carefully. Avoid clicking links or opening attachments in emails you were not expecting." },
                { "browsing",
                  "Safe browsing means sticking to HTTPS websites, ignoring suspicious pop-ups and not downloading software from unofficial sources." },
                { "identity theft",
                  "Identity theft is when someone uses your personal information to commit fraud. Shred sensitive documents, use strong passwords and monitor your bank statements regularly." },
                { "antivirus",
                  "Antivirus software scans your device for threats and removes them. Keep it updated so it can catch the latest malware." },
                { "backup",
                  "Regularly backing up data to an external drive or cloud means you will not permanently lose your files to ransomware or hardware failure." },
            };

            private List<string> _phishingTips = new List<string>
            {
                "Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations.",
                "Check the sender address carefully — phishers use addresses that look almost right, like 'support@paypa1.com'.",
                "Hover over links before clicking to see the real destination. If the URL looks odd, do not click it.",
                "Phishing messages create urgency — 'Your account will be closed!' is a classic trick to rush you into a mistake.",
                "A legitimate company will never ask for your password via email. Such a request is almost certainly phishing.",
                "Always type a website address directly into your browser rather than following links from emails."
            };

            private List<string> _passwordTips = new List<string>
            {
                "Use a passphrase — something like 'BlueSky$Lamp42!' is long and much easier to remember than a random jumble.",
                "Never reuse passwords across websites. If one site is breached, every account sharing that password is at risk.",
                "Consider using a reputable password manager to create and store unique, complex passwords for every account.",
                "Change a password immediately if you suspect that account has been compromised.",
                "Avoid obvious passwords like '123456', 'password' or your own name — these are the very first things attackers try."
            };

            private Dictionary<string, string> _sentimentResponses =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "worried",     "It is completely understandable to feel worried — cyber threats are real. But knowing about them is already your biggest defence. Let me share something helpful." },
                { "scared",      "There is no need to panic. You are already taking the right step by learning! Let me share something reassuring." },
                { "frustrated",  "I understand this can feel overwhelming. Let us take it one step at a time. Here is something straightforward to help." },
                { "confused",    "No problem at all — cybersecurity can be complex. Let me explain things more clearly for you." },
                { "curious",     "Curiosity is the best starting point! You are already on the right track. Here is something interesting." },
                { "happy",       "Great to hear! Staying positive while learning makes you even more prepared. Here is a tip to keep the momentum going." },
                { "overwhelmed", "Take a breath — you do not have to learn everything at once. Let us focus on one important thing at a time." },
                { "anxious",     "It is natural to feel anxious about online threats. The good news is that a few simple habits can protect you significantly. Here is where to start." },
                { "unsure",      "Being unsure is perfectly fine — it means you are thinking carefully. Let me help clarify things." },
            };

            private List<string> _followUpPhrases = new List<string>
            {
                "tell me more", "explain more", "more details", "give me another",
                "another tip", "go on", "continue", "more info", "elaborate", "expand on that"
            };

            private List<string> _generalTips = new List<string>
            {
                "Keep your software and operating system updated — many attacks exploit outdated software.",
                "Enable two-factor authentication wherever you can. It dramatically reduces the chance of an account being hijacked.",
                "Be careful about what personal information you share on social media. Attackers piece together details to target you.",
                "Back up your data regularly so ransomware cannot hold you hostage.",
                "When in doubt, do not click! Suspicious links and attachments are one of the most common ways malware spreads."
            };

            // ─────────────────────────────────────────────────────────────────
            //  PUBLIC — main entry point, returns a response for any input
            // ─────────────────────────────────────────────────────────────────
            public string GetResponse(string userInput)
            {
                PendingAction = BotAction.None;

                if (string.IsNullOrWhiteSpace(userInput))
                    return "Please type something so I can help you! 😊";

                string input = userInput.Trim().ToLower();

                // 0. Waiting for a yes/no reminder answer from a previous message
                if (_awaitingReminderForTaskId != null)
                    return HandleReminderResponse(input);

                // 1. "What have you done for me?" / activity log
                if (IsActivityLogIntent(input))
                    return FormatActivityLog();

                // 2. Quiz request
                if (IsQuizIntent(input))
                {
                    PendingAction = BotAction.OpenQuiz;
                    return "🎮 Great! Let's test your cybersecurity knowledge. Launching the quiz now...";
                }

                // 3. Task management intents (NLP-style keyword detection)
                if (IsViewTasksIntent(input))
                    return FormatTaskList();

                if (IsCompleteTaskIntent(input))
                    return HandleCompleteTask(input);

                if (IsDeleteTaskIntent(input))
                    return HandleDeleteTask(input);

                if (IsAddTaskIntent(input))
                    return HandleAddTask(input, userInput);

                // 4. Follow-up phrases ("tell me more", "another tip"...)
                foreach (string phrase in _followUpPhrases)
                {
                    if (input.Contains(phrase))
                        return HandleFollowUp();
                }

                // 5. Memory queries ("I am interested in X")
                string memResponse = HandleMemoryInput(input);
                if (!string.IsNullOrEmpty(memResponse))
                    return memResponse;

                // 6. Sentiment + topic detection (Part 2 logic)
                string sentiment = DetectSentiment(input);
                string topic = DetectTopic(input);

                if (!string.IsNullOrEmpty(sentiment) && !string.IsNullOrEmpty(topic))
                    return sentiment + "\n\n" + topic;

                if (!string.IsNullOrEmpty(sentiment) && string.IsNullOrEmpty(topic))
                    return sentiment + "\n\n" + _generalTips[_rng.Next(_generalTips.Count)];

                if (!string.IsNullOrEmpty(topic))
                    return topic;

                // 7. Default fallback
                return "I am not sure I understand. Could you try rephrasing? " +
                       "You can ask me about cybersecurity topics, say 'add task - ...', 'show tasks', " +
                       "'start quiz', or 'show activity log'. 🔐";
            }

            // ═════════════════════════════════════════════════════════════════
            //  NLP-STYLE INTENT DETECTION  (simple string/Regex matching)
            // ═════════════════════════════════════════════════════════════════
            private bool IsAddTaskIntent(string input)
            {
                string[] triggers =
                {
                    "add task", "add a task", "create task", "create a task", "new task",
                    "remind me to", "set a reminder to", "set reminder to"
                };
                return triggers.Any(t => input.Contains(t));
            }

            private bool IsViewTasksIntent(string input)
            {
                string[] triggers =
                {
                    "show tasks", "view tasks", "my tasks", "list tasks",
                    "see my tasks", "task list", "show my tasks", "what are my tasks"
                };
                return triggers.Any(t => input.Contains(t));
            }

            private bool IsCompleteTaskIntent(string input)
            {
                string[] triggers =
                {
                    "complete task", "mark task", "finish task", "done with",
                    "mark as done", "task done", "completed task", "mark complete"
                };
                return triggers.Any(t => input.Contains(t));
            }

            private bool IsDeleteTaskIntent(string input)
            {
                string[] triggers = { "delete task", "remove task", "cancel task" };
                return triggers.Any(t => input.Contains(t));
            }

            private bool IsQuizIntent(string input)
            {
                string[] triggers =
                {
                    "start quiz", "play quiz", "take quiz", "quiz me",
                    "test my knowledge", "begin quiz", "start the quiz", "do a quiz"
                };
                return triggers.Any(t => input.Contains(t));
            }

            private bool IsActivityLogIntent(string input)
            {
                string[] triggers =
                {
                    "show activity log", "what have you done", "show log",
                    "activity history", "what have you done for me",
                    "show my activity", "recent actions", "activity log"
                };
                return triggers.Any(t => input.Contains(t));
            }

            // ═════════════════════════════════════════════════════════════════
            //  TASK ASSISTANT HANDLERS
            // ═════════════════════════════════════════════════════════════════
            private string HandleAddTask(string lowerInput, string originalInput)
            {
                string title = ExtractTaskTitle(lowerInput);
                if (string.IsNullOrWhiteSpace(title))
                    return "I can add that for you — what should the task be? " +
                           "Try something like 'Add task - Enable two-factor authentication'.";

                string description = BuildTaskDescription(title);
                DateTime? reminder = ExtractReminderDate(lowerInput);

                int taskId = Tasks.AddTask(title, description, reminder);

                if (reminder != null)
                {
                    Log.Add($"Task added: '{title}' with reminder on {reminder:dd MMM yyyy}.");
                    return $"✅ Task added: \"{title}\"\n📋 {description}\n⏰ Reminder set for {reminder:dd MMM yyyy}.";
                }
                else
                {
                    Log.Add($"Task added: '{title}' (no reminder set yet).");
                    _awaitingReminderForTaskId = taskId;
                    _awaitingReminderTaskTitle = title;
                    return $"✅ Task added with the description \"{description}\"\n" +
                           "Would you like a reminder? (e.g. 'Yes, remind me in 3 days' or 'No')";
                }
            }

            private string HandleReminderResponse(string input)
            {
                int taskId = _awaitingReminderForTaskId.Value;
                string title = _awaitingReminderTaskTitle;
                _awaitingReminderForTaskId = null;
                _awaitingReminderTaskTitle = "";

                if (input.Contains("no"))
                {
                    Log.Add($"No reminder set for task '{title}'.");
                    return "No problem! I won't set a reminder for that task. " +
                           "You can add one later by saying 'set a reminder to " + title.ToLower() + "'.";
                }

                DateTime? reminder = ExtractReminderDate(input) ?? DateTime.Now.AddDays(7);
                Tasks.SetReminder(taskId, reminder.Value);
                Log.Add($"Reminder set for '{title}' on {reminder:dd MMM yyyy}.");
                return $"Got it! I'll remind you about \"{title}\" on {reminder:dd MMM yyyy}. 🔔";
            }

            private string HandleCompleteTask(string input)
            {
                List<TaskItem> tasks = Tasks.GetAllTasks();
                foreach (TaskItem t in tasks)
                {
                    if (!t.IsCompleted && input.Contains(t.Title.ToLower()))
                    {
                        Tasks.CompleteTask(t.Id);
                        Log.Add($"Task marked as completed: '{t.Title}'.");
                        return $"✅ Marked \"{t.Title}\" as completed. Great job staying on top of your security!";
                    }
                }
                return "I couldn't find that task. Try 'show tasks' to see the exact titles, then say " +
                       "'complete task - [title]'.";
            }

            private string HandleDeleteTask(string input)
            {
                List<TaskItem> tasks = Tasks.GetAllTasks();
                foreach (TaskItem t in tasks)
                {
                    if (input.Contains(t.Title.ToLower()))
                    {
                        Tasks.DeleteTask(t.Id);
                        Log.Add($"Task deleted: '{t.Title}'.");
                        return $"🗑️ Deleted the task \"{t.Title}\".";
                    }
                }
                return "I couldn't find that task to delete. Try 'show tasks' to see the exact titles.";
            }

            private string FormatTaskList()
            {
                List<TaskItem> tasks = Tasks.GetAllTasks();
                if (tasks.Count == 0)
                    return "You don't have any tasks yet. Try saying 'Add task - Enable two-factor authentication'.";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("📋 Your Cybersecurity Tasks:");
                int i = 1;
                foreach (TaskItem t in tasks)
                {
                    string status = t.IsCompleted ? "✅ Done" : "⏳ Pending";
                    string rem = t.ReminderDate.HasValue ? $"  |  ⏰ {t.ReminderDate:dd MMM yyyy}" : "";
                    sb.AppendLine($"{i}. [{status}] {t.Title}{rem}");
                    i++;
                }
                return sb.ToString().TrimEnd();
            }

            // ── Extract a task title from natural phrasing ─────────────────────
            private string ExtractTaskTitle(string input)
            {
                string[] markers =
                {
                    "add a task to", "add task to", "add a task -", "add task -",
                    "add a task:", "add task:", "create a task to", "create task to",
                    "new task to", "add task", "add a task", "remind me to",
                    "set a reminder to", "set reminder to"
                };

                foreach (string marker in markers)
                {
                    int idx = input.IndexOf(marker);
                    if (idx >= 0)
                    {
                        string rest = input.Substring(idx + marker.Length).Trim();
                        rest = StripReminderPhrase(rest);
                        if (!string.IsNullOrWhiteSpace(rest))
                            return CapitalizeFirst(rest);
                    }
                }
                return "";
            }

            // ── Remove trailing date phrases like "tomorrow" or "in 3 days" ────
            private string StripReminderPhrase(string text)
            {
                text = Regex.Replace(text, @"\b(tomorrow|today)\b", "", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\bin\s+\d+\s+(day|days|week|weeks)\b", "", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\band remind me.*$", "", RegexOptions.IgnoreCase);
                text = Regex.Replace(text, @"\bnext week\b", "", RegexOptions.IgnoreCase);
                return text.Trim(' ', '-', ':', '.');
            }

            // ── Pull a relative or absolute date out of free text ──────────────
            private DateTime? ExtractReminderDate(string input)
            {
                if (input.Contains("tomorrow")) return DateTime.Now.AddDays(1);
                if (input.Contains("today")) return DateTime.Now;
                if (input.Contains("next week")) return DateTime.Now.AddDays(7);

                Match m = Regex.Match(input, @"in\s+(\d+)\s+day");
                if (m.Success) return DateTime.Now.AddDays(int.Parse(m.Groups[1].Value));

                m = Regex.Match(input, @"in\s+(\d+)\s+week");
                if (m.Success) return DateTime.Now.AddDays(int.Parse(m.Groups[1].Value) * 7);

                return null;
            }

            // ── Build a sensible description for a new task ────────────────────
            private string BuildTaskDescription(string title)
            {
                string lower = title.ToLower();
                foreach (var pair in _keywordResponses)
                {
                    if (lower.Contains(pair.Key.ToLower()))
                        return pair.Value;
                }
                if (lower.Contains("password")) return _passwordTips[0];
                if (lower.Contains("phishing")) return _phishingTips[0];
                return $"Complete the cybersecurity task: {title}.";
            }

            private string CapitalizeFirst(string text)
            {
                if (string.IsNullOrEmpty(text)) return text;
                return char.ToUpper(text[0]) + text.Substring(1);
            }

            // ═════════════════════════════════════════════════════════════════
            //  ACTIVITY LOG FORMATTING
            // ═════════════════════════════════════════════════════════════════
            private string FormatActivityLog()
            {
                List<LogEntry> entries = Log.GetRecent(8);
                if (entries.Count == 0)
                    return "I haven't logged any actions yet. Try adding a task or playing the quiz!";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("📜 Here's a summary of recent actions:");
                int i = 1;
                foreach (LogEntry e in entries)
                {
                    sb.AppendLine($"{i}. {e.Description} ({e.Timestamp:dd MMM, HH:mm})");
                    i++;
                }
                return sb.ToString().TrimEnd();
            }

            // ═════════════════════════════════════════════════════════════════
            //  PART 2 LOGIC — sentiment, topics, follow-ups, memory  (unchanged)
            // ═════════════════════════════════════════════════════════════════
            private string DetectSentiment(string input)
            {
                foreach (var pair in _sentimentResponses)
                {
                    if (input.Contains(pair.Key))
                    {
                        Memory.LastSentiment = pair.Key;
                        return pair.Value;
                    }
                }
                return "";
            }

            private string DetectTopic(string input)
            {
                if (input.Contains("phishing") || input.Contains("phish"))
                {
                    Memory.LastTopic = "phishing";
                    CheckFavourite("phishing", input);
                    return "🎣 Phishing Tip:\n" + _phishingTips[_rng.Next(_phishingTips.Count)];
                }

                if (input.Contains("password"))
                {
                    Memory.LastTopic = "password";
                    CheckFavourite("password", input);
                    return "🔑 Password Tip:\n" + _passwordTips[_rng.Next(_passwordTips.Count)];
                }

                foreach (var pair in _keywordResponses)
                {
                    if (input.Contains(pair.Key.ToLower()))
                    {
                        Memory.LastTopic = pair.Key;
                        CheckFavourite(pair.Key, input);
                        string label = char.ToUpper(pair.Key[0]) + pair.Key.Substring(1);
                        return $"🔐 {label}:\n{pair.Value}";
                    }
                }
                return "";
            }

            private string HandleFollowUp()
            {
                if (string.IsNullOrEmpty(Memory.LastTopic))
                    return "We have not covered a specific topic yet! " +
                           "Ask me about phishing, passwords, scams or any cybersecurity topic and I will tell you more. 😊";

                string t = Memory.LastTopic.ToLower();

                if (t == "phishing")
                    return "🎣 Another Phishing Tip:\n" + _phishingTips[_rng.Next(_phishingTips.Count)];

                if (t == "password")
                    return "🔑 Another Password Tip:\n" + _passwordTips[_rng.Next(_passwordTips.Count)];

                if (_keywordResponses.ContainsKey(t))
                {
                    string label = char.ToUpper(t[0]) + t.Substring(1);
                    return $"🔐 More on {label}:\n{_keywordResponses[t]}\n\n" +
                           "Would you like to explore another topic? Try asking about VPNs, two-factor authentication, or safe browsing!";
                }

                return "💡 General Tip:\n" + _generalTips[_rng.Next(_generalTips.Count)];
            }

            private string HandleMemoryInput(string input)
            {
                bool isInterest = input.Contains("interested in") ||
                                  input.Contains("i like") ||
                                  input.Contains("i care about");

                bool isQuery = input.Contains("what do i like") ||
                                  input.Contains("what am i interested in") ||
                                  input.Contains("what do you remember");

                if (isInterest)
                {
                    if (input.Contains("phishing"))
                    {
                        Memory.FavouriteTopic = "phishing";
                        return "Great! I will remember that you are interested in phishing. " +
                               "Knowing how to spot phishing attempts is one of the most valuable cybersecurity skills you can have! 🌐";
                    }
                    if (input.Contains("password"))
                    {
                        Memory.FavouriteTopic = "passwords";
                        return "Great! I will remember that you are interested in passwords. " +
                               "Strong password habits prevent the majority of account breaches! 🌐";
                    }
                    foreach (var pair in _keywordResponses)
                    {
                        if (input.Contains(pair.Key.ToLower()))
                        {
                            Memory.FavouriteTopic = pair.Key;
                            return $"Great! I will remember that you are interested in {pair.Key}. " +
                                   "It is a crucial part of staying safe online. 🌐\n\n" +
                                   $"Any time you want tips on {pair.Key}, just ask!";
                        }
                    }
                }

                if (isQuery && !string.IsNullOrEmpty(Memory.FavouriteTopic))
                {
                    return $"You mentioned being interested in {Memory.FavouriteTopic}. " +
                           $"As someone interested in {Memory.FavouriteTopic}, " +
                           "you might want to review the security settings on your accounts! 💡";
                }

                return "";
            }

            private void CheckFavourite(string topic, string input)
            {
                if (input.Contains("favourite") || input.Contains("favorite") ||
                    input.Contains("love") || input.Contains("most interested"))
                {
                    Memory.FavouriteTopic = topic;
                }
            }

            public void SetUserName(string name)
            {
                Memory.Name = name;
            }

            public static void PlayGreeting()
            {
                try
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Chat bot.wav");
                    if (File.Exists(path))
                    {
                        SoundPlayer player = new SoundPlayer(path);
                        player.Play();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Sound error: " + ex.Message);
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  WELCOME FORM  —  splash screen with ASCII art and name entry
        // ══════════════════════════════════════════════════════════════════════
        class WelcomeForm : Form
        {
            private TextBox _nameBox;
            private Button _startButton;

            public WelcomeForm()
            {
                ChatbotEngine.PlayGreeting();
                BuildUI();
            }

            private void BuildUI()
            {
                Text = "CyberSecurity Awareness Bot — Part 2";
                Size = new Size(920, 600);
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(10, 12, 20);
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;
                Font = new Font("Consolas", 9f);

                Controls.Add(MakeDivider(12));

                string art =
                    "   _ _  _____      _                                        _ _            \n" +
                    " ( | )/ ____|    | |                                      (_) |           \n" +
                    "  V V| |    _   _| |__   ___ _ __ ___  ___  ___ _   _ _ __ _| |_ _   _   \n" +
                    "     | |   | | | | '_ \\ / _ \\ '__/ __|/ _ \\/ __| | | | '__| | __| | | |  \n" +
                    "     | |___| |_| | |_) |  __/ |  \\__ \\  __/ (__| |_| | |  | | |_| |_| |  \n" +
                    "      \\_____\\__, |_.__/ \\___|_|  |___/\\___|\\___|\\__,_|_|  |_|\\__|\\__, |  \n" +
                    "             __/ |                                                __/ |  \n" +
                    "            |___/                                       ____     |___/    \n" +
                    "      /\\                                               |  _ \\      | |    \n" +
                    "     /  \\__      ____ _ _ __ ___ _ __   ___  ___ ___  | |_) | ___ | |    \n" +
                    "    / /\\ \\ \\ /\\ / / _` | '__/ _ \\ '_ \\ / _ \\/ __/ __| |  _ < / _ \\| |   \n" +
                    "   / ____ \\ V  V / (_| | | |  __/ | | |  __/\\__ \\__ \\ | |_) | (_) | |   \n" +
                    "  /_/    \\_\\_/\\_/ \\__,_|_|  \\___|_| |_|\\___||___/___/ |____/ \\___/ \\__|  \n" +
                    "                                                                          \n" +
                    "               Your virtual assistant chat bot";

                Controls.Add(new Label
                {
                    Text = art,
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 7.5f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(15, 28)
                });

                Controls.Add(MakeDivider(245));

                Controls.Add(MakeLabel(
                    " Activate me by providing your name...",
                    new Point(15, 268),
                    Color.FromArgb(160, 210, 255), 10f));

                Controls.Add(MakeLabel(
                    " What should I call you?",
                    new Point(15, 298),
                    Color.FromArgb(0, 220, 180), 11f, FontStyle.Bold));

                _nameBox = new TextBox
                {
                    Location = new Point(15, 332),
                    Size = new Size(360, 34),
                    Font = new Font("Consolas", 12f),
                    BackColor = Color.FromArgb(16, 26, 42),
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BorderStyle = BorderStyle.FixedSingle,
                    PlaceholderText = "Enter your name..."
                };
                _nameBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) ActivateBot(); };
                Controls.Add(_nameBox);

                _startButton = new Button
                {
                    Text = "▶  START CHAT",
                    Location = new Point(390, 330),
                    Size = new Size(150, 36),
                    Font = new Font("Consolas", 10f, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 175, 135),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                _startButton.FlatAppearance.BorderSize = 0;
                _startButton.Click += (s, e) => ActivateBot();
                Controls.Add(_startButton);

                Controls.Add(MakeDivider(395));

                Controls.Add(MakeLabel(
                    " 💡 Now with Tasks, a Quiz, and an Activity Log! Type 'show tasks', 'start quiz' or 'show activity log'.",
                    new Point(15, 415),
                    Color.FromArgb(70, 130, 170), 8f));

                AcceptButton = _startButton;
            }

            private void ActivateBot()
            {
                string name = _nameBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(name))
                {
                    MessageBox.Show("Please enter your name to continue!",
                        "Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ChatForm chat = new ChatForm(name);
                chat.Show();
                Hide();
                chat.FormClosed += (s, args) => Close();
            }

            private Label MakeDivider(int y)
            {
                return new Label
                {
                    Text = new string('─', 112),
                    ForeColor = Color.FromArgb(0, 110, 90),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f),
                    AutoSize = true,
                    Location = new Point(12, y)
                };
            }

            private Label MakeLabel(string text, Point loc, Color color,
                                    float size = 9f, FontStyle style = FontStyle.Regular)
            {
                return new Label
                {
                    Text = text,
                    Location = loc,
                    ForeColor = color,
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", size, style),
                    AutoSize = true
                };
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CHAT FORM  —  the main conversation window
        // ══════════════════════════════════════════════════════════════════════
        class ChatForm : Form
        {
            private ChatbotEngine _engine;
            private string _userName;

            private RichTextBox _chatDisplay;
            private TextBox _inputBox;
            private Button _sendButton;
            private Button _clearButton;
            private Label _memoryLabel;
            private ListBox _topicList;
            private Button _tasksButton;
            private Button _quizButton;
            private Button _logButton;

            public ChatForm(string userName)
            {
                _userName = userName;
                _engine = new ChatbotEngine();
                _engine.SetUserName(userName);
                BuildUI();
                ShowWelcomeMessage();
            }

            private void BuildUI()
            {
                Text = "Part 2 — CyberSecurity Awareness Bot";
                Size = new Size(1130, 740);
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(10, 12, 20);
                FormBorderStyle = FormBorderStyle.Sizable;
                MinimumSize = new Size(860, 600);
                Font = new Font("Consolas", 9f);

                // ═══ TOP PANEL ═══
                Panel topPanel = new Panel { Dock = DockStyle.Top, Height = 54, BackColor = Color.FromArgb(13, 19, 32) };
                topPanel.Controls.Add(new Label
                {
                    Text = $"  🔐  CyberSecurity Awareness Bot  |  Chatting with: {_userName}",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 13f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(8, 8)
                });
                topPanel.Controls.Add(new Label
                {
                    Text = "  ● Online  |  Tasks · Quiz · Activity Log enabled",
                    ForeColor = Color.FromArgb(0, 200, 100),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f),
                    AutoSize = true,
                    Location = new Point(8, 34)
                });
                Controls.Add(topPanel);

                // ═══ RIGHT SIDE PANEL ═══
                Panel sidePanel = new Panel { Dock = DockStyle.Right, Width = 230, BackColor = Color.FromArgb(13, 19, 32), Padding = new Padding(6) };

                sidePanel.Controls.Add(new Label
                {
                    Text = "── Quick Topics ──",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleCenter
                });

                _topicList = new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 168,
                    BackColor = Color.FromArgb(16, 24, 40),
                    ForeColor = Color.FromArgb(155, 205, 255),
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Consolas", 9f)
                };
                _topicList.Items.AddRange(new string[]
                {
                    "🎣  Phishing", "🔑  Passwords", "⚠️   Scams", "🕵️   Privacy",
                    "🦠  Malware", "🌐  VPN", "🔐  Two-Factor Auth", "📧  Email Security"
                });
                _topicList.Click += TopicList_Click;
                sidePanel.Controls.Add(_topicList);

                sidePanel.Controls.Add(new Label
                {
                    Text = "── Tools ──",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleCenter
                });

                _tasksButton = MakeToolButton("📝  My Tasks");
                _tasksButton.Click += (s, e) => OpenTaskManager();
                sidePanel.Controls.Add(_tasksButton);

                _quizButton = MakeToolButton("🎮  Start Quiz");
                _quizButton.Click += (s, e) => OpenQuiz();
                sidePanel.Controls.Add(_quizButton);

                _logButton = MakeToolButton("📜  Activity Log");
                _logButton.Click += (s, e) => OpenActivityLog();
                sidePanel.Controls.Add(_logButton);

                sidePanel.Controls.Add(new Label
                {
                    Text = "── Bot Memory ──",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = 24,
                    TextAlign = ContentAlignment.MiddleCenter
                });

                _memoryLabel = new Label
                {
                    Text = BuildMemoryText(),
                    ForeColor = Color.FromArgb(155, 200, 240),
                    BackColor = Color.FromArgb(16, 24, 40),
                    Font = new Font("Consolas", 7.5f),
                    Dock = DockStyle.Top,
                    Height = 90,
                    Padding = new Padding(6)
                };
                sidePanel.Controls.Add(_memoryLabel);

                Controls.Add(sidePanel);

                // ═══ BOTTOM PANEL ═══
                Panel bottomPanel = new Panel { Dock = DockStyle.Bottom, Height = 64, BackColor = Color.FromArgb(13, 19, 32) };

                bottomPanel.Controls.Add(new Label
                {
                    Text = _userName + " ➤",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 10f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(10, 20)
                });

                _inputBox = new TextBox
                {
                    Location = new Point(90, 17),
                    Size = new Size(690, 32),
                    Font = new Font("Consolas", 11f),
                    BackColor = Color.FromArgb(16, 26, 42),
                    ForeColor = Color.FromArgb(200, 230, 255),
                    BorderStyle = BorderStyle.FixedSingle,
                    PlaceholderText = "Try: 'add task - enable 2FA', 'show tasks', 'start quiz', 'show activity log'..."
                };
                _inputBox.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Send(); } };
                bottomPanel.Controls.Add(_inputBox);

                _sendButton = new Button
                {
                    Text = "SEND ►",
                    Location = new Point(792, 15),
                    Size = new Size(108, 34),
                    Font = new Font("Consolas", 10f, FontStyle.Bold),
                    BackColor = Color.FromArgb(0, 175, 135),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                _sendButton.FlatAppearance.BorderSize = 0;
                _sendButton.Click += (s, e) => Send();
                bottomPanel.Controls.Add(_sendButton);

                _clearButton = new Button
                {
                    Text = "CLEAR",
                    Location = new Point(910, 15),
                    Size = new Size(88, 34),
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    BackColor = Color.FromArgb(55, 14, 14),
                    ForeColor = Color.FromArgb(255, 100, 100),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                _clearButton.FlatAppearance.BorderColor = Color.FromArgb(100, 28, 28);
                _clearButton.Click += (s, e) => { _chatDisplay.Clear(); ShowWelcomeMessage(); };
                bottomPanel.Controls.Add(_clearButton);

                Controls.Add(bottomPanel);

                // ═══ CHAT DISPLAY ═══
                _chatDisplay = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.FromArgb(10, 13, 22),
                    ForeColor = Color.FromArgb(200, 230, 255),
                    Font = new Font("Consolas", 10f),
                    BorderStyle = BorderStyle.None,
                    ReadOnly = true,
                    ScrollBars = RichTextBoxScrollBars.Vertical,
                    Padding = new Padding(8),
                    WordWrap = true
                };
                Controls.Add(_chatDisplay);

                AcceptButton = _sendButton;
                Resize += (s, e) => { if (_inputBox != null) _inputBox.Width = Math.Max(200, ClientSize.Width - 350); };
            }

            private Button MakeToolButton(string text)
            {
                return new Button
                {
                    Text = text,
                    Dock = DockStyle.Top,
                    Height = 36,
                    Margin = new Padding(0, 4, 0, 0),
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    BackColor = Color.FromArgb(20, 34, 52),
                    ForeColor = Color.FromArgb(150, 220, 255),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(8, 0, 0, 0)
                };
            }

            private void ShowWelcomeMessage()
            {
                Separator();
                BotSay($" Hello {_userName}! 👋 Welcome to the CyberSecurity Awareness Bot.");
                BotSay(" Here is what you can do:");
                BotSay("  • Ask me about any cybersecurity topic — phishing, passwords, scams, and more.");
                BotSay("  • Say 'add task - [task]' to add a cybersecurity to-do with an optional reminder.");
                BotSay("  • Say 'show tasks' to see everything you've added, or use the My Tasks button.");
                BotSay("  • Say 'start quiz' or click Start Quiz to test your cybersecurity knowledge.");
                BotSay("  • Say 'show activity log' or click Activity Log to see what I've done for you.");
                BotSay("  • Type 'quit' or 'bye' to exit.");
                Separator();
            }

            private void TopicList_Click(object sender, EventArgs e)
            {
                if (_topicList.SelectedItem == null) return;
                Dictionary<string, string> map = new Dictionary<string, string>
                {
                    { "🎣  Phishing",       "Tell me about phishing" },
                    { "🔑  Passwords",      "Tell me about passwords" },
                    { "⚠️   Scams",          "Tell me about scams" },
                    { "🕵️   Privacy",        "Tell me about privacy" },
                    { "🦠  Malware",        "Tell me about malware" },
                    { "🌐  VPN",            "Tell me about VPN" },
                    { "🔐  Two-Factor Auth","Tell me about two-factor authentication" },
                    { "📧  Email Security", "Tell me about email security" }
                };
                string key = _topicList.SelectedItem.ToString();
                if (map.ContainsKey(key)) ProcessInput(map[key]);
                _topicList.ClearSelected();
            }

            private void Send()
            {
                string text = _inputBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(text)) return;
                _inputBox.Clear();
                ProcessInput(text);
            }

            private void ProcessInput(string input)
            {
                if (input.ToLower() == "quit" || input.ToLower() == "exit" || input.ToLower() == "bye")
                {
                    UserSay(input);
                    BotSay($" Goodbye {_userName}! Stay safe online! 🛡️");
                    Separator();
                    return;
                }

                UserSay(input);
                string response = _engine.GetResponse(input);
                TypewriterSay(response);
                _memoryLabel.Text = BuildMemoryText();
                Separator();

                // Open the relevant dialog if the engine flagged one
                switch (_engine.PendingAction)
                {
                    case ChatbotEngine.BotAction.OpenQuiz: OpenQuiz(); break;
                    case ChatbotEngine.BotAction.OpenTaskManager: OpenTaskManager(); break;
                    case ChatbotEngine.BotAction.OpenActivityLog: OpenActivityLog(); break;
                }
            }

            // ── Open the Task Manager dialog ────────────────────────────────────
            private void OpenTaskManager()
            {
                TaskManagerDialog dlg = new TaskManagerDialog(_engine.Tasks, _engine.Log);
                dlg.ShowDialog(this);
                _memoryLabel.Text = BuildMemoryText();
            }

            // ── Open the Quiz dialog ─────────────────────────────────────────────
            private void OpenQuiz()
            {
                QuizDialog dlg = new QuizDialog(_engine.Quiz, _engine.Log);
                dlg.ShowDialog(this);
            }

            // ── Open the Activity Log dialog ────────────────────────────────────
            private void OpenActivityLog()
            {
                ActivityLogDialog dlg = new ActivityLogDialog(_engine.Log);
                dlg.ShowDialog(this);
            }

            private void UserSay(string message)
            {
                _chatDisplay.SelectionStart = _chatDisplay.TextLength;
                _chatDisplay.SelectionColor = Color.FromArgb(255, 200, 70);
                _chatDisplay.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold);
                _chatDisplay.AppendText($" [{_userName}] ");
                _chatDisplay.SelectionColor = Color.FromArgb(255, 228, 140);
                _chatDisplay.SelectionFont = new Font("Consolas", 10f);
                _chatDisplay.AppendText(message + "\n");
                _chatDisplay.ScrollToCaret();
            }

            private void BotSay(string message)
            {
                _chatDisplay.SelectionStart = _chatDisplay.TextLength;
                _chatDisplay.SelectionColor = Color.FromArgb(0, 220, 180);
                _chatDisplay.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold);
                _chatDisplay.AppendText(" [BOT] ");
                _chatDisplay.SelectionColor = Color.FromArgb(200, 230, 255);
                _chatDisplay.SelectionFont = new Font("Consolas", 10f);
                _chatDisplay.AppendText(message + "\n");
                _chatDisplay.ScrollToCaret();
            }

            private void TypewriterSay(string message)
            {
                _chatDisplay.SelectionStart = _chatDisplay.TextLength;
                _chatDisplay.SelectionColor = Color.FromArgb(0, 220, 180);
                _chatDisplay.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold);
                _chatDisplay.AppendText(" [BOT] ");
                _chatDisplay.SelectionColor = Color.FromArgb(200, 230, 255);
                _chatDisplay.SelectionFont = new Font("Consolas", 10f);

                foreach (char c in message)
                {
                    _chatDisplay.AppendText(c.ToString());
                    _chatDisplay.ScrollToCaret();
                    Application.DoEvents();
                    Thread.Sleep(6);
                }

                _chatDisplay.AppendText("\n");
                _chatDisplay.ScrollToCaret();
            }

            private void Separator()
            {
                _chatDisplay.SelectionStart = _chatDisplay.TextLength;
                _chatDisplay.SelectionColor = Color.FromArgb(25, 48, 65);
                _chatDisplay.SelectionFont = new Font("Consolas", 8f);
                _chatDisplay.AppendText(" " + new string('─', 84) + "\n");
            }

            private string BuildMemoryText()
            {
                UserMemory m = _engine.Memory;
                string name = string.IsNullOrEmpty(m.Name) ? "-" : m.Name;
                string fav = string.IsNullOrEmpty(m.FavouriteTopic) ? "Not set" : m.FavouriteTopic;
                string last = string.IsNullOrEmpty(m.LastTopic) ? "None yet" : m.LastTopic;
                string sentiment = string.IsNullOrEmpty(m.LastSentiment) ? "Neutral" : m.LastSentiment;

                return $" Name:       {name}\n" +
                       $" Fav Topic:  {fav}\n" +
                       $" Last Topic: {last}\n" +
                       $" Mood:       {sentiment}";
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  TASK MANAGER DIALOG  —  view, add, complete, and delete tasks
        // ══════════════════════════════════════════════════════════════════════
        class TaskManagerDialog : Form
        {
            private TaskDatabase _db;
            private ActivityLog _log;

            private ListView _listView;
            private TextBox _titleBox;
            private TextBox _descBox;
            private DateTimePicker _reminderPicker;
            private CheckBox _setReminderCheck;
            private Button _addButton;
            private Button _completeButton;
            private Button _deleteButton;
            private Button _closeButton;

            public TaskManagerDialog(TaskDatabase db, ActivityLog log)
            {
                _db = db;
                _log = log;
                BuildUI();
                RefreshList();
            }

            private void BuildUI()
            {
                Text = "My Cybersecurity Tasks";
                Size = new Size(760, 560);
                StartPosition = FormStartPosition.CenterParent;
                BackColor = Color.FromArgb(12, 16, 26);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                Font = new Font("Consolas", 9f);

                Label header = new Label
                {
                    Text = "📝  My Cybersecurity Tasks",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 14f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(20, 15)
                };
                Controls.Add(header);

                // ── Task list ────────────────────────────────────────────────────
                _listView = new ListView
                {
                    Location = new Point(20, 55),
                    Size = new Size(710, 240),
                    View = View.Details,
                    FullRowSelect = true,
                    GridLines = true,
                    BackColor = Color.FromArgb(16, 24, 40),
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 9f)
                };
                _listView.Columns.Add("Title", 220);
                _listView.Columns.Add("Description", 290);
                _listView.Columns.Add("Reminder", 100);
                _listView.Columns.Add("Status", 90);
                Controls.Add(_listView);

                // ── Action buttons for the list ────────────────────────────────────
                _completeButton = new Button
                {
                    Text = "✅ Mark Completed",
                    Location = new Point(20, 305),
                    Size = new Size(180, 34),
                    BackColor = Color.FromArgb(0, 130, 100),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                _completeButton.FlatAppearance.BorderSize = 0;
                _completeButton.Click += CompleteButton_Click;
                Controls.Add(_completeButton);

                _deleteButton = new Button
                {
                    Text = "🗑️ Delete Task",
                    Location = new Point(210, 305),
                    Size = new Size(150, 34),
                    BackColor = Color.FromArgb(110, 25, 25),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                _deleteButton.FlatAppearance.BorderSize = 0;
                _deleteButton.Click += DeleteButton_Click;
                Controls.Add(_deleteButton);

                // ── Divider ─────────────────────────────────────────────────────
                Controls.Add(new Label
                {
                    Text = new string('─', 96),
                    ForeColor = Color.FromArgb(0, 110, 90),
                    Location = new Point(20, 350),
                    AutoSize = true
                });

                // ── Add new task section ────────────────────────────────────────
                Controls.Add(new Label
                {
                    Text = "Add a New Task",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    Font = new Font("Consolas", 11f, FontStyle.Bold),
                    Location = new Point(20, 365),
                    AutoSize = true
                });

                Controls.Add(new Label { Text = "Title:", ForeColor = Color.White, Location = new Point(20, 400), AutoSize = true });
                _titleBox = new TextBox
                {
                    Location = new Point(90, 397),
                    Size = new Size(300, 26),
                    BackColor = Color.FromArgb(16, 24, 40),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                Controls.Add(_titleBox);

                Controls.Add(new Label { Text = "Description:", ForeColor = Color.White, Location = new Point(20, 435), AutoSize = true });
                _descBox = new TextBox
                {
                    Location = new Point(120, 432),
                    Size = new Size(580, 26),
                    BackColor = Color.FromArgb(16, 24, 40),
                    ForeColor = Color.White,
                    BorderStyle = BorderStyle.FixedSingle
                };
                Controls.Add(_descBox);

                _setReminderCheck = new CheckBox
                {
                    Text = "Set reminder for:",
                    ForeColor = Color.White,
                    Location = new Point(20, 470),
                    AutoSize = true
                };
                Controls.Add(_setReminderCheck);

                _reminderPicker = new DateTimePicker
                {
                    Location = new Point(170, 467),
                    Size = new Size(160, 26),
                    Format = DateTimePickerFormat.Short,
                    MinDate = DateTime.Now
                };
                Controls.Add(_reminderPicker);

                _addButton = new Button
                {
                    Text = "➕ Add Task",
                    Location = new Point(580, 463),
                    Size = new Size(150, 34),
                    BackColor = Color.FromArgb(0, 175, 135),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Consolas", 9f, FontStyle.Bold)
                };
                _addButton.FlatAppearance.BorderSize = 0;
                _addButton.Click += AddButton_Click;
                Controls.Add(_addButton);

                _closeButton = new Button
                {
                    Text = "Close",
                    Location = new Point(610, 505),
                    Size = new Size(120, 32),
                    BackColor = Color.FromArgb(40, 50, 65),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                _closeButton.FlatAppearance.BorderSize = 0;
                _closeButton.Click += (s, e) => Close();
                Controls.Add(_closeButton);
            }

            private void RefreshList()
            {
                _listView.Items.Clear();
                foreach (TaskItem t in _db.GetAllTasks())
                {
                    ListViewItem item = new ListViewItem(new[]
                    {
                        t.Title,
                        t.Description,
                        t.ReminderDate.HasValue ? t.ReminderDate.Value.ToString("dd MMM yyyy") : "-",
                        t.IsCompleted ? "Completed" : "Pending"
                    });
                    item.Tag = t.Id;
                    if (t.IsCompleted) item.ForeColor = Color.Gray;
                    _listView.Items.Add(item);
                }
            }

            private void AddButton_Click(object sender, EventArgs e)
            {
                string title = _titleBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(title))
                {
                    MessageBox.Show("Please enter a task title.", "Title Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string description = _descBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(description))
                    description = $"Complete the cybersecurity task: {title}.";

                DateTime? reminder = _setReminderCheck.Checked ? _reminderPicker.Value : (DateTime?)null;

                _db.AddTask(title, description, reminder);
                _log.Add($"Task added via Task Manager: '{title}'" +
                         (reminder.HasValue ? $" with reminder on {reminder:dd MMM yyyy}." : " (no reminder set)."));

                _titleBox.Clear();
                _descBox.Clear();
                _setReminderCheck.Checked = false;
                RefreshList();
            }

            private void CompleteButton_Click(object sender, EventArgs e)
            {
                if (_listView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a task first.", "No Task Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ListViewItem item = _listView.SelectedItems[0];
                int id = (int)item.Tag;
                string title = item.SubItems[0].Text;

                _db.CompleteTask(id);
                _log.Add($"Task marked as completed: '{title}'.");
                RefreshList();
            }

            private void DeleteButton_Click(object sender, EventArgs e)
            {
                if (_listView.SelectedItems.Count == 0)
                {
                    MessageBox.Show("Please select a task first.", "No Task Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                ListViewItem item = _listView.SelectedItems[0];
                int id = (int)item.Tag;
                string title = item.SubItems[0].Text;

                DialogResult confirm = MessageBox.Show($"Delete task \"{title}\"?", "Confirm Delete",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    _db.DeleteTask(id);
                    _log.Add($"Task deleted: '{title}'.");
                    RefreshList();
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  QUIZ DIALOG  —  one question at a time, immediate feedback, score
        // ══════════════════════════════════════════════════════════════════════
        class QuizDialog : Form
        {
            private QuizEngine _quiz;
            private ActivityLog _log;
            private bool _quizFinished = false;

            private Label _questionLabel;
            private Label _progressLabel;
            private Label _feedbackLabel;
            private FlowLayoutPanel _optionsPanel;
            private Button _nextButton;

            public QuizDialog(QuizEngine quiz, ActivityLog log)
            {
                _quiz = quiz;
                _log = log;
                _quiz.Reset();
                BuildUI();
                _log.Add("Quiz started.");
                ShowQuestion();
            }

            private void BuildUI()
            {
                Text = "Cybersecurity Quiz";
                Size = new Size(650, 480);
                StartPosition = FormStartPosition.CenterParent;
                BackColor = Color.FromArgb(12, 16, 26);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                Font = new Font("Consolas", 9f);

                Controls.Add(new Label
                {
                    Text = "🎮  Cybersecurity Knowledge Quiz",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    Font = new Font("Consolas", 14f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(20, 15)
                });

                _progressLabel = new Label
                {
                    ForeColor = Color.FromArgb(150, 200, 255),
                    Font = new Font("Consolas", 9f),
                    AutoSize = true,
                    Location = new Point(20, 55)
                };
                Controls.Add(_progressLabel);

                _questionLabel = new Label
                {
                    ForeColor = Color.White,
                    Font = new Font("Consolas", 12f, FontStyle.Bold),
                    Location = new Point(20, 90),
                    Size = new Size(590, 80)
                };
                Controls.Add(_questionLabel);

                _optionsPanel = new FlowLayoutPanel
                {
                    Location = new Point(20, 180),
                    Size = new Size(590, 180),
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false
                };
                Controls.Add(_optionsPanel);

                _feedbackLabel = new Label
                {
                    Font = new Font("Consolas", 10f, FontStyle.Bold),
                    Location = new Point(20, 370),
                    Size = new Size(590, 50)
                };
                Controls.Add(_feedbackLabel);

                _nextButton = new Button
                {
                    Text = "Next ►",
                    Location = new Point(490, 410),
                    Size = new Size(120, 34),
                    BackColor = Color.FromArgb(0, 175, 135),
                    ForeColor = Color.Black,
                    FlatStyle = FlatStyle.Flat,
                    Visible = false
                };
                _nextButton.FlatAppearance.BorderSize = 0;
                _nextButton.Click += NextButton_Click;
                Controls.Add(_nextButton);
            }

            private void ShowQuestion()
            {
                if (!_quiz.HasNext)
                {
                    ShowResults();
                    return;
                }

                QuizQuestion q = _quiz.GetCurrentQuestion();
                _questionLabel.Text = q.Question;
                _progressLabel.Text = $"Question {_quiz.CurrentIndex + 1} of {_quiz.TotalQuestions}   |   Score: {_quiz.Score}";
                _feedbackLabel.Text = "";
                _nextButton.Visible = false;
                _optionsPanel.Controls.Clear();

                for (int i = 0; i < q.Options.Length; i++)
                {
                    int idx = i;
                    Button optBtn = new Button
                    {
                        Text = q.Options[i],
                        Size = new Size(560, 38),
                        BackColor = Color.FromArgb(20, 34, 52),
                        ForeColor = Color.FromArgb(190, 220, 255),
                        FlatStyle = FlatStyle.Flat,
                        TextAlign = ContentAlignment.MiddleLeft,
                        Font = new Font("Consolas", 10f),
                        Margin = new Padding(0, 0, 0, 6)
                    };
                    optBtn.FlatAppearance.BorderSize = 1;
                    optBtn.FlatAppearance.BorderColor = Color.FromArgb(0, 110, 90);
                    optBtn.Click += (s, e) => AnswerSelected(idx, q);
                    _optionsPanel.Controls.Add(optBtn);
                }
            }

            private void AnswerSelected(int selectedIndex, QuizQuestion question)
            {
                bool correct = selectedIndex == question.CorrectIndex;
                if (correct) _quiz.AddScore();

                foreach (Control c in _optionsPanel.Controls)
                {
                    c.Enabled = false;
                }

                _feedbackLabel.ForeColor = correct ? Color.FromArgb(0, 220, 130) : Color.FromArgb(255, 110, 110);
                _feedbackLabel.Text = (correct ? "✅ Correct! " : "❌ Not quite. ") + question.Explanation;

                _quiz.Advance();
                _nextButton.Visible = true;
                _nextButton.Text = _quiz.HasNext ? "Next ►" : "See Results ►";
            }

            private void ShowResults()
            {
                int score = _quiz.Score;
                int total = _quiz.TotalQuestions;
                _log.Add($"Quiz completed with score {score}/{total}.");

                string message;
                if (score == total)
                    message = "🏆 Perfect score! You're a cybersecurity pro!";
                else if (score >= total * 0.7)
                    message = "🎉 Great job! You really know your stuff!";
                else if (score >= total * 0.4)
                    message = "👍 Good effort! Keep learning to stay even safer online.";
                else
                    message = "📚 Keep learning to stay safe online! Try again to improve your score.";

                _questionLabel.Text = $"Quiz Complete!\n\nYour Score: {score} / {total}";
                _progressLabel.Text = "";
                _optionsPanel.Controls.Clear();
                _feedbackLabel.ForeColor = Color.FromArgb(0, 220, 180);
                _feedbackLabel.Text = message;
                _nextButton.Text = "Close";
                _nextButton.Visible = true;
                _quizFinished = true;
            }

            private void NextButton_Click(object sender, EventArgs e)
            {
                if (_quizFinished)
                    Close();
                else
                    ShowQuestion();
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        //  ACTIVITY LOG DIALOG  —  view recent or full bot activity history
        // ══════════════════════════════════════════════════════════════════════
        class ActivityLogDialog : Form
        {
            private ActivityLog _log;
            private RichTextBox _display;
            private Button _toggleButton;
            private bool _showingAll = false;

            public ActivityLogDialog(ActivityLog log)
            {
                _log = log;
                BuildUI();
                RefreshDisplay();
            }

            private void BuildUI()
            {
                Text = "Activity Log";
                Size = new Size(620, 500);
                StartPosition = FormStartPosition.CenterParent;
                BackColor = Color.FromArgb(12, 16, 26);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false;
                MinimizeBox = false;
                Font = new Font("Consolas", 9f);

                Controls.Add(new Label
                {
                    Text = "📜  Activity Log",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    Font = new Font("Consolas", 14f, FontStyle.Bold),
                    AutoSize = true,
                    Location = new Point(20, 15)
                });

                _display = new RichTextBox
                {
                    Location = new Point(20, 55),
                    Size = new Size(560, 350),
                    BackColor = Color.FromArgb(16, 24, 40),
                    ForeColor = Color.FromArgb(200, 230, 255),
                    BorderStyle = BorderStyle.FixedSingle,
                    ReadOnly = true,
                    Font = new Font("Consolas", 10f)
                };
                Controls.Add(_display);

                _toggleButton = new Button
                {
                    Text = "Show Full History",
                    Location = new Point(20, 415),
                    Size = new Size(180, 32),
                    BackColor = Color.FromArgb(20, 34, 52),
                    ForeColor = Color.FromArgb(150, 220, 255),
                    FlatStyle = FlatStyle.Flat
                };
                _toggleButton.FlatAppearance.BorderSize = 0;
                _toggleButton.Click += (s, e) => { _showingAll = !_showingAll; _toggleButton.Text = _showingAll ? "Show Recent Only" : "Show Full History"; RefreshDisplay(); };
                Controls.Add(_toggleButton);

                Button closeBtn = new Button
                {
                    Text = "Close",
                    Location = new Point(460, 415),
                    Size = new Size(120, 32),
                    BackColor = Color.FromArgb(40, 50, 65),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat
                };
                closeBtn.FlatAppearance.BorderSize = 0;
                closeBtn.Click += (s, e) => Close();
                Controls.Add(closeBtn);
            }

            private void RefreshDisplay()
            {
                _display.Clear();
                List<LogEntry> entries = _showingAll ? _log.GetAll() : _log.GetRecent(8);

                if (entries.Count == 0)
                {
                    _display.Text = "No activity has been recorded yet.\n\nTry adding a task, setting a reminder, or playing the quiz!";
                    return;
                }

                int i = 1;
                foreach (LogEntry entry in entries)
                {
                    _display.SelectionColor = Color.FromArgb(0, 220, 180);
                    _display.SelectionFont = new Font("Consolas", 10f, FontStyle.Bold);
                    _display.AppendText($"{i}. ");
                    _display.SelectionColor = Color.FromArgb(200, 230, 255);
                    _display.SelectionFont = new Font("Consolas", 10f);
                    _display.AppendText($"{entry.Description}\n");
                    _display.SelectionColor = Color.FromArgb(100, 140, 170);
                    _display.SelectionFont = new Font("Consolas", 8f);
                    _display.AppendText($"    {entry.Timestamp:dd MMM yyyy, HH:mm}\n\n");
                    i++;
                }
            }
        }

    } // end class Program
}