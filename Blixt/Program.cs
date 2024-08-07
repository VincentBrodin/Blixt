using System.Text;
using Spectre.Console;

namespace Blixt{
    internal static class Program{
        public static DirectoryInfo CurrentDirectory = null!;

        private static readonly Command[] Commands =[
            new Drive("drive"),
            new Dive("dive", 1),
            new Rise("rise"),
            new Rise("rise", 1),
            new Find("find", 1),
            new Find("find", 2),
            new Big("big", 1),
            new Explore("explore"),
        ];

        public static void Main(){
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine(Console.WindowHeight);
            Console.WindowHeight -= 10;

            Rule title = new("Blixt :cloud_with_lightning:");
            AnsiConsole.Write(title);

            CurrentDirectory = ChooseDrive().RootDirectory;

            while (true){
                AnsiConsole.Clear();
                AnsiConsole.WriteLine();

                AnsiConsole.Write(title);
                AnsiConsole.WriteLine(CurrentDirectory.FullName);
                AnsiConsole.Write(RenderDirectory(CurrentDirectory));
                AwaitCommand().Run();

                AnsiConsole.WriteLine();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        //====Renderer====
        private static Layout RenderDirectory(DirectoryInfo directory){
            Layout layout = new("Root");
            layout.Size = 2;
            layout.SplitColumns(new Layout("Left"), new Layout("Right"));

            int runningIndex = 1;

            Tree tree = new(":file_folder:");
            foreach (DirectoryInfo childDirectory in directory.GetDirectories()){
                if (childDirectory.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                string node = $"({runningIndex}) [aquamarine1]{childDirectory.Name}[/]";
                tree.AddNode(node);
                runningIndex++;
            }

            layout["Left"].Update(tree);

            tree = new Tree(":card_file_box:");
            foreach (FileInfo childFile in directory.GetFiles()){
                if (childFile.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                string node = $"({runningIndex}) [darkviolet]{childFile.Name}[/] [grey]({Tools.FormatBytes(childFile.Length)})[/]";
                tree.AddNode(node);
                runningIndex++;
            }

            layout["Right"].Update(tree);

            return layout;
        }

        //====Commands====
        private static Command AwaitCommand(){
            TextPrompt<string> prompt = new("Enter command:");
            prompt.ValidationErrorMessage("[red]Not valid[/]");
            prompt.Validate(ValidateCommand);
            return ParseCommand(AnsiConsole.Prompt(prompt));
        }

        private static Command ParseCommand(string input){
            string[] commands = input.Split();

            foreach (Command command in Commands){
                if (command.Name == commands[0] && command.Attributes == commands.Length - 1){
                    command.Input = input;
                    return command;
                }
            }

            return new Command("null");
        }

        private static ValidationResult ValidateCommand(string input){
            string[] commands = input.Split();
            foreach (Command command in Commands){
                if (command.Name == commands[0] && command.Attributes == commands.Length - 1){
                    return ValidationResult.Success();
                }
            }

            return ValidationResult.Error("[red]Not a valid command[/]");
        }

        //====Drives====
        public static DriveInfo ChooseDrive(){
            AnsiConsole.Clear();
            
            AnsiConsole.WriteLine();
            
            DriveInfo[] drives = DriveInfo.GetDrives();
            string[] options = new string[drives.Length];
            for (int i = 0; i < drives.Length; i++){
                DriveInfo drive = drives[i];
                try{
                    long totalSize = drive.TotalSize;
                    long freeSpace = drive.AvailableFreeSpace;
                    long usedSpace = totalSize - freeSpace;

                    string space = $"[grey]({Tools.FormatBytes(usedSpace)}/{Tools.FormatBytes(totalSize)})[/]";
                    options[i] = $"[grey]{drive.Name}[/] {drive.VolumeLabel} {space}";
                }
                catch (AccessViolationException exception){
                    options[i] += $"[red]{drive.Name} ({exception.Message})[/]";
                }
            }

            SelectionPrompt<string> prompt = new();
            prompt.Title(":floppy_disk: Choose a drive");
            prompt.PageSize(10);
            prompt.AddChoices(options);

            string choice = AnsiConsole.Prompt(prompt);
            int choiceIndex = -1;
            for (int i = 0; i < options.Length; i++){
                if (options[i] == choice) choiceIndex = i;
            }

            return drives[choiceIndex];
        }
    }
}