using System;
using System.Collections.Generic;
using System.Linq;

namespace DiscordTimeBot
{

    public class Message
    {
        #region Public Properties

        public List<string> Arguments { get; }
        public string Command { get; }
        public string OriginalMessage { get; }
        public string OriginalArguments => OriginalMessage.Replace(Command + " ", "");

        #endregion

        #region Constructors

        public Message(string input)
        {
            OriginalMessage = input;

            List<string> sections = GetDetails();

            if (sections.Count < 1)
                throw new ArgumentException(nameof(input));

            Command = sections[0].ToLowerInvariant();

            // Check if section has arguments
            if (sections.Count > 1)
                Arguments = sections.Skip(1).ToList();

        }

        #endregion

        #region Private Methods

        private List<string> GetDetails()
        {
            List<string> sections = new List<string>();

            var inQuotes = false;
            string curSection = string.Empty;

            foreach (char c in OriginalMessage)
                switch (c)
                {
                    case ' ':

                        if (!inQuotes)
                        {
                            sections.Add(curSection);
                            curSection = string.Empty;
                        }
                        else
                        {
                            curSection += c;
                        }

                        break;
                    case '"':
                        inQuotes = !inQuotes;

                        break;
                    case ',':
                        break;
                    default:
                        curSection += c;

                        break;
                }

            if (!string.IsNullOrEmpty(curSection))
                sections.Add(curSection);

            return sections;
        }

        #endregion
    }

}