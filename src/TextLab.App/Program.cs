using Liss.TextLab.App;

ApplicationConfiguration.Initialize();
Application.SetDefaultFont(Theme.Font());
Application.ThreadException += (_, e) => MessageBox.Show(e.Exception.Message, "Text Lab", MessageBoxButtons.OK, MessageBoxIcon.Warning);
Application.Run(new MainForm(args));
