using System.Windows.Input;

namespace Kavand.Windows.Controls {
    internal static class KeyboardHelper {
        public static void GetMetaKeyState(out bool ctrl, out bool shift) {
            ctrl = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            shift = (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        }
    }
}