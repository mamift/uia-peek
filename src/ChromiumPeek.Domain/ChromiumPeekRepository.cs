using ChromiumPeek.Domain.Models;

namespace UiaPeek.Domain
{
    /// <summary>
    /// Represents a repository for accessing UI Automation elements and their ancestor chains.
    /// </summary>
    public class ChromiumPeekRepository : IChromiumPeekRepository
    {
        public ChromiumChainModel Peek()
        {
            throw new System.NotImplementedException();
        }

        public ChromiumChainModel Peek(int x, int y)
        {
            throw new System.NotImplementedException();
        }
    }
}