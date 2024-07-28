using System.Collections.Concurrent;

namespace Blixt{
    public struct Word{
        //0-1
        public double Score;

        //The word
        public string Value;

        public FileInfo File;
    }

    public static class FuzzySearch{
        public static List<Word> Search(string input, string[] words){
            List<Word> fuzzyList =[];

            input = input.ToLower().Trim();
            foreach (string word in words){
                if (string.IsNullOrEmpty(word)) continue;
                string processedWord = word.ToLower().Trim();

                if (input == processedWord){
                    fuzzyList.Add(new Word{ Score = 1.0, Value = word });
                    continue;
                }

                double score = JaroWinklerDistance(input, processedWord);
                if (score > 0.5){
                    fuzzyList.Add(new Word{ Score = score, Value = word });
                }
            }

            return fuzzyList.OrderByDescending(x => x.Score).ToList();
        }

        public static List<Word> Search(string input, FileInfo[] files){
            List<Word> fuzzyList =[];

            input = input.ToLower().Trim();
            foreach (FileInfo file in files){
                string processedWord = file.Name.ToLower().Trim();

                if (input == processedWord){
                    fuzzyList.Add(new Word{ Score = 1.0, Value = file.Name, File = file });
                    continue;
                }

                double score = JaroWinklerDistance(input, processedWord);
                if (score > 0.5){
                    fuzzyList.Add(new Word{ Score = score, Value = file.Name, File = file });
                }
            }

            return fuzzyList.OrderByDescending(x => x.Score).ToList();
        }

        public static List<Word> ParallelSearch(string input, FileInfo[] files){
            ConcurrentBag<Word> fuzzyBag =[];

            input = input.ToLower().Trim();
            Parallel.ForEach(files, file => {
                string processedWord = file.Name.ToLower().Trim();

                if (input == processedWord){
                    fuzzyBag.Add(new Word{ Score = 1.0, Value = file.Name, File = file });
                }
                else{
                    double score = JaroWinklerDistance(input, processedWord);
                    if (score > 0.5){
                        fuzzyBag.Add(new Word{ Score = score, Value = file.Name, File = file });
                    }
                }
            });

            return fuzzyBag.OrderByDescending(x => x.Score).ToList();
        }

        public static double JaroWinklerDistance(string s1, string s2){
            int s1Len = s1.Length;
            int s2Len = s2.Length;

            if (s1Len == 0) return s2Len == 0 ? 1.0 : 0.0;

            int matchDistance = Math.Max(s1Len, s2Len) / 2 - 1;

            bool[] s1Matches = new bool[s1Len];
            bool[] s2Matches = new bool[s2Len];

            int matches = 0;
            int transpositions = 0;

            for (int i = 0; i < s1Len; i++){
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, s2Len);

                for (int j = start; j < end; j++){
                    if (s2Matches[j]) continue;
                    if (s1[i] != s2[j]) continue;
                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            int k = 0;
            for (int i = 0; i < s1Len; i++){
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) transpositions++;
                k++;
            }

            double m = matches;
            double jaro = ((m / s1Len) + (m / s2Len) + ((m - transpositions / 2.0) / m)) / 3.0;
            double p = 0.1; // scaling factor
            int l = 0; // length of common prefix at the start of the string up to a maximum of 4 characters

            for (int i = 0; i < Math.Min(4, Math.Min(s1Len, s2Len)); i++){
                if (s1[i] == s2[i]) l++;
                else break;
            }

            return jaro + l * p * (1 - jaro);
        }
    }
}