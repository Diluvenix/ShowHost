using Client.Views;
using System.Windows.Controls;
using System.Windows.Media;

namespace Client.Controllers
{
    class ModeratorPanelController : IController
    {
        private readonly ModeratorPanel view;
        public UserControl View => view;
        public Geometry? Path => null;


        private readonly IController[] panels;

        public ModeratorPanelController(IController[] panels)
        {
            view = new ModeratorPanel();
            view.SetPanels(panels);
            this.panels = panels;
        }

        public void Dispose() 
        {
            foreach (IController panel in panels)
                panel.Dispose();
        }

        public async Task HandleAsync<T>(T packet) 
            => await Parallel.ForEachAsync(panels, async (panel, _) => await panel.HandleAsync(packet));
    }
}
