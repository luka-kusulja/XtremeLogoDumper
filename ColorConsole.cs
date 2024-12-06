namespace XtremeLogoDumper
{
    internal static class ColorConsole
    {
        public static void Write(string data, bool writeLine = false)
        {
            if(data.IndexOf("~|")  == -1)
            {
                Console.WriteLine(data);
                return;
            }

            var split = data.Split("~|");

            foreach(var currentPart in split)
            {
                if(string.IsNullOrWhiteSpace(currentPart))
                {
                    continue;
                }

                if(currentPart.IndexOf("|") == -1)
                {
                    Console.Write(currentPart);
                    continue;
                }

                var color = currentPart.Substring(0, currentPart.IndexOf("|"));

                if(color == "Reset")
                {
                    Console.ResetColor();
                }
                else
                {
                    var enumColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), color);
                    Console.ForegroundColor = enumColor;
                }

                Console.Write(currentPart.Substring(currentPart.IndexOf("|") + 1));
            }

            Console.ResetColor();
            if (writeLine == true)
            {
                Console.WriteLine(); 
            }
        }

        public static void WriteLine(string data)
        {
            Write(data, true);
        }
    }
}
