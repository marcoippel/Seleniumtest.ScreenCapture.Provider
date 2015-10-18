using Seleniumtest.Provider.Shared.Enum;

namespace Seleniumtest.ScreenCapture.Provider
{
    public interface IScreenCaptureProvider
    {
        void Start();
        void Stop();
        void Save(string pageSource, string url, string message, string methodName, EventType type);
    }
}