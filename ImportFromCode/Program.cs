using EPiServerChannelLib;

namespace ImportFromCode
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var channel = new EPiServerChannel("Deane", "http://beaumontdemo.local/webservices/contentchannelservice.asmx");

            var page = new Page
            {
                MainBody = "This is the body",
                TeaserText = "This is the teaser"
            };
            channel.Process("Deane's Awesome Page", "1234", page);

            channel.Close();
        }
    }

    internal class Page
    {
        [Ignore]
        public string SomePropertyThatShouldNotBeMappedAndWillNeverBeSet { get; set; }

        public string TeaserText { get; set; }
        public string MainBody { get; set; }
    }
}