namespace Blixt{
    public static class Tools{
        public static string FormatBytes(long bytes, bool round = true, bool showSuffix = true){
            string[] suffix =["B", "KB", "MB", "GB", "TB"];
            int i;
            double dblSByte = bytes;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024){
                dblSByte = bytes / 1024.0;
            }

            if (round) dblSByte = Math.Round(dblSByte);

            return showSuffix ? $"{dblSByte:0.##}{suffix[i]}" : $"{dblSByte:0.##}";
        }
        
        public static string FormatTime(TimeSpan time){
            double milliseconds = time.Milliseconds;
            double seconds = time.Seconds;
            double minutes = time.Minutes;

            string s = "";

            if (minutes != 0)
                s += $"{minutes}m ";

            if (seconds != 0)
                s += $"{seconds}s ";

            s += $"{milliseconds}ms";

            return s;
        }
    }
}