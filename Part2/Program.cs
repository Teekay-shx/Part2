using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows.Forms;

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
        //  USER MEMORY  —  stores details the bot remembers about the user
        // ══════════════════════════════════════════════════════════════════════
        class UserMemory
        {
            public string Name { get; set; } = "";
            public string FavouriteTopic { get; set; } = "";
            public string LastTopic { get; set; } = "";
            public string LastSentiment { get; set; } = "";
        }

        // ══════════════════════════════════════════════════════════════════════
        //  CHATBOT ENGINE  —  all response logic
        // ══════════════════════════════════════════════════════════════════════
        class ChatbotEngine
        {
            private Random _rng = new Random();
            public UserMemory Memory { get; private set; } = new UserMemory();

            // ── Single responses for each keyword ─────────────────────────────
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

            // ── Multiple phishing tips — random pick each time ────────────────
            private List<string> _phishingTips = new List<string>
            {
                "Be cautious of emails asking for personal information. Scammers often disguise themselves as trusted organisations.",
                "Check the sender address carefully — phishers use addresses that look almost right, like 'support@paypa1.com'.",
                "Hover over links before clicking to see the real destination. If the URL looks odd, do not click it.",
                "Phishing messages create urgency — 'Your account will be closed!' is a classic trick to rush you into a mistake.",
                "A legitimate company will never ask for your password via email. Such a request is almost certainly phishing.",
                "Always type a website address directly into your browser rather than following links from emails."
            };

            // ── Multiple password tips — random pick each time ────────────────
            private List<string> _passwordTips = new List<string>
            {
                "Use a passphrase — something like 'BlueSky$Lamp42!' is long and much easier to remember than a random jumble.",
                "Never reuse passwords across websites. If one site is breached, every account sharing that password is at risk.",
                "Consider using a reputable password manager to create and store unique, complex passwords for every account.",
                "Change a password immediately if you suspect that account has been compromised.",
                "Avoid obvious passwords like '123456', 'password' or your own name — these are the very first things attackers try."
            };

            // ── Sentiment keyword to empathetic opener ────────────────────────
            private Dictionary<string, string> _sentimentResponses =
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "worried",
                  "It is completely understandable to feel worried — cyber threats are real. But knowing about them is already your biggest defence. Let me share something helpful." },

                { "scared",
                  "There is no need to panic. You are already taking the right step by learning! Let me share something reassuring." },

                { "frustrated",
                  "I understand this can feel overwhelming. Let us take it one step at a time. Here is something straightforward to help." },

                { "confused",
                  "No problem at all — cybersecurity can be complex. Let me explain things more clearly for you." },

                { "curious",
                  "Curiosity is the best starting point! You are already on the right track. Here is something interesting." },

                { "happy",
                  "Great to hear! Staying positive while learning makes you even more prepared. Here is a tip to keep the momentum going." },

                { "overwhelmed",
                  "Take a breath — you do not have to learn everything at once. Let us focus on one important thing at a time." },

                { "anxious",
                  "It is natural to feel anxious about online threats. The good news is that a few simple habits can protect you significantly. Here is where to start." },

                { "unsure",
                  "Being unsure is perfectly fine — it means you are thinking carefully. Let me help clarify things." },
            };

            // ── Phrases that trigger a follow-up on the last topic ────────────
            private List<string> _followUpPhrases = new List<string>
            {
                "tell me more", "explain more", "more details", "give me another",
                "another tip", "go on", "continue", "more info", "elaborate", "expand on that"
            };

            // ── General tips used as fallback ─────────────────────────────────
            private List<string> _generalTips = new List<string>
            {
                "Keep your software and operating system updated — many attacks exploit outdated software.",
                "Enable two-factor authentication wherever you can. It dramatically reduces the chance of an account being hijacked.",
                "Be careful about what personal information you share on social media. Attackers piece together details to target you.",
                "Back up your data regularly so ransomware cannot hold you hostage.",
                "When in doubt, do not click! Suspicious links and attachments are one of the most common ways malware spreads."
            };

            // ─────────────────────────────────────────────────────────────────
            //  PUBLIC — returns the bot response for any user input
            // ─────────────────────────────────────────────────────────────────
            public string GetResponse(string userInput)
            {
                if (string.IsNullOrWhiteSpace(userInput))
                    return "Please type something so I can help you! 😊";

                string input = userInput.Trim().ToLower();

                // 1. Check for follow-up phrases first
                foreach (string phrase in _followUpPhrases)
                {
                    if (input.Contains(phrase))
                        return HandleFollowUp();
                }

                // 2. Check for memory input ("I am interested in X")
                string memResponse = HandleMemoryInput(input);
                if (!string.IsNullOrEmpty(memResponse))
                    return memResponse;

                // 3. Detect sentiment
                string sentiment = DetectSentiment(input);

                // 4. Detect topic
                string topic = DetectTopic(input);

                // 5. Combine, or use whichever was found, or fallback
                if (!string.IsNullOrEmpty(sentiment) && !string.IsNullOrEmpty(topic))
                    return sentiment + "\n\n" + topic;

                if (!string.IsNullOrEmpty(sentiment) && string.IsNullOrEmpty(topic))
                    return sentiment + "\n\n" + _generalTips[_rng.Next(_generalTips.Count)];

                if (!string.IsNullOrEmpty(topic))
                    return topic;

                // 6. Default fallback for unrecognised input
                return "I am not sure I understand. Could you try rephrasing? " +
                       "You can ask me about phishing, passwords, scams, privacy, malware, VPNs, 2FA and more! 🔐";
            }

            // ── Detect sentiment, store in memory and return opener ────────────
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

            // ── Detect a cybersecurity topic and build a response ─────────────
            private string DetectTopic(string input)
            {
                // Phishing — random tip
                if (input.Contains("phishing") || input.Contains("phish"))
                {
                    Memory.LastTopic = "phishing";
                    CheckFavourite("phishing", input);
                    return "🎣 Phishing Tip:\n" + _phishingTips[_rng.Next(_phishingTips.Count)];
                }

                // Password — random tip
                if (input.Contains("password"))
                {
                    Memory.LastTopic = "password";
                    CheckFavourite("password", input);
                    return "🔑 Password Tip:\n" + _passwordTips[_rng.Next(_passwordTips.Count)];
                }

                // All other keywords
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

            // ── Continue on the last topic discussed ──────────────────────────
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

            // ── Handle "I am interested in X" — store favourite topic ─────────
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

            // ── If the user phrases suggest a favourite, store it ─────────────
            private void CheckFavourite(string topic, string input)
            {
                if (input.Contains("favourite") || input.Contains("favorite") ||
                    input.Contains("love") || input.Contains("most interested"))
                {
                    Memory.FavouriteTopic = topic;
                }
            }

            // ── Store the user's name in memory ───────────────────────────────
            public void SetUserName(string name)
            {
                Memory.Name = name;
            }

            // ── Play WAV greeting (carried over from Part 1) ──────────────────
            public static void PlayGreeting()
            {
                try
                {
                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "chat bot.wav");
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
                // ── Form properties ────────────────────────────────────────────
                Text = "CyberSecurity Awareness Bot — Part 2";
                Size = new Size(920, 600);
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(10, 12, 20);
                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;
                Font = new Font("Consolas", 9f);

                // ── Top divider ────────────────────────────────────────────────
                Controls.Add(MakeDivider(12));

                // ── ASCII art from Part 1 ──────────────────────────────────────
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

                // ── Middle divider ─────────────────────────────────────────────
                Controls.Add(MakeDivider(245));

                // ── Prompt labels ──────────────────────────────────────────────
                Controls.Add(MakeLabel(
                    " Activate me by providing your name...",
                    new Point(15, 268),
                    Color.FromArgb(160, 210, 255), 10f));

                Controls.Add(MakeLabel(
                    " What should I call you?",
                    new Point(15, 298),
                    Color.FromArgb(0, 220, 180), 11f, FontStyle.Bold));

                // ── Name input box ─────────────────────────────────────────────
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
                _nameBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter) ActivateBot();
                };
                Controls.Add(_nameBox);

                // ── Start button ───────────────────────────────────────────────
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

                // ── Bottom divider ─────────────────────────────────────────────
                Controls.Add(MakeDivider(395));

                // ── Hint ───────────────────────────────────────────────────────
                Controls.Add(MakeLabel(
                    " 💡 Topics: Phishing  |  Passwords  |  Scams  |  Privacy  |  Malware  |  VPN  |  2FA  |  Wi-Fi",
                    new Point(15, 415),
                    Color.FromArgb(70, 130, 170), 8f));

                AcceptButton = _startButton;
            }

            // ── Open the chat window with the entered name ─────────────────────
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

            // ── Helper: dashed divider line ────────────────────────────────────
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

            // ── Helper: generic label ──────────────────────────────────────────
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

            // Controls
            private RichTextBox _chatDisplay;
            private TextBox _inputBox;
            private Button _sendButton;
            private Button _clearButton;
            private Label _memoryLabel;
            private ListBox _topicList;

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
                // ── Form ──────────────────────────────────────────────────────
                Text = "Part 2 — CyberSecurity Awareness Bot";
                Size = new Size(1100, 720);
                StartPosition = FormStartPosition.CenterScreen;
                BackColor = Color.FromArgb(10, 12, 20);
                FormBorderStyle = FormBorderStyle.Sizable;
                MinimumSize = new Size(820, 580);
                Font = new Font("Consolas", 9f);

                // ═══════════════════════════════════════════════════════════════
                //  TOP PANEL
                // ═══════════════════════════════════════════════════════════════
                Panel topPanel = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 54,
                    BackColor = Color.FromArgb(13, 19, 32)
                };

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
                    Text = "  ● Online  |  Ready to help",
                    ForeColor = Color.FromArgb(0, 200, 100),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f),
                    AutoSize = true,
                    Location = new Point(8, 34)
                });

                Controls.Add(topPanel);

                // ═══════════════════════════════════════════════════════════════
                //  RIGHT SIDE PANEL — quick topics + memory display
                // ═══════════════════════════════════════════════════════════════
                Panel sidePanel = new Panel
                {
                    Dock = DockStyle.Right,
                    Width = 222,
                    BackColor = Color.FromArgb(13, 19, 32),
                    Padding = new Padding(6)
                };

                sidePanel.Controls.Add(new Label
                {
                    Text = "── Quick Topics ──",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = 26,
                    TextAlign = ContentAlignment.MiddleCenter
                });

                _topicList = new ListBox
                {
                    Dock = DockStyle.Top,
                    Height = 236,
                    BackColor = Color.FromArgb(16, 24, 40),
                    ForeColor = Color.FromArgb(155, 205, 255),
                    BorderStyle = BorderStyle.None,
                    Font = new Font("Consolas", 9f)
                };
                _topicList.Items.AddRange(new string[]
                {
                    "🎣  Phishing",
                    "🔑  Passwords",
                    "⚠️   Scams",
                    "🕵️   Privacy",
                    "🦠  Malware",
                    "🌐  VPN",
                    "🔐  Two-Factor Auth",
                    "📧  Email Security",
                    "📡  Public Wi-Fi",
                    "🛡️   Ransomware",
                    "🔥  Firewall",
                    "🌍  Safe Browsing"
                });
                _topicList.Click += TopicList_Click;
                sidePanel.Controls.Add(_topicList);

                sidePanel.Controls.Add(new Label
                {
                    Text = " Click a topic to learn about it",
                    ForeColor = Color.FromArgb(60, 100, 140),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 7f),
                    Dock = DockStyle.Top,
                    Height = 18,
                    TextAlign = ContentAlignment.MiddleCenter
                });

                sidePanel.Controls.Add(new Label
                {
                    Text = "── Bot Memory ──",
                    ForeColor = Color.FromArgb(0, 220, 180),
                    BackColor = Color.Transparent,
                    Font = new Font("Consolas", 8f, FontStyle.Bold),
                    Dock = DockStyle.Top,
                    Height = 26,
                    TextAlign = ContentAlignment.MiddleCenter
                });

                _memoryLabel = new Label
                {
                    Text = BuildMemoryText(),
                    ForeColor = Color.FromArgb(155, 200, 240),
                    BackColor = Color.FromArgb(16, 24, 40),
                    Font = new Font("Consolas", 7.5f),
                    Dock = DockStyle.Top,
                    Height = 94,
                    Padding = new Padding(6)
                };
                sidePanel.Controls.Add(_memoryLabel);

                Controls.Add(sidePanel);

                // ═══════════════════════════════════════════════════════════════
                //  BOTTOM PANEL — input row
                // ═══════════════════════════════════════════════════════════════
                Panel bottomPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = 64,
                    BackColor = Color.FromArgb(13, 19, 32)
                };

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
                    Size = new Size(680, 32),
                    Font = new Font("Consolas", 11f),
                    BackColor = Color.FromArgb(16, 26, 42),
                    ForeColor = Color.FromArgb(200, 230, 255),
                    BorderStyle = BorderStyle.FixedSingle,
                    PlaceholderText = "Type here... e.g. 'Tell me about phishing' or 'give me another tip'"
                };
                _inputBox.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        e.SuppressKeyPress = true;
                        Send();
                    }
                };
                bottomPanel.Controls.Add(_inputBox);

                _sendButton = new Button
                {
                    Text = "SEND ►",
                    Location = new Point(782, 15),
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
                    Location = new Point(900, 15),
                    Size = new Size(88, 34),
                    Font = new Font("Consolas", 9f, FontStyle.Bold),
                    BackColor = Color.FromArgb(55, 14, 14),
                    ForeColor = Color.FromArgb(255, 100, 100),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand
                };
                _clearButton.FlatAppearance.BorderColor = Color.FromArgb(100, 28, 28);
                _clearButton.Click += (s, e) =>
                {
                    _chatDisplay.Clear();
                    ShowWelcomeMessage();
                };
                bottomPanel.Controls.Add(_clearButton);

                Controls.Add(bottomPanel);

                // ═══════════════════════════════════════════════════════════════
                //  CHAT DISPLAY — fills the remaining centre space
                // ═══════════════════════════════════════════════════════════════
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

                // Adjust input box width when form is resized
                Resize += (s, e) =>
                {
                    if (_inputBox != null)
                        _inputBox.Width = Math.Max(200, ClientSize.Width - 340);
                };
            }

            // ── Show welcome message when chat window opens ────────────────────
            private void ShowWelcomeMessage()
            {
                Separator();
                BotSay($" Hello {_userName}! 👋 Welcome to the CyberSecurity Awareness Bot.");
                BotSay(" Here is what you can do:");
                BotSay("  • Ask me about any cybersecurity topic — phishing, passwords, scams, VPNs, malware, and more.");
                BotSay("  • Click a topic from the Quick Topics panel on the right.");
                BotSay("  • Type 'give me another tip' or 'tell me more' to continue on a topic.");
                BotSay("  • Tell me how you feel — e.g. 'I am worried about scams' — and I will adjust my response.");
                BotSay("  • Type 'quit' or 'bye' to exit.");
                Separator();
            }

            // ── Handle a click on the quick topic list ─────────────────────────
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
                    { "📧  Email Security", "Tell me about email security" },
                    { "📡  Public Wi-Fi",   "Tell me about public wi-fi" },
                    { "🛡️   Ransomware",     "Tell me about ransomware" },
                    { "🔥  Firewall",       "Tell me about firewall" },
                    { "🌍  Safe Browsing",  "Tell me about safe browsing" }
                };

                string key = _topicList.SelectedItem.ToString();
                if (map.ContainsKey(key))
                    ProcessInput(map[key]);

                _topicList.ClearSelected();
            }

            // ── Read the input box and send ────────────────────────────────────
            private void Send()
            {
                string text = _inputBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(text)) return;
                _inputBox.Clear();
                ProcessInput(text);
            }

            // ── Display the user message then get and display the bot response ─
            private void ProcessInput(string input)
            {
                // Handle exit commands
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
            }

            // ── Append a user message to the chat display ──────────────────────
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

            // ── Append a bot message to the chat display instantly ─────────────
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

            // ── Append a bot message with a typewriter character-by-character effect
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
                    Thread.Sleep(7);
                }

                _chatDisplay.AppendText("\n");
                _chatDisplay.ScrollToCaret();
            }

            // ── Draw a thin separator line in the chat ─────────────────────────
            private void Separator()
            {
                _chatDisplay.SelectionStart = _chatDisplay.TextLength;
                _chatDisplay.SelectionColor = Color.FromArgb(25, 48, 65);
                _chatDisplay.SelectionFont = new Font("Consolas", 8f);
                _chatDisplay.AppendText(" " + new string('─', 84) + "\n");
            }

            // ── Build the text shown inside the memory panel ───────────────────
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

    } // end class Program
}
