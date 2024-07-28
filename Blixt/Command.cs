using System.Collections.Concurrent;
using System.Diagnostics;
using Spectre.Console;

namespace Blixt{
    public class Command(string name, int attributes = 0){
        public readonly string Name = name;
        public readonly int Attributes = attributes;
        public string Input = null!;

        public virtual bool Run(){
            return false;
        }
    }

    public class Drive(string name, int attributes = 0) : Command(name, attributes){
        public override bool Run(){
            if (Attributes == 0){
                Program.CurrentDirectory = Program.ChooseDrive().RootDirectory;
                return true;
            }

            return false;
        }
    }

    public class Explore(string name, int attributes = 0) : Command(name, attributes){
        public override bool Run(){
            Process.Start("explorer.exe", Program.CurrentDirectory.FullName);
            return true;
        }
    }

    public class Rise(string name, int attributes = 0) : Command(name, attributes){
        public override bool Run(){
            if (attributes == 0){
                if (Program.CurrentDirectory.Parent == null) return true;

                Program.CurrentDirectory = Program.CurrentDirectory.Parent;
            }
            else{
                string attribute = Input.Split()[1];
                if (!int.TryParse(attribute, out int amount)) return false;
                for (int i = 0; i < amount; i++){
                    if (Program.CurrentDirectory.Parent == null) return true;
                    Program.CurrentDirectory = Program.CurrentDirectory.Parent;
                }
            }

            return true;
        }
    }
    
    public class Big(string name, int attributes = 0) : Command(name, attributes){
        public override bool Run(){
            DateTime startTime = DateTime.Now;

            string attribute = Input.Split()[1];
            if (!int.TryParse(attribute, out int count)) return false;

            AnsiConsole.MarkupLine($"Starting search...");
            ConcurrentBag<FileInfo> bag =[];
            TargetSearch(bag, Program.CurrentDirectory);
            FileInfo[] files = bag.ToArray().OrderBy(x => x.Length).ToArray();
            
            //After finding files
            AnsiConsole.Clear();

            string[] options = new string[Math.Min(files.Length, count) + 1];
            for (int i = 0; i < Math.Min(files.Length, count); i++){
                FileInfo file = files[i];
                string sizePrompt = Tools.FormatBytes(files.Length);
                options[i] = $"[grey]({sizePrompt})[/][green]{file.Name}[/]";
            }

            options[^1] = $"[red]Exit[/]";

            SelectionPrompt<string> prompt = new();
            string timePrompt = Tools.FormatTime(DateTime.Now.Subtract(startTime));
            prompt.Title($"Found [green]{files.Length}[/] files in [green]{timePrompt}[/].");
            prompt.PageSize(10);
            prompt.AddChoices(options);

            string choice = AnsiConsole.Prompt(prompt);
            int choiceIndex = -1;
            for (int i = 0; i < options.Length - 1; i++){
                if (options[i] == choice)
                    choiceIndex = i;
            }

            if (choiceIndex != -1 && files[choiceIndex].Directory != null){
                Program.CurrentDirectory = files[choiceIndex].Directory;
            }

            return true;
        }

        private static void TargetSearch(ConcurrentBag<FileInfo> bag, DirectoryInfo directory){
            Parallel.ForEach(directory.GetDirectories(),
                childDirectory => {
                    try{
                        TargetSearch(bag, childDirectory);
                    }
                    catch (Exception e){
                        // ignored
                    }
                });

            foreach (FileInfo file in directory.GetFiles()){
                bag.Add(file);
            }
        }
    }

    public class Find(string name, int attributes = 0) : Command(name, attributes){
        public override bool Run(){
            DateTime startTime = DateTime.Now;

            string attribute = Input.Split()[1];
            int count = 10;
            if (attributes == 2){
                _ = int.TryParse(Input.Split()[2], out count);
            }

            List<FileInfo> files =[];


            AnsiConsole.MarkupLine($"Starting search...");

            //Wild card
            if (attribute[0] == '*'){
                string wildCard = attribute.Remove(0, 1);
                SearchPrompt searchPrompt = new(){
                    Target = wildCard,
                    Extension = true,
                    Fuzzy = false
                };
                TargetSearch(searchPrompt, files, Program.CurrentDirectory);
            }
            else{
                ConcurrentBag<FileInfo> bag =[];
                TargetSearch(bag, Program.CurrentDirectory);

                List<Word> fuzzyResult = FuzzySearch.ParallelSearch(attribute, bag.ToArray());

                files.Clear();
                foreach (Word word in fuzzyResult){
                    files.Add(word.File);
                }
            }

            //After finding files
            AnsiConsole.Clear();

            string[] options = new string[Math.Min(files.Count, count) + 1];
            for (int i = 0; i < Math.Min(files.Count, count); i++){
                FileInfo file = files[i];
                options[i] = $"[green]{file.Name}[/] [gray]({file.FullName})[/]";
            }

            options[^1] = $"[red]Exit[/]";

            SelectionPrompt<string> prompt = new();
            string timePrompt = Tools.FormatTime(DateTime.Now.Subtract(startTime));
            prompt.Title($"Found [green]{files.Count}[/] files in [green]{timePrompt}[/]. [grey]({attribute})[/]");
            prompt.PageSize(10);
            prompt.AddChoices(options);

            string choice = AnsiConsole.Prompt(prompt);
            int choiceIndex = -1;
            for (int i = 0; i < options.Length - 1; i++){
                if (options[i] == choice)
                    choiceIndex = i;
            }

            if (choiceIndex != -1 && files[choiceIndex].Directory != null){
                Program.CurrentDirectory = files[choiceIndex].Directory;
            }

            return true;
        }

        private struct SearchPrompt{
            public string Target;
            public bool Fuzzy;
            public bool Extension;
        }

        private static void TargetSearch(SearchPrompt targetSearch, List<FileInfo> files, DirectoryInfo directory){
            foreach (DirectoryInfo childDirectory in directory.GetDirectories()){
                TargetSearch(targetSearch, files, childDirectory);
            }

            foreach (FileInfo file in directory.GetFiles()){
                if (targetSearch is{ Extension: true, Fuzzy: false } && targetSearch.Target == file.Extension){
                    files.Add(file);
                }
            }
        }

        private static void TargetSearch(List<FileInfo> files, DirectoryInfo directory, int parallel = 4){
            if (parallel != 0){
                Parallel.ForEach(directory.GetDirectories(),
                    childDirectory => { TargetSearch(files, childDirectory, parallel - 1); });
            }
            else{
                foreach (DirectoryInfo childDirectory in directory.GetDirectories()){
                    TargetSearch(files, childDirectory, 0);
                }
            }

            lock (files){
                foreach (FileInfo file in directory.GetFiles()){
                    files.Add(file);
                }
            }
        }

        private static void TargetSearch(ConcurrentBag<FileInfo> bag, DirectoryInfo directory){
            Parallel.ForEach(directory.GetDirectories(),
                childDirectory => {
                    try{
                        TargetSearch(bag, childDirectory);
                    }
                    catch (Exception e){
                        // ignored
                    }
                });

            foreach (FileInfo file in directory.GetFiles()){
                bag.Add(file);
            }
        }
    }

    public class Dive(string name, int attributes = 0) : Command(name, attributes){
        public override bool Run(){
            string attribute = Input.Split()[1];

            DirectoryInfo[] directories = Program.CurrentDirectory.GetDirectories();
            FileInfo[] files = Program.CurrentDirectory.GetFiles();
            string[] words = new string[directories.Length + files.Length];
            int runningIndex = 0;
            foreach (DirectoryInfo directory in directories){
                if (directory.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                words[runningIndex] = directory.Name;
                runningIndex++;
            }

            foreach (FileInfo file in files){
                if (file.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                string name = string.IsNullOrEmpty(file.Extension) ? file.Name : file.Name.Replace(file.Extension, "");
                words[runningIndex] = name;
                runningIndex++;
            }

            List<Word> fuzzyResults = FuzzySearch.Search(attribute, words);

            if (fuzzyResults.Count != 0){
                string target = fuzzyResults[0].Value;
                foreach (DirectoryInfo directory in directories){
                    if (directory.Name != target) continue;

                    Program.CurrentDirectory = directory;
                    return true;
                }

                foreach (FileInfo file in files){
                    string name = string.IsNullOrEmpty(file.Extension)
                        ? file.Name
                        : file.Name.Replace(file.Extension, "");
                    if (name != target) continue;

                    AnsiConsole.WriteLine($"Starting [darkviolet]{file.Name}[/]");
                    Process.Start("explorer.exe", file.FullName);
                }
            }

            if (!int.TryParse(attribute, out int index)) return false;

            runningIndex = 1;
            foreach (DirectoryInfo directory in directories){
                if (directory.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                if (index == runningIndex){
                    Program.CurrentDirectory = directory;
                    return true;
                }

                runningIndex++;
            }

            foreach (FileInfo file in files){
                if (file.Attributes.HasFlag(FileAttributes.Hidden)) continue;
                if (index == runningIndex){
                    return true;
                }

                runningIndex++;
            }

            return false;
        }
    }
}