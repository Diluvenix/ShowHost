using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers
{
    internal interface IController : IDisposable
    {
        public UserControl View { get; }
        public Geometry? Path { get; }

        public Task HandleAsync<T>(T packet, CancellationToken ct);
    }
}
