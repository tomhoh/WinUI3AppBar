using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace AppAppBar3
{
    // Thin strip pinned to the bar's inner edge. Panel (not Border/Grid/StackPanel,
    // which are sealed in WinUI 3) is the lightest non-sealed UIElement host that
    // exposes Background and lets us access ProtectedCursor to swap in the two-way
    // resize arrows.
    public class ResizeGrip : Panel
    {
        public void SetCursorShape(InputCursor cursor) => ProtectedCursor = cursor;
    }
}
