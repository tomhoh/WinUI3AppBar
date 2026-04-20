using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;

namespace AppAppBar3
{
    // Thin strip pinned to the bar's inner edge. Exists as a subclass so the
    // parent window can set ProtectedCursor (which is only accessible from a
    // derived UIElement) to swap in the two-way resize arrows.
    public class ResizeGrip : Border
    {
        public void SetCursorShape(InputCursor cursor) => ProtectedCursor = cursor;
    }
}
