using Client.Views.Games._57;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers.Games._57
{
    class ModeratorLobbyController : IController
    {
        private readonly ModeratorLobbyView view = new();
        public UserControl View => view;
        public Geometry? Path => null;

        public void Dispose() { }

        public async Task HandleAsync<T>(T packet)
        {

        }
    }
}
