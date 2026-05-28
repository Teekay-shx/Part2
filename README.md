Part 2 — CyberSecurity Awareness Chatbot
A Windows Forms chatbot that helps users learn about cybersecurity.
Built with C# and .NET 8.0 — all code is written in a single Program.cs file.

Requirements

Windows 10 or 11
Visual Studio 2022
.NET 8.0 SDK


How to Set Up

Open Visual Studio and create a new Windows Forms App project
Name it Part2 and select .NET 8.0 as the framework
Delete the default Form1.cs, Form1.Designer.cs and Program.cs
Add the provided Program.cs and Part2.csproj into the project
Press F5 to run


Adding the Audio File
Place your chat bot.wav file in the following folder after building:
bin\Debug\net8.0-windows\
To find this folder — right-click the project in Solution Explorer → Open Folder in File Explorer, then navigate into bin → Debug → net8.0-windows.

What It Does

Greets the user by name and remembers it throughout the conversation
Answers questions about 15+ cybersecurity topics such as phishing, passwords, scams, malware, VPNs, and more
Gives random tips for phishing and password topics so responses stay varied
Detects follow-up phrases like "tell me more" or "give me another tip" and continues the last topic
Detects the user's mood from words like "worried" or "confused" and adjusts its response
Remembers the user's favourite topic and refers back to it later
Includes a Quick Topics panel on the right to click instead of typing
Shows a live Memory panel displaying what the bot currently knows about the user


How to Use It

What you type                              What happens 

Tell me about phishing                     Get a phishing tip
Give me another tip                        Get another tip on the same topic
I am worried about scams                   Bot detects your mood and responds with encouragement
I am interested in privacy                 Bot remembers this as your favourite topic
quit or bye                                Closes the chat
