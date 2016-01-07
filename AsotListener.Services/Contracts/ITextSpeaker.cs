using System.Threading.Tasks;

namespace AsotListener.Services.Contracts
{
    public interface ITextSpeaker
    {
        Task SpeakText(string text);
    }
}