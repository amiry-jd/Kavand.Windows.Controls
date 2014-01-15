namespace Kavand.Windows.Controls {
    public interface IVisualStateUpdateable {
        void UpdateVisualState();
        void UpdateVisualState(bool useTransitions);
    }
}